using Xunit;
using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BannerService.Application.Tests.Providers;

public class AccountRecoveryTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _tokens = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly PasswordHashService _hasher = new();
    private readonly AuthenticationService _service;
    private readonly User _user;
    private string? _sentResetToken;
    private string? _sentVerifyToken;

    public AccountRecoveryTests()
    {
        _user = new User { Id = Guid.NewGuid().ToString(), Email = "owner@example.com", FirstName = "Olive", IsActive = true, PasswordHash = new PasswordHashService().HashPassword("OldPassword1!") };
        _users.Setup(r => r.GetByEmailAsync("owner@example.com")).ReturnsAsync(_user);
        _users.Setup(r => r.GetByIdAsync(_user.Id)).ReturnsAsync(_user);
        _users.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _users.Setup(r => r.GetByPasswordResetTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((string hash) => _user.PasswordResetToken == hash ? _user : null);
        _users.Setup(r => r.GetByVerificationTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((string hash) => _user.VerificationToken == hash ? _user : null);
        _tokens.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);
        _tokens.Setup(r => r.GetActiveByUserIdAsync(It.IsAny<string>())).ReturnsAsync(new List<RefreshToken>());
        _email.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string _, string token, string _) => _sentResetToken = token).ReturnsAsync(true);
        _email.Setup(e => e.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string _, string token, string _) => _sentVerifyToken = token).ReturnsAsync(true);

        _service = new AuthenticationService(_users.Object, Mock.Of<IRoleRepository>(), Mock.Of<IJwtTokenService>(), _hasher, _tokens.Object,
            Mock.Of<IShopRepository>(), Mock.Of<ISubscriptionRepository>(), _email.Object, NullLogger<AuthenticationService>.Instance);
    }

    // ---- forgot password

    [Fact]
    public async Task Forgot_ForAKnownAddress_MailsATokenAndStoresOnlyItsHash()
    {
        var (success, message) = await _service.RequestPasswordResetAsync("Owner@Example.com ");

        Assert.True(success);
        Assert.Equal(AuthenticationService.PasswordResetRequestedMessage, message);
        Assert.NotNull(_sentResetToken);
        Assert.NotEqual(_sentResetToken, _user.PasswordResetToken);
        Assert.Equal(RecoveryTokens.Hash(_sentResetToken!), _user.PasswordResetToken);
        Assert.InRange(_user.PasswordResetExpires!.Value, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
    }

    [Fact]
    public async Task Forgot_GivesTheSameAnswer_WhetherOrNotTheAddressHasAnAccount()
    {
        var known = await _service.RequestPasswordResetAsync("owner@example.com");
        var unknown = await _service.RequestPasswordResetAsync("nobody@example.com");
        var empty = await _service.RequestPasswordResetAsync("");

        Assert.Equal(known, unknown);
        Assert.Equal(known, empty);
        _email.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Forgot_ForAnInactiveAccount_SendsNothingAndSaysTheSame()
    {
        _user.IsActive = false;
        var (_, message) = await _service.RequestPasswordResetAsync("owner@example.com");
        Assert.Equal(AuthenticationService.PasswordResetRequestedMessage, message);
        _email.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Forgot_AskedTwiceWithinAMinute_MailsOnlyOnce()
    {
        await _service.RequestPasswordResetAsync("owner@example.com");
        await _service.RequestPasswordResetAsync("owner@example.com");
        _email.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Forgot_WhenTheMailFails_StillGivesTheSameAnswer()
    {
        _email.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("smtp down"));
        var (success, message) = await _service.RequestPasswordResetAsync("owner@example.com");
        Assert.True(success);
        Assert.Equal(AuthenticationService.PasswordResetRequestedMessage, message);
    }

    // ---- reset

    [Fact]
    public async Task Reset_WithTheMailedToken_ChangesThePassword_ConsumesTheToken_AndEndsEverySession()
    {
        await _service.RequestPasswordResetAsync("owner@example.com");
        var one = new RefreshToken { UserId = _user.Id, Token = "a", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var two = new RefreshToken { UserId = _user.Id, Token = "b", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _tokens.Setup(r => r.GetActiveByUserIdAsync(_user.Id)).ReturnsAsync(new List<RefreshToken> { one, two });

        var (success, _) = await _service.ResetPasswordAsync(_sentResetToken!, "NewPassword1!");

        Assert.True(success);
        Assert.True(_hasher.VerifyPassword("NewPassword1!", _user.PasswordHash));
        Assert.False(_hasher.VerifyPassword("OldPassword1!", _user.PasswordHash));
        Assert.Null(_user.PasswordResetToken);
        Assert.True(one.IsRevoked && two.IsRevoked);
    }

    [Fact]
    public async Task Reset_Works_OnlyOnce()
    {
        await _service.RequestPasswordResetAsync("owner@example.com");
        var token = _sentResetToken!;
        Assert.True((await _service.ResetPasswordAsync(token, "NewPassword1!")).success);
        Assert.False((await _service.ResetPasswordAsync(token, "AnotherPassword1!")).success);
        Assert.True(_hasher.VerifyPassword("NewPassword1!", _user.PasswordHash));
    }

    [Fact]
    public async Task Reset_WithAnExpiredToken_Fails()
    {
        await _service.RequestPasswordResetAsync("owner@example.com");
        _user.PasswordResetExpires = DateTime.UtcNow.AddMinutes(-1);
        var (success, message) = await _service.ResetPasswordAsync(_sentResetToken!, "NewPassword1!");
        Assert.False(success);
        Assert.Contains("not valid or has expired", message);
        Assert.True(_hasher.VerifyPassword("OldPassword1!", _user.PasswordHash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-real-token")]
    public async Task Reset_WithAnUnknownToken_Fails(string token) =>
        Assert.False((await _service.ResetPasswordAsync(token, "NewPassword1!")).success);

    [Fact]
    public async Task Reset_WithTheStoredHashInsteadOfTheToken_Fails()
    {
        await _service.RequestPasswordResetAsync("owner@example.com");
        // someone who read the database only has the hash: it must not work as a token
        Assert.False((await _service.ResetPasswordAsync(_user.PasswordResetToken!, "NewPassword1!")).success);
    }

    [Fact]
    public async Task Reset_WithAShortPassword_FailsAndKeepsTheToken()
    {
        await _service.RequestPasswordResetAsync("owner@example.com");
        var (success, message) = await _service.ResetPasswordAsync(_sentResetToken!, "short");
        Assert.False(success);
        Assert.Contains("8 characters", message);
        Assert.NotNull(_user.PasswordResetToken);
    }

    // ---- verification

    [Fact]
    public async Task Verify_WithTheMailedToken_MarksTheAddressVerified_Once()
    {
        _users.Setup(r => r.GetByIdAsync(_user.Id)).ReturnsAsync(_user);
        await _service.ResendVerificationAsync(_user.Id);
        Assert.NotNull(_sentVerifyToken);
        Assert.Equal(RecoveryTokens.Hash(_sentVerifyToken!), _user.VerificationToken);
        Assert.InRange(_user.VerificationTokenExpires!.Value, DateTime.UtcNow.AddHours(23).AddMinutes(59), DateTime.UtcNow.AddHours(24).AddMinutes(1));

        Assert.True((await _service.VerifyEmailAsync(_sentVerifyToken!)).success);
        Assert.True(_user.EmailVerified);
        Assert.Null(_user.VerificationToken);
        Assert.False((await _service.VerifyEmailAsync(_sentVerifyToken!)).success);
    }

    [Fact]
    public async Task Verify_WithAnExpiredOrUnknownToken_Fails()
    {
        await _service.ResendVerificationAsync(_user.Id);
        var token = _sentVerifyToken!;
        _user.VerificationTokenExpires = DateTime.UtcNow.AddMinutes(-1);

        Assert.False((await _service.VerifyEmailAsync(token)).success);
        Assert.False((await _service.VerifyEmailAsync("unknown")).success);
        Assert.False(_user.EmailVerified);
    }

    [Fact]
    public async Task Resend_IsLimitedToOneAMinute_AndIsSkippedWhenAlreadyVerified()
    {
        Assert.True((await _service.ResendVerificationAsync(_user.Id)).success);
        Assert.False((await _service.ResendVerificationAsync(_user.Id)).success);
        _email.Verify(e => e.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        _user.EmailVerified = true;
        var (success, message) = await _service.ResendVerificationAsync(_user.Id);
        Assert.True(success);
        Assert.Contains("already verified", message);
    }

    [Fact]
    public async Task IsEmailVerified_ReportsTheStoredState()
    {
        Assert.False(await _service.IsEmailVerifiedAsync(_user.Id));
        _user.EmailVerified = true;
        Assert.True(await _service.IsEmailVerifiedAsync(_user.Id));
        Assert.False(await _service.IsEmailVerifiedAsync("nobody"));
    }
}

public class LifecycleWithProviderTests
{
    private static readonly DateTime Today = DateTime.UtcNow.Date;

    private static (SubscriptionLifecycleService service, Mock<IPaymentGateway> gateway, Mock<ISmsSender> sms, Mock<IEmailService> email, Subscription subscription)
        Build(bool providerOn, bool autoRenew, int renewsInDays)
    {
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Olive Mart", OwnerUserId = Guid.NewGuid(), PhoneNumber = "9876543210" };
        var owner = new User { Id = shop.OwnerUserId!.Value.ToString(), Email = "owner@example.com" };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(), ShopId = shop.Id, Status = SubscriptionStatus.Active, AutoRenew = autoRenew, BillingPeriod = BillingPeriod.Annual,
            CurrentPrice = 499m, RenewalDate = Today.AddDays(renewsInDays).AddHours(6), Plan = new SubscriptionPlan { Name = "Gold", Currency = "INR" },
        };

        var subscriptions = new Mock<ISubscriptionRepository>();
        subscriptions.Setup(r => r.GetByStatusAsync(It.IsAny<SubscriptionStatus>()))
            .ReturnsAsync((SubscriptionStatus s) => s == subscription.Status ? new List<Subscription> { subscription } : new List<Subscription>());
        subscriptions.Setup(r => r.UpdateAsync(It.IsAny<Subscription>())).ReturnsAsync((Subscription s) => s);
        var invoices = new Mock<IInvoiceRepository>();
        invoices.Setup(r => r.GenerateInvoiceNumberAsync()).ReturnsAsync("INV-1");
        invoices.Setup(r => r.CreateAsync(It.IsAny<Invoice>())).ReturnsAsync((Invoice i) => i);
        var shops = new Mock<IShopRepository>();
        shops.Setup(r => r.GetByIdAsync(shop.Id)).ReturnsAsync(shop);
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByIdAsync(owner.Id)).ReturnsAsync(owner);
        var notifications = new Mock<ISubscriptionNotificationRepository>();
        notifications.Setup(n => n.TryClaimAsync(It.IsAny<SubscriptionNotification>())).ReturnsAsync(true);

        var gateway = new Mock<IPaymentGateway>();
        gateway.Setup(g => g.ChargeAsync(It.IsAny<Subscription>(), It.IsAny<decimal>())).ReturnsAsync(new PaymentResult { Success = true, Reference = "r" });
        var sms = new Mock<ISmsSender>();
        sms.Setup(s => s.SendTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>())).ReturnsAsync(true);
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Payments:Provider"] = providerOn ? "Fake" : "None" }).Build();
        var registry = new PaymentProviderRegistry(new IPaymentProvider[] { new FakePaymentProvider() }, config);
        var service = new SubscriptionLifecycleService(
            subscriptions.Object, invoices.Object, shops.Object, users.Object, Mock.Of<IRefreshTokenRepository>(),
            email.Object, sms.Object, gateway.Object, registry, notifications.Object,
            new SubscriptionService(Mock.Of<ISubscriptionPlanRepository>(), subscriptions.Object, invoices.Object),
            NullLogger<SubscriptionLifecycleService>.Instance);
        return (service, gateway, sms, email, subscription);
    }

    [Fact]
    public async Task WithAProvider_AutoRenewIsIgnored_NobodyIsChargedAndTheOwnerIsRemindedInstead()
    {
        var (service, gateway, sms, email, subscription) = Build(providerOn: true, autoRenew: true, renewsInDays: 3);

        await service.RunAsync(DateTime.UtcNow);

        gateway.Verify(g => g.ChargeAsync(It.IsAny<Subscription>(), It.IsAny<decimal>()), Times.Never);
        sms.Verify(s => s.SendTemplateAsync("9876543210", SmsTemplateKeys.RenewalReminder, It.IsAny<IReadOnlyList<string>>()), Times.Once);
        email.Verify(e => e.SendEmailAsync("owner@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public async Task WithAProvider_APlanPastItsDate_GoesToGrace_WithoutAChargeAttempt()
    {
        var (service, gateway, _, _, subscription) = Build(providerOn: true, autoRenew: true, renewsInDays: -1);

        await service.RunAsync(DateTime.UtcNow);

        gateway.Verify(g => g.ChargeAsync(It.IsAny<Subscription>(), It.IsAny<decimal>()), Times.Never);
        Assert.Equal(SubscriptionStatus.GracePeriod, subscription.Status);
    }

    [Fact]
    public async Task WithoutAProvider_AutoRenewStillChargesAsBefore()
    {
        var (service, gateway, _, _, _) = Build(providerOn: false, autoRenew: true, renewsInDays: -1);

        await service.RunAsync(DateTime.UtcNow);

        gateway.Verify(g => g.ChargeAsync(It.IsAny<Subscription>(), 499m), Times.Once);
    }

    [Fact]
    public void SmsFor_UsesRegisteredTemplatesAndOnlyDatesPlansAndAmounts()
    {
        var subscription = new Subscription { RenewalDate = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), CurrentPrice = 499m, Plan = new SubscriptionPlan { Name = "Gold", Currency = "USD" } };

        var (reminder, reminderValues) = SubscriptionLifecycleService.SmsFor("Reminder3", subscription);
        Assert.Equal(SmsTemplateKeys.RenewalReminder, reminder);
        Assert.Equal(new[] { "Gold", "1 Dec 2026" }, reminderValues);

        var (paid, paidValues) = SubscriptionLifecycleService.SmsFor("Renewed", subscription);
        Assert.Equal(SmsTemplateKeys.PaymentReceived, paid);
        Assert.Equal(new[] { "499 USD" }, paidValues);

        foreach (var kind in new[] { "GraceDaily", "Expired", "PaymentFailed" })
        {
            var (key, values) = SubscriptionLifecycleService.SmsFor(kind, subscription);
            Assert.Equal(SmsTemplateKeys.PlanEnded, key);
            Assert.Empty(values);
        }
    }
}

public class ProviderStatusServiceTests
{
    private static ProviderStatusService Service(Dictionary<string, string?> settings, Mock<ISmsSender>? sms = null, Mock<IEmailService>? email = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var registry = new PaymentProviderRegistry(Array.Empty<IPaymentProvider>(), config);
        return new ProviderStatusService(config, registry, (sms ?? new Mock<ISmsSender>()).Object, new SmsTemplateCatalog(config), (email ?? new Mock<IEmailService>()).Object);
    }

    [Fact]
    public void WithEverythingEmpty_AllThreeAreNotConnected()
    {
        var status = Service(new()).GetStatus();

        Assert.Equal(new[] { "payments", "sms", "email" }, status.Select(s => s.Name));
        Assert.All(status, s => Assert.False(s.Configured));
        Assert.All(status, s => Assert.Equal("None", s.Provider));
    }

    [Fact]
    public void TheStatus_NamesMissingSettings_ButNeverShowsAValue()
    {
        var status = Service(new()
        {
            ["Payments:Provider"] = "Razorpay",
            ["Payments:Razorpay:KeyId"] = "rzp_live_SECRETVALUE",
            ["Email:Smtp:Host"] = "smtp.secret-host.example",
            ["Email:Smtp:Password"] = "hunter2",
        }).GetStatus();

        var json = System.Text.Json.JsonSerializer.Serialize(status);
        Assert.DoesNotContain("SECRETVALUE", json);
        Assert.DoesNotContain("hunter2", json);
        Assert.DoesNotContain("smtp.secret-host.example", json);
        Assert.Contains("Payments:Razorpay:KeySecret", status.Single(s => s.Name == "payments").Missing);
        Assert.True(status.Single(s => s.Name == "email").Configured);
    }

    [Fact]
    public async Task TestEmail_WithoutMailSetup_SaysSoAndSendsNothing()
    {
        var email = new Mock<IEmailService>();
        var result = await Service(new(), email: email).SendTestEmailAsync("a@b.com");
        Assert.False(result.Sent);
        email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task TestEmail_WithMailSetup_Sends()
    {
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendEmailAsync("a@b.com", It.IsAny<string>(), It.IsAny<string>(), true)).ReturnsAsync(true);
        var result = await Service(new() { ["Email:Smtp:Host"] = "h" }, email: email).SendTestEmailAsync("a@b.com");
        Assert.True(result.Sent);
    }

    [Fact]
    public async Task TestSms_UsesATemplate_NeverFreeText()
    {
        var sms = new Mock<ISmsSender>();
        sms.Setup(s => s.SendTemplateAsync("+919876543210", SmsTemplateKeys.RenewalReminder, It.IsAny<IReadOnlyList<string>>())).ReturnsAsync(true);
        var settings = new Dictionary<string, string?>
        {
            ["Sms:Provider"] = "Msg91",
            ["Sms:Msg91:AuthKey"] = "k",
            ["Sms:Templates:renewal-reminder:ProviderTemplateId"] = "tpl",
            ["Sms:Templates:renewal-reminder:Text"] = "Plan {#var#} ends {#var#}",
        };

        var result = await Service(settings, sms).SendTestSmsAsync("98765 43210");

        Assert.True(result.Sent);
        sms.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TestSms_NotSetUp_OrBadNumber_OrNoTemplateId_IsNotSent()
    {
        var sms = new Mock<ISmsSender>();
        Assert.False((await Service(new(), sms).SendTestSmsAsync("9876543210")).Sent);

        var on = new Dictionary<string, string?> { ["Sms:Provider"] = "Msg91", ["Sms:Msg91:AuthKey"] = "k" };
        Assert.False((await Service(on, sms).SendTestSmsAsync("12")).Sent);

        var noId = new Dictionary<string, string?>(on) { ["Sms:Templates:renewal-reminder:Text"] = "x {#var#} {#var#}" };
        var result = await Service(noId, sms).SendTestSmsAsync("9876543210");
        Assert.False(result.Sent);
        Assert.Contains("ProviderTemplateId", result.Reason);
        sms.Verify(s => s.SendTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()), Times.Never);
    }
}

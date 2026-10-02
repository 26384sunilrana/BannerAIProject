using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class SubscriptionLifecycleTests
{
    // Noon UTC on a fixed day, so "days before renewal" is easy to read
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly Mock<IInvoiceRepository> _invoices = new();
    private readonly Mock<IShopRepository> _shops = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ISmsSender> _sms = new();
    private readonly Mock<IPaymentGateway> _payments = new();
    private readonly FakeNotificationRepository _notifications = new();
    private readonly SubscriptionLifecycleService _service;

    private readonly Shop _shop;
    private readonly User _owner;
    private readonly User _executive;
    private readonly List<Subscription> _all = new();
    private readonly List<Invoice> _createdInvoices = new();

    public SubscriptionLifecycleTests()
    {
        _owner = new User { Id = Guid.NewGuid().ToString(), Email = "owner@example.com", FirstName = "Olive" };
        _shop = new Shop { Id = Guid.NewGuid(), Name = "Olive Mart", OwnerUserId = Guid.Parse(_owner.Id), PhoneNumber = "9876543210" };
        _executive = new User { Id = Guid.NewGuid().ToString(), Email = "exec@example.com", ShopId = _shop.Id.ToString() };
        _owner.ShopId = _shop.Id.ToString();

        _shops.Setup(r => r.GetByIdAsync(_shop.Id)).ReturnsAsync(_shop);
        _users.Setup(r => r.GetByIdAsync(_owner.Id)).ReturnsAsync(_owner);
        _users.Setup(r => r.GetByShopIdAsync(_shop.Id.ToString())).ReturnsAsync(new List<User> { _owner, _executive });
        _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);
        _sms.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _payments.Setup(p => p.ChargeAsync(It.IsAny<Subscription>(), It.IsAny<decimal>()))
            .ReturnsAsync(new PaymentResult { Success = true, Reference = "ref-1" });
        _subscriptions.Setup(r => r.GetByStatusAsync(It.IsAny<SubscriptionStatus>()))
            .ReturnsAsync((SubscriptionStatus s) => _all.Where(x => x.Status == s).ToList());
        _subscriptions.Setup(r => r.UpdateAsync(It.IsAny<Subscription>())).ReturnsAsync((Subscription s) => s);
        _invoices.Setup(r => r.GenerateInvoiceNumberAsync()).ReturnsAsync("INV-1");
        _invoices.Setup(r => r.CreateAsync(It.IsAny<Invoice>())).ReturnsAsync((Invoice i) => { _createdInvoices.Add(i); return i; });
        _refreshTokens.Setup(r => r.GetActiveByUserIdAsync(It.IsAny<string>())).ReturnsAsync(new List<RefreshToken>());

        var subscriptionService = new SubscriptionService(Mock.Of<ISubscriptionPlanRepository>(), _subscriptions.Object, _invoices.Object);
        _service = new SubscriptionLifecycleService(
            _subscriptions.Object, _invoices.Object, _shops.Object, _users.Object, _refreshTokens.Object,
            _email.Object, _sms.Object, _payments.Object, _notifications, subscriptionService,
            NullLogger<SubscriptionLifecycleService>.Instance);
    }

    private Subscription Sub(int renewsInDays, bool autoRenew, SubscriptionStatus status = SubscriptionStatus.Active)
    {
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ShopId = _shop.Id,
            Status = status,
            AutoRenew = autoRenew,
            BillingPeriod = BillingPeriod.Quarterly,
            CurrentPrice = 200m,
            RenewalDate = Now.Date.AddDays(renewsInDays).AddHours(6)
        };
        _all.Add(subscription);
        return subscription;
    }

    [Theory]
    [InlineData(7)]
    [InlineData(3)]
    [InlineData(1)]
    public async Task AutoRenewOff_RemindsByEmailAndSms_SevenThreeAndOneDaysBefore(int daysBefore)
    {
        Sub(daysBefore, autoRenew: false);

        var report = await _service.RunAsync(Now);

        Assert.Equal(2, report.MessagesSent);
        _email.Verify(e => e.SendEmailAsync("owner@example.com", It.Is<string>(s => s.Contains("renews in")), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        _sms.Verify(s => s.SendAsync("9876543210", It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(5)]
    [InlineData(2)]
    public async Task AutoRenewOff_NoReminderOnOtherDays(int daysBefore)
    {
        Sub(daysBefore, autoRenew: false);

        var report = await _service.RunAsync(Now);

        Assert.Equal(0, report.MessagesSent);
    }

    [Fact]
    public async Task AutoRenewOn_NoReminder_AndNothingChargedBeforeTheDate()
    {
        Sub(3, autoRenew: true);

        var report = await _service.RunAsync(Now);

        Assert.Equal(0, report.MessagesSent);
        _payments.Verify(p => p.ChargeAsync(It.IsAny<Subscription>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task RunningTwiceInADay_SendsEachReminderOnce()
    {
        Sub(3, autoRenew: false);

        await _service.RunAsync(Now);
        await _service.RunAsync(Now.AddHours(1));

        _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        _sms.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AutoRenewOn_OnTheRenewalDate_ChargesAndRenews()
    {
        var subscription = Sub(-1, autoRenew: true);   // date passed this morning
        var due = subscription.RenewalDate;

        var report = await _service.RunAsync(Now);

        Assert.Equal(1, report.AutoRenewed);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(due.AddMonths(3), subscription.RenewalDate);
        var invoice = Assert.Single(_createdInvoices);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(200m, invoice.Amount);
        Assert.Equal("ref-1", invoice.PaymentReference);
    }

    [Fact]
    public async Task AutoRenewOn_WhenPaymentFails_StartsAWeekOfGrace()
    {
        _payments.Setup(p => p.ChargeAsync(It.IsAny<Subscription>(), It.IsAny<decimal>()))
            .ReturnsAsync(new PaymentResult { Success = false, Error = "declined" });
        var subscription = Sub(-1, autoRenew: true);
        var due = subscription.RenewalDate;

        var report = await _service.RunAsync(Now);

        Assert.Equal(1, report.RenewalsFailed);
        Assert.Equal(1, report.EnteredGrace);
        Assert.Equal(SubscriptionStatus.GracePeriod, subscription.Status);
        Assert.Equal(due.AddDays(7), subscription.GraceEndsAt);
        Assert.Empty(_createdInvoices);
        Assert.True(subscription.ShowsDefaultBannerOnly);
        Assert.False(subscription.IsLocked);   // logins still work during grace
    }

    [Fact]
    public async Task AutoRenewOff_WhenTheDatePasses_StartsGraceAndTellsTheOwner()
    {
        var subscription = Sub(-1, autoRenew: false);

        var report = await _service.RunAsync(Now);

        Assert.Equal(1, report.EnteredGrace);
        Assert.Equal(SubscriptionStatus.GracePeriod, subscription.Status);
        Assert.Equal(0, report.AutoRenewed);
        _payments.Verify(p => p.ChargeAsync(It.IsAny<Subscription>(), It.IsAny<decimal>()), Times.Never);
        _email.Verify(e => e.SendEmailAsync("owner@example.com", It.IsAny<string>(), It.Is<string>(b => b.Contains("default banner")), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task DuringGrace_ThereIsOneReminderEveryDay()
    {
        var subscription = Sub(-3, autoRenew: false, SubscriptionStatus.GracePeriod);
        subscription.GraceEndsAt = subscription.RenewalDate.AddDays(7);

        await _service.RunAsync(Now);
        await _service.RunAsync(Now.AddDays(1));
        await _service.RunAsync(Now.AddDays(1).AddHours(2));   // same day again

        _email.Verify(e => e.SendEmailAsync("owner@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(2));
        _sms.Verify(s => s.SendAsync("9876543210", It.IsAny<string>()), Times.Exactly(2));
        Assert.Equal(SubscriptionStatus.GracePeriod, subscription.Status);
    }

    [Fact]
    public async Task DuringGrace_AutoRenewRetriesTheChargeOnceADay_AndRecoversWhenItWorks()
    {
        var results = new Queue<bool>(new[] { false, true });
        _payments.Setup(p => p.ChargeAsync(It.IsAny<Subscription>(), It.IsAny<decimal>()))
            .ReturnsAsync(() => new PaymentResult { Success = results.Dequeue(), Reference = "ok" });
        var subscription = Sub(-3, autoRenew: true, SubscriptionStatus.GracePeriod);
        subscription.GraceEndsAt = subscription.RenewalDate.AddDays(7);

        await _service.RunAsync(Now);                    // retry fails
        await _service.RunAsync(Now.AddHours(1));        // same day: no second attempt
        _payments.Verify(p => p.ChargeAsync(It.IsAny<Subscription>(), It.IsAny<decimal>()), Times.Once);
        Assert.Equal(SubscriptionStatus.GracePeriod, subscription.Status);

        var report = await _service.RunAsync(Now.AddDays(1));   // next day the retry works

        Assert.Equal(1, report.AutoRenewed);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Null(subscription.GraceEndsAt);
        Assert.False(subscription.ShowsDefaultBannerOnly);
        Assert.True(subscription.RenewalDate > Now.AddDays(1));
    }

    [Fact]
    public async Task WhenGraceIsOver_TheSubscriptionExpires_AndShopSessionsEnd()
    {
        var subscription = Sub(-8, autoRenew: false, SubscriptionStatus.GracePeriod);
        subscription.GraceEndsAt = subscription.RenewalDate.AddDays(7);
        var ownerToken = new RefreshToken { UserId = _owner.Id, Token = "o" };
        var execToken = new RefreshToken { UserId = _executive.Id, Token = "e" };
        _refreshTokens.Setup(r => r.GetActiveByUserIdAsync(_owner.Id)).ReturnsAsync(new List<RefreshToken> { ownerToken });
        _refreshTokens.Setup(r => r.GetActiveByUserIdAsync(_executive.Id)).ReturnsAsync(new List<RefreshToken> { execToken });

        var report = await _service.RunAsync(Now);

        Assert.Equal(1, report.Expired);
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.True(subscription.IsLocked);
        Assert.True(ownerToken.IsRevoked);
        Assert.True(execToken.IsRevoked);
        _email.Verify(e => e.SendEmailAsync("owner@example.com", It.IsAny<string>(), It.Is<string>(b => b.Contains("switched off")), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task PlatformAdminSessionsAreNotEnded()
    {
        var subscription = Sub(-8, autoRenew: false, SubscriptionStatus.GracePeriod);
        subscription.GraceEndsAt = subscription.RenewalDate.AddDays(7);
        _executive.UserRoles.Add(new UserRole { UserId = _executive.Id, RoleId = "1", Role = new Role { Id = "1", Name = Role.Admin } });
        var adminToken = new RefreshToken { UserId = _executive.Id, Token = "a" };
        _refreshTokens.Setup(r => r.GetActiveByUserIdAsync(_executive.Id)).ReturnsAsync(new List<RefreshToken> { adminToken });

        await _service.RunAsync(Now);

        Assert.False(adminToken.IsRevoked);
    }

    [Fact]
    public async Task ExpiredAndCancelledSubscriptionsAreLeftAlone()
    {
        var expired = Sub(-30, autoRenew: true, SubscriptionStatus.Expired);
        var cancelled = Sub(-30, autoRenew: true, SubscriptionStatus.Cancelled);

        var report = await _service.RunAsync(Now);

        Assert.Equal(0, report.MessagesSent + report.AutoRenewed + report.EnteredGrace + report.Expired);
        Assert.Equal(SubscriptionStatus.Expired, expired.Status);
        Assert.Equal(SubscriptionStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task OneBrokenSubscriptionDoesNotStopTheOthers()
    {
        var broken = Sub(3, autoRenew: false);
        var other = Sub(1, autoRenew: false);
        broken.ShopId = Guid.NewGuid();
        _shops.Setup(r => r.GetByIdAsync(broken.ShopId)).ThrowsAsync(new InvalidOperationException("database unavailable"));

        var report = await _service.RunAsync(Now);

        Assert.Equal(2, report.MessagesSent);   // the other subscription still got its reminder
        _email.Verify(e => e.SendEmailAsync("owner@example.com", It.Is<string>(s => s.Contains("1 day")), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task ASmsFailureDoesNotPreventTheEmail()
    {
        _sms.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("gateway down"));
        Sub(1, autoRenew: false);

        var report = await _service.RunAsync(Now);

        Assert.Equal(1, report.MessagesSent);
        _email.Verify(e => e.SendEmailAsync("owner@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    private void Finds(Subscription subscription) =>
        _subscriptions.Setup(r => r.GetByIdAsync(subscription.Id)).ReturnsAsync(subscription);

    [Fact]
    public async Task RenewNow_BeforeTheDate_AddsAPeriodToTheCurrentOne()
    {
        var subscription = Sub(10, autoRenew: false);
        Finds(subscription);
        var due = subscription.RenewalDate;

        var (success, _) = await _service.RenewNowAsync(subscription.Id);

        Assert.True(success);
        Assert.Equal(due.AddMonths(3), subscription.RenewalDate);   // no paid days are lost
        Assert.Single(_createdInvoices);
    }

    [Fact]
    public async Task RenewNow_AfterTheGraceWeek_StartsANewPeriodTodayAndLiftsTheLock()
    {
        var subscription = Sub(-20, autoRenew: false, SubscriptionStatus.Expired);
        Finds(subscription);

        var (success, _) = await _service.RenewNowAsync(subscription.Id);

        Assert.True(success);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.False(subscription.IsLocked);
        Assert.True(subscription.RenewalDate > DateTime.UtcNow.AddMonths(2));
    }

    [Fact]
    public async Task RenewNow_WhenThePaymentFails_ChangesNothing()
    {
        _payments.Setup(p => p.ChargeAsync(It.IsAny<Subscription>(), It.IsAny<decimal>()))
            .ReturnsAsync(new PaymentResult { Success = false, Error = "Card declined" });
        var subscription = Sub(-1, autoRenew: false, SubscriptionStatus.GracePeriod);
        Finds(subscription);

        var (success, message) = await _service.RenewNowAsync(subscription.Id);

        Assert.False(success);
        Assert.Equal("Card declined", message);
        Assert.Equal(SubscriptionStatus.GracePeriod, subscription.Status);
        Assert.Empty(_createdInvoices);
    }

    [Fact]
    public async Task RenewNow_RefusesCancelledAndUnknownSubscriptions()
    {
        var cancelled = Sub(5, autoRenew: false, SubscriptionStatus.Cancelled);
        Finds(cancelled);

        Assert.False((await _service.RenewNowAsync(cancelled.Id)).success);
        Assert.False((await _service.RenewNowAsync(Guid.NewGuid())).success);
    }

    [Fact]
    public void SubscriptionLockRules()
    {
        Assert.False(new Subscription { Status = SubscriptionStatus.Active }.ShowsDefaultBannerOnly);
        Assert.True(new Subscription { Status = SubscriptionStatus.GracePeriod }.ShowsDefaultBannerOnly);
        Assert.False(new Subscription { Status = SubscriptionStatus.GracePeriod }.IsLocked);
        Assert.True(new Subscription { Status = SubscriptionStatus.Expired }.IsLocked);
        Assert.True(new Subscription { Status = SubscriptionStatus.Suspended }.IsLocked);
    }

    [Fact]
    public void RenewFrom_ClearsGraceAndMovesTheDateOn()
    {
        var subscription = new Subscription { Status = SubscriptionStatus.GracePeriod, BillingPeriod = BillingPeriod.HalfYearly, GraceEndsAt = Now, PaymentFailureCount = 2 };

        subscription.RenewFrom(Now);

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(Now.AddMonths(6), subscription.RenewalDate);
        Assert.Null(subscription.GraceEndsAt);
        Assert.Equal(0, subscription.PaymentFailureCount);
    }

    private class FakeNotificationRepository : ISubscriptionNotificationRepository
    {
        private readonly HashSet<(Guid, string, NotificationChannel, DateTime)> _claimed = new();

        public Task<bool> TryClaimAsync(SubscriptionNotification n) =>
            Task.FromResult(_claimed.Add((n.SubscriptionId, n.Kind, n.Channel, n.Day.Date)));

        public Task MarkResultAsync(Guid notificationId, bool delivered, string? error) => Task.CompletedTask;
    }
}

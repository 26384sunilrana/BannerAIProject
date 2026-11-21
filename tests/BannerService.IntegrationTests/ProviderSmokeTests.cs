using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>Answers the provider's calls with a prepared response, so no test reaches a real payment or SMS service.</summary>
public class ScriptedHandler : HttpMessageHandler
{
    public List<(string url, string body)> Calls { get; } = new();
    public string Response { get; set; } = "{\"id\":\"order_test_1\"}";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls.Add((request.RequestUri!.ToString(), request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Response, Encoding.UTF8, "application/json") };
    }
}

/// <summary>The application with Razorpay switched on (test values) and its HTTP calls scripted.</summary>
public class RazorpayFactory : SmokeFactory
{
    public const string KeyId = "rzp_test_abc";
    public const string KeySecret = "test-key-secret";
    public const string WebhookSecret = "test-webhook-secret";
    public ScriptedHandler Handler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Payments:Provider", "Razorpay");
        builder.UseSetting("Payments:Razorpay:KeyId", KeyId);
        builder.UseSetting("Payments:Razorpay:KeySecret", KeySecret);
        builder.UseSetting("Payments:Razorpay:WebhookSecret", WebhookSecret);
        builder.ConfigureServices(services =>
            services.AddHttpClient<RazorpayPaymentProvider>().ConfigurePrimaryHttpMessageHandler(() => Handler));
    }
}

/// <summary>Payments, SMS, e-mail and account recovery through the real application, with every provider empty (bolt 044a).</summary>
public class ProviderOffSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public ProviderOffSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<HttpClient> SignedInAsync(string email, string password)
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/authentication/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login)).GetProperty("tokens").GetProperty("accessToken").GetString()!);
        return client;
    }

    private async Task<(HttpClient client, string email)> NewOwnerAsync()
    {
        await _factory.SeedAsync();
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/authentication/register", new { email, password = "Password123!", firstName = "Olive", lastName = "Owner", shopName = "Olive Mart " + email });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(response)).GetProperty("tokens").GetProperty("accessToken").GetString()!);
        return (client, email);
    }

    // ---- payments are off

    [Fact]
    public async Task PaymentConfig_NeedsASignIn_AndSaysOnlinePaymentIsOff()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/payments/config")).StatusCode);

        var (owner, _) = await NewOwnerAsync();
        var config = await Json(await owner.GetAsync("/api/payments/config"));

        Assert.False(config.GetProperty("enabled").GetBoolean());
        Assert.Equal("INR", config.GetProperty("currency").GetString());
        Assert.Equal("IN", config.GetProperty("country").GetString());
        Assert.DoesNotContain("secret", config.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WithNoProvider_CheckoutIsRefused_AndTheWebhookIsNotFound()
    {
        var (owner, _) = await NewOwnerAsync();

        var checkout = await owner.PostAsJsonAsync($"/api/subscriptions/{Guid.NewGuid()}/checkout", new { });
        Assert.NotEqual(HttpStatusCode.OK, checkout.StatusCode);

        var webhook = await _factory.CreateClient().PostAsync("/api/payments/webhook", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.NotFound, webhook.StatusCode);
    }

    [Fact]
    public async Task RefundsAreForAdministratorsOnly()
    {
        var (owner, _) = await NewOwnerAsync();
        var response = await owner.PostAsJsonAsync($"/api/admin/payments/{Guid.NewGuid()}/refund", new { reason = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var admin = await SignedInAsync("admin@example.com", "AdminPass123!");
        var none = await admin.PostAsJsonAsync($"/api/admin/payments/{Guid.NewGuid()}/refund", new { reason = "x" });
        Assert.Equal(HttpStatusCode.Conflict, none.StatusCode); // no provider
    }

    // ---- the Providers screen

    [Fact]
    public async Task ProviderStatus_IsForAdministrators_AndShowsEverythingNotConnected()
    {
        var (owner, _) = await NewOwnerAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/admin/providers")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/admin/providers")).StatusCode);

        var admin = await SignedInAsync("admin@example.com", "AdminPass123!");
        var status = await Json(await admin.GetAsync("/api/admin/providers"));

        Assert.Equal(new[] { "payments", "sms", "email" }, status.EnumerateArray().Select(e => e.GetProperty("name").GetString()).ToArray());
        Assert.All(status.EnumerateArray(), e => Assert.False(e.GetProperty("configured").GetBoolean()));
    }

    [Fact]
    public async Task TheTestButtons_SayWhyNothingWasSent_WhenNothingIsSetUp()
    {
        var admin = await SignedInAsync("admin@example.com", "AdminPass123!");

        var email = await Json(await admin.PostAsJsonAsync("/api/admin/providers/test-email", new { to = (string?)null }));
        Assert.False(email.GetProperty("sent").GetBoolean());
        Assert.Contains("not set up", email.GetProperty("reason").GetString());

        var sms = await Json(await admin.PostAsJsonAsync("/api/admin/providers/test-sms", new { to = "9876543210" }));
        Assert.False(sms.GetProperty("sent").GetBoolean());

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/providers/test-sms", new { to = "" })).StatusCode);
        var (owner, _) = await NewOwnerAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/admin/providers/test-email", new { to = "a@b.com" })).StatusCode);
    }

    // ---- account recovery

    private async Task<string> GiveTokenAsync(string email, bool reset, TimeSpan? expiresIn = null)
    {
        var raw = RecoveryTokens.NewToken();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.FirstAsync(u => u.Email == email);
        if (reset)
        {
            user.PasswordResetToken = RecoveryTokens.Hash(raw);
            user.PasswordResetExpires = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(1));
        }
        else
        {
            user.VerificationToken = RecoveryTokens.Hash(raw);
            user.VerificationTokenExpires = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(24));
        }
        await context.SaveChangesAsync();
        return raw;
    }

    [Fact]
    public async Task ForgotPassword_GivesTheSameAnswer_ForAKnownAndAnUnknownAddress()
    {
        var (_, email) = await NewOwnerAsync();
        var anonymous = _factory.CreateClient();

        var known = await anonymous.PostAsJsonAsync("/api/authentication/forgot-password", new { email });
        var unknown = await anonymous.PostAsJsonAsync("/api/authentication/forgot-password", new { email = "nobody-" + Guid.NewGuid().ToString("N") + "@example.com" });

        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        Assert.Equal((await Json(known)).GetProperty("message").GetString(), (await Json(unknown)).GetProperty("message").GetString());
    }

    [Fact]
    public async Task ResetPassword_NeedsNoSession_ChangesThePassword_IsSingleUse_AndEndsEarlierSessions()
    {
        var (owner, email) = await NewOwnerAsync();
        var anonymous = _factory.CreateClient();

        // a sign-in made before the reset
        var before = _factory.CreateClient();
        var firstLogin = await before.PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" });
        var oldRefresh = (await Json(firstLogin)).GetProperty("tokens").GetProperty("refreshToken").GetString();

        var token = await GiveTokenAsync(email, reset: true);
        var reset = await anonymous.PostAsJsonAsync("/api/authentication/reset-password", new { token, newPassword = "BrandNew123!", confirmPassword = "BrandNew123!" });

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/authentication/login", new { email, password = "BrandNew123!" })).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" })).StatusCode);

        // the same link twice
        var again = await anonymous.PostAsJsonAsync("/api/authentication/reset-password", new { token, newPassword = "Another123!!", confirmPassword = "Another123!!" });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        // the session from before no longer refreshes
        if (!string.IsNullOrEmpty(oldRefresh))
        {
            var refreshed = await anonymous.PostAsJsonAsync("/api/authentication/refresh-token", new { refreshToken = oldRefresh });
            Assert.NotEqual(HttpStatusCode.OK, refreshed.StatusCode);
        }
    }

    [Fact]
    public async Task ResetPassword_WithAnExpiredOrMadeUpToken_OrMismatchedPasswords_IsRefused()
    {
        var (_, email) = await NewOwnerAsync();
        var anonymous = _factory.CreateClient();

        var expired = await GiveTokenAsync(email, reset: true, expiresIn: TimeSpan.FromMinutes(-5));
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/authentication/reset-password", new { token = expired, newPassword = "BrandNew123!", confirmPassword = "BrandNew123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/authentication/reset-password", new { token = "made-up", newPassword = "BrandNew123!", confirmPassword = "BrandNew123!" })).StatusCode);

        var live = await GiveTokenAsync(email, reset: true);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/authentication/reset-password", new { token = live, newPassword = "BrandNew123!", confirmPassword = "Different123!" })).StatusCode);
        // the link still works after a typing mistake
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/authentication/reset-password", new { token = live, newPassword = "BrandNew123!", confirmPassword = "BrandNew123!" })).StatusCode);
    }

    [Fact]
    public async Task TheStoredToken_IsAHash_NotTheLink()
    {
        var (_, email) = await NewOwnerAsync();
        var raw = await GiveTokenAsync(email, reset: true);

        using var scope = _factory.Services.CreateScope();
        var stored = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.AsNoTracking().FirstAsync(u => u.Email == email)).PasswordResetToken;
        Assert.NotEqual(raw, stored);

        // the hash on its own is no use as a link
        var withHash = await _factory.CreateClient().PostAsJsonAsync("/api/authentication/reset-password", new { token = stored, newPassword = "BrandNew123!", confirmPassword = "BrandNew123!" });
        Assert.Equal(HttpStatusCode.BadRequest, withHash.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_NeedsNoSession_WorksOnce_AndShowsInMe()
    {
        var (owner, email) = await NewOwnerAsync();
        var me = await Json(await owner.GetAsync("/api/authentication/me"));
        Assert.False(me.GetProperty("emailVerified").GetBoolean());
        Assert.False(me.GetProperty("emailEnabled").GetBoolean()); // no mail server in this test

        var token = await GiveTokenAsync(email, reset: false);
        var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/authentication/verify-email", new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/authentication/verify-email", new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/authentication/verify-email", new { token = "nope" })).StatusCode);

        Assert.True((await Json(await owner.GetAsync("/api/authentication/me"))).GetProperty("emailVerified").GetBoolean());
    }

    [Fact]
    public async Task ResendVerification_NeedsASignIn()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().PostAsJsonAsync("/api/authentication/resend-verification", new { })).StatusCode);

        var (owner, _) = await NewOwnerAsync();
        // sign-up already issued a link a moment ago, so a second one is refused until a minute has passed
        var response = await owner.PostAsJsonAsync("/api/authentication/resend-verification", new { });
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.TooManyRequests);
    }
}

/// <summary>The same through the real application with Razorpay on (test values, scripted HTTP): checkout, confirm, webhook, refund.</summary>
public class RazorpaySmokeTests : IClassFixture<RazorpayFactory>
{
    private readonly RazorpayFactory _factory;

    public RazorpaySmokeTests(RazorpayFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    private static string Sign(string secret, string payload) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

    private async Task<(HttpClient owner, Subscription subscription)> OwnerWithSubscriptionAsync()
    {
        await _factory.SeedAsync();
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/authentication/register", new { email, password = "Password123!", firstName = "Olive", lastName = "Owner", shopName = "Olive Mart " + email });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(response)).GetProperty("tokens").GetProperty("accessToken").GetString()!);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var shopId = Guid.Parse((await context.Users.FirstAsync(u => u.Email == email)).ShopId);
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Gold " + Guid.NewGuid().ToString("N")[..6], MonthlyPrice = 49m, AnnualPrice = 499m, Currency = "INR", IsActive = true };
        context.SubscriptionPlans.Add(plan);
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(), ShopId = shopId, PlanId = plan.Id, Status = SubscriptionStatus.Active, BillingPeriod = BillingPeriod.Annual,
            StartDate = DateTime.UtcNow, RenewalDate = DateTime.UtcNow.Date.AddDays(5), CurrentPrice = 499m, AutoRenew = true,
        };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        return (client, subscription);
    }

    private static string CapturedBody(string orderId, string paymentId, long amountMinor, string currency = "INR") =>
        $"{{\"event\":\"payment.captured\",\"payload\":{{\"payment\":{{\"entity\":{{\"id\":\"{paymentId}\",\"order_id\":\"{orderId}\",\"amount\":{amountMinor},\"currency\":\"{currency}\"}}}}}}}}";

    private async Task<HttpResponseMessage> PostWebhookAsync(string body, string? signature = null, string? eventId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        request.Headers.Add("X-Razorpay-Signature", signature ?? Sign(RazorpayFactory.WebhookSecret, body));
        request.Headers.Add("X-Razorpay-Event-Id", eventId ?? "evt_" + Guid.NewGuid().ToString("N"));
        return await _factory.CreateClient().SendAsync(request);
    }

    private async Task<T> InDb<T>(Func<ApplicationDbContext, Task<T>> work)
    {
        using var scope = _factory.Services.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    [Fact]
    public async Task Config_ReportsRazorpay_AndNoSecrets()
    {
        var (owner, _) = await OwnerWithSubscriptionAsync();
        var config = await Json(await owner.GetAsync("/api/payments/config"));

        Assert.True(config.GetProperty("enabled").GetBoolean());
        Assert.Equal("Razorpay", config.GetProperty("provider").GetString());
        Assert.DoesNotContain(RazorpayFactory.KeySecret, config.GetRawText());
        Assert.DoesNotContain(RazorpayFactory.WebhookSecret, config.GetRawText());
    }

    [Fact]
    public async Task Checkout_MakesAnOrder_ForThePlansPriceInMinorUnits_AndSendsNoPersonalData()
    {
        var (owner, subscription) = await OwnerWithSubscriptionAsync();
        _factory.Handler.Calls.Clear();
        _factory.Handler.Response = "{\"id\":\"order_http_1\"}";

        var response = await owner.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/checkout", new { });
        var body = await Json(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("order_http_1", body.GetProperty("orderId").GetString());
        Assert.Equal(49900, body.GetProperty("amountMinor").GetInt64());
        Assert.Equal("INR", body.GetProperty("currency").GetString());
        Assert.Equal(RazorpayFactory.KeyId, body.GetProperty("publicKeyId").GetString());
        Assert.DoesNotContain(RazorpayFactory.KeySecret, body.GetRawText());

        var call = Assert.Single(_factory.Handler.Calls);
        Assert.EndsWith("/v1/orders", call.url);
        Assert.DoesNotContain("@", call.body);   // no e-mail address
        Assert.DoesNotContain("Olive", call.body); // no name

        var attempt = await InDb(db => db.PaymentAttempts.AsNoTracking().FirstAsync(a => a.SubscriptionId == subscription.Id));
        Assert.Equal(PaymentAttemptStatus.Created, attempt.Status);
        Assert.Equal("order_http_1", attempt.ProviderOrderId);
    }

    [Fact]
    public async Task Checkout_ForAnotherShopsSubscription_IsRefused()
    {
        var (_, mine) = await OwnerWithSubscriptionAsync();
        var (other, _) = await OwnerWithSubscriptionAsync();

        var response = await other.PostAsJsonAsync($"/api/subscriptions/{mine.Id}/checkout", new { });

        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        Assert.Empty(await InDb(db => db.PaymentAttempts.Where(a => a.SubscriptionId == mine.Id).ToListAsync()));
    }

    [Fact]
    public async Task Confirm_WithAGenuineSignature_PaysAndRenews_ThenTheWebhookChangesNothing()
    {
        var (owner, subscription) = await OwnerWithSubscriptionAsync();
        _factory.Handler.Response = "{\"id\":\"order_confirm_1\"}";
        var due = subscription.RenewalDate;
        await owner.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/checkout", new { });

        var confirm = await owner.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/checkout/confirm",
            new { orderId = "order_confirm_1", paymentId = "pay_confirm_1", signature = Sign(RazorpayFactory.KeySecret, "order_confirm_1|pay_confirm_1") });

        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var renewed = await InDb(db => db.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id));
        Assert.Equal(due.AddMonths(12), renewed.RenewalDate);
        var invoices = await InDb(db => db.Invoices.AsNoTracking().Where(i => i.SubscriptionId == subscription.Id).ToListAsync());
        var invoice = Assert.Single(invoices);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal("INR", invoice.Currency);

        // the provider's own report of the same payment, afterwards
        var webhook = await PostWebhookAsync(CapturedBody("order_confirm_1", "pay_confirm_1", 49900));
        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);
        Assert.Single(await InDb(db => db.Invoices.AsNoTracking().Where(i => i.SubscriptionId == subscription.Id).ToListAsync()));
        Assert.Equal(due.AddMonths(12), (await InDb(db => db.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id))).RenewalDate);

        var shown = await Json(await owner.GetAsync($"/api/shops/{subscription.ShopId}/payments"));
        var row = Assert.Single(shown.EnumerateArray());
        Assert.Equal("Paid", row.GetProperty("status").GetString());
        Assert.DoesNotContain("pay_confirm_1", shown.GetRawText()); // provider ids are not shown
    }

    [Fact]
    public async Task Confirm_WithABadSignature_IsRefused_AndNothingChanges()
    {
        var (owner, subscription) = await OwnerWithSubscriptionAsync();
        _factory.Handler.Response = "{\"id\":\"order_bad_1\"}";
        var due = subscription.RenewalDate;
        await owner.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/checkout", new { });

        var confirm = await owner.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/checkout/confirm",
            new { orderId = "order_bad_1", paymentId = "pay_1", signature = Sign("wrong-secret", "order_bad_1|pay_1") });

        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
        Assert.Equal(due, (await InDb(db => db.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id))).RenewalDate);
        Assert.Empty(await InDb(db => db.Invoices.Where(i => i.SubscriptionId == subscription.Id).ToListAsync()));
    }

    [Fact]
    public async Task Webhook_WithABadSignature_IsRefused_AndLeavesNoTrace()
    {
        var body = CapturedBody("order_x", "pay_x", 100);
        var response = await PostWebhookAsync(body, signature: Sign("not-the-secret", body), eventId: "evt_bad_signature");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await InDb(db => db.WebhookReceipts.Where(r => r.ProviderEventId == "evt_bad_signature").ToListAsync()));
    }

    [Fact]
    public async Task Webhook_PaysAnOrder_Once_EvenWhenDeliveredTwice()
    {
        var (owner, subscription) = await OwnerWithSubscriptionAsync();
        _factory.Handler.Response = "{\"id\":\"order_hook_1\"}";
        var due = subscription.RenewalDate;
        await owner.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/checkout", new { });
        var body = CapturedBody("order_hook_1", "pay_hook_1", 49900);

        var first = await PostWebhookAsync(body, eventId: "evt_same");
        var second = await PostWebhookAsync(body, eventId: "evt_same");

        Assert.Equal("Applied", (await Json(first)).GetProperty("outcome").GetString());
        Assert.Equal("AlreadyApplied", (await Json(second)).GetProperty("outcome").GetString());
        Assert.Single(await InDb(db => db.Invoices.Where(i => i.SubscriptionId == subscription.Id).ToListAsync()));
        Assert.Equal(due.AddMonths(12), (await InDb(db => db.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id))).RenewalDate);
    }

    [Fact]
    public async Task Webhook_ForAnotherAmount_IsAcceptedButNotApplied()
    {
        var (owner, subscription) = await OwnerWithSubscriptionAsync();
        _factory.Handler.Response = "{\"id\":\"order_short_1\"}";
        var due = subscription.RenewalDate;
        await owner.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/checkout", new { });

        var response = await PostWebhookAsync(CapturedBody("order_short_1", "pay_short_1", 100));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // so the provider stops retrying
        Assert.Equal("AmountMismatch", (await Json(response)).GetProperty("outcome").GetString());
        Assert.Equal(due, (await InDb(db => db.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id))).RenewalDate);
        Assert.Empty(await InDb(db => db.Invoices.Where(i => i.SubscriptionId == subscription.Id).ToListAsync()));
        Assert.Equal(PaymentAttemptStatus.Created, (await InDb(db => db.PaymentAttempts.AsNoTracking().FirstAsync(a => a.SubscriptionId == subscription.Id))).Status);
    }

    [Fact]
    public async Task Webhook_ForAnUnknownOrder_OrAnEventWeIgnore_IsAcceptedAndChangesNothing()
    {
        var unknown = await PostWebhookAsync(CapturedBody("order_nobody", "pay_nobody", 100));
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        Assert.Equal("UnknownOrder", (await Json(unknown)).GetProperty("outcome").GetString());

        var ignored = await PostWebhookAsync("{\"event\":\"subscription.charged\",\"payload\":{}}");
        Assert.Equal(HttpStatusCode.OK, ignored.StatusCode);
        Assert.Equal("Ignored", (await Json(ignored)).GetProperty("outcome").GetString());
    }

    [Fact]
    public async Task Webhook_OverTheSizeLimit_IsRefused()
    {
        var big = new string('x', 70 * 1024);
        var response = await PostWebhookAsync(big);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task AnOwnersRenewClick_NeedsPayment_ButAnAdministratorCanStillRenewByHand()
    {
        var (owner, subscription) = await OwnerWithSubscriptionAsync();

        var click = await owner.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/renew", new { });
        Assert.Equal(HttpStatusCode.Conflict, click.StatusCode);
        Assert.Equal("payment_required", (await Json(click)).GetProperty("code").GetString());

        var admin = _factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" });
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login)).GetProperty("tokens").GetProperty("accessToken").GetString()!);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/renew", new { })).StatusCode);
    }

    [Fact]
    public async Task Refund_IsMadeThroughTheProvider_AndCannotExceedWhatWasPaid()
    {
        var (owner, subscription) = await OwnerWithSubscriptionAsync();
        _factory.Handler.Response = "{\"id\":\"order_refund_1\"}";
        await owner.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/checkout", new { });
        await PostWebhookAsync(CapturedBody("order_refund_1", "pay_refund_1", 49900));
        var attemptId = (await InDb(db => db.PaymentAttempts.AsNoTracking().FirstAsync(a => a.SubscriptionId == subscription.Id))).Id;

        var admin = _factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" });
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login)).GetProperty("tokens").GetProperty("accessToken").GetString()!);

        _factory.Handler.Calls.Clear();
        _factory.Handler.Response = "{\"id\":\"rfnd_http_1\"}";
        var part = await admin.PostAsJsonAsync($"/api/admin/payments/{attemptId}/refund", new { amount = 100m, reason = "goodwill" });
        Assert.Equal(HttpStatusCode.OK, part.StatusCode);
        Assert.Contains("/v1/payments/pay_refund_1/refund", _factory.Handler.Calls.Single().url);
        Assert.Equal("{\"amount\":10000}", _factory.Handler.Calls.Single().body);

        var tooMuch = await admin.PostAsJsonAsync($"/api/admin/payments/{attemptId}/refund", new { amount = 500m, reason = "too much" });
        Assert.Equal(HttpStatusCode.BadRequest, tooMuch.StatusCode);

        var attempt = await InDb(db => db.PaymentAttempts.Include(a => a.Refunds).AsNoTracking().FirstAsync(a => a.Id == attemptId));
        Assert.Equal(PaymentAttemptStatus.PartlyRefunded, attempt.Status);
        Assert.Equal(100m, attempt.RefundedAmount);

        // the reason stays with us; the payment id is stored encrypted, not in the clear
        Assert.DoesNotContain("goodwill", _factory.Handler.Calls.Single().body);
    }

    [Fact]
    public async Task CleanUp_RemovesOnlyOldUnfinishedAttemptsAndOldReceipts()
    {
        await _factory.SeedAsync();
        var shop = Guid.NewGuid();
        var subscription = Guid.NewGuid();
        PaymentAttempt Attempt(string order, PaymentAttemptStatus status, int daysOld) => new()
        {
            SubscriptionId = subscription, ShopId = shop, Provider = "Razorpay", ProviderOrderId = order, Amount = 1m, Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(-daysOld),
        };

        await InDb(async db =>
        {
            db.PaymentAttempts.AddRange(
                Attempt("clean_old_created", PaymentAttemptStatus.Created, 8),
                Attempt("clean_new_created", PaymentAttemptStatus.Created, 1),
                Attempt("clean_old_paid", PaymentAttemptStatus.Paid, 30));
            db.WebhookReceipts.AddRange(
                new WebhookReceipt { Provider = "Razorpay", ProviderEventId = "clean_old_event", EventType = "x", ReceivedAt = DateTime.UtcNow.AddDays(-200) },
                new WebhookReceipt { Provider = "Razorpay", ProviderEventId = "clean_new_event", EventType = "x", ReceivedAt = DateTime.UtcNow.AddDays(-1) });
            await db.SaveChangesAsync();
            return 0;
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var removed = await scope.ServiceProvider.GetRequiredService<BannerService.Domain.Interfaces.IPaymentRepository>().CleanUpAsync(DateTime.UtcNow);
            Assert.Equal(2, removed);
        }

        var orders = await InDb(db => db.PaymentAttempts.AsNoTracking().Where(a => a.ProviderOrderId.StartsWith("clean_")).Select(a => a.ProviderOrderId).ToListAsync());
        Assert.Equal(new[] { "clean_new_created", "clean_old_paid" }, orders.OrderBy(o => o).ToArray());
        var events = await InDb(db => db.WebhookReceipts.AsNoTracking().Where(r => r.ProviderEventId.StartsWith("clean_")).Select(r => r.ProviderEventId).ToListAsync());
        Assert.Equal(new[] { "clean_new_event" }, events.ToArray());
    }

    [Fact]
    public async Task TheListOfAShopsPayments_BelongsToThatShopOnly()
    {
        var (mine, subscription) = await OwnerWithSubscriptionAsync();
        var (other, otherSubscription) = await OwnerWithSubscriptionAsync();
        _factory.Handler.Response = "{\"id\":\"order_list_1\"}";
        await mine.PostAsJsonAsync($"/api/subscriptions/{subscription.Id}/checkout", new { });

        Assert.Single((await Json(await mine.GetAsync($"/api/shops/{subscription.ShopId}/payments"))).EnumerateArray());
        Assert.Empty((await Json(await other.GetAsync($"/api/shops/{otherSubscription.ShopId}/payments"))).EnumerateArray());
        Assert.True((await other.GetAsync($"/api/shops/{subscription.ShopId}/payments")).StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TheProvidersScreen_ShowsPaymentsConnected_WithoutShowingAnyValue()
    {
        await _factory.SeedAsync();
        var admin = _factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" });
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login)).GetProperty("tokens").GetProperty("accessToken").GetString()!);

        var raw = await (await admin.GetAsync("/api/admin/providers")).Content.ReadAsStringAsync();
        var status = JsonDocument.Parse(raw).RootElement;

        Assert.True(status.EnumerateArray().Single(e => e.GetProperty("name").GetString() == "payments").GetProperty("configured").GetBoolean());
        Assert.DoesNotContain(RazorpayFactory.KeySecret, raw);
        Assert.DoesNotContain(RazorpayFactory.KeyId, raw);
        Assert.DoesNotContain(RazorpayFactory.WebhookSecret, raw);
    }
}

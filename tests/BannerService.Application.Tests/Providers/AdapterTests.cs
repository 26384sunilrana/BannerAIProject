using Xunit;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using BannerService.Application.Services;
using BannerService.Domain.Interfaces;
using BannerService.Domain.ValueObjects;
using BannerService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BannerService.Application.Tests.Providers;

/// <summary>Answers every request with a prepared response and keeps what was sent, so adapters are tested without any network.</summary>
public class FakeHttpHandler : HttpMessageHandler
{
    public List<(HttpRequestMessage request, string body)> Requests { get; } = new();
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public string Response { get; set; } = "{}";
    public Exception? Throw { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        if (Throw != null) throw Throw;
        return new HttpResponseMessage(Status) { Content = new StringContent(Response, Encoding.UTF8, "application/json") };
    }
}

public class RazorpayProviderTests
{
    private const string KeyId = "rzp_test_key";
    private const string KeySecret = "key-secret";
    private const string WebhookSecret = "webhook-secret";

    private readonly FakeHttpHandler _http = new();

    private RazorpayPaymentProvider Provider(bool configured = true)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Payments:Razorpay:KeyId"] = configured ? KeyId : "",
            ["Payments:Razorpay:KeySecret"] = configured ? KeySecret : "",
            ["Payments:Razorpay:WebhookSecret"] = configured ? WebhookSecret : "",
        }).Build();
        return new RazorpayPaymentProvider(new HttpClient(_http), config, NullLogger<RazorpayPaymentProvider>.Instance);
    }

    private static string Sign(string secret, string payload) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

    private static IReadOnlyDictionary<string, string> Headers(string body, string secret = WebhookSecret, string? eventId = "evt_1") =>
        new Dictionary<string, string>
        {
            ["x-razorpay-signature"] = Sign(secret, body),
            ["x-razorpay-event-id"] = eventId ?? string.Empty,
        };

    private const string Captured =
        "{\"event\":\"payment.captured\",\"payload\":{\"payment\":{\"entity\":{\"id\":\"pay_1\",\"order_id\":\"order_1\",\"amount\":49900,\"currency\":\"INR\"}}}}";

    [Fact]
    public void IsConfigured_OnlyWithAllThreeValues()
    {
        Assert.True(Provider(true).IsConfigured);
        Assert.False(Provider(false).IsConfigured);
    }

    [Fact]
    public void CheckoutSignature_IsHmacOfOrderAndPaymentWithTheKeySecret()
    {
        var provider = Provider();
        var good = Sign(KeySecret, "order_1|pay_1");

        Assert.True(provider.VerifyCheckoutSignature("order_1", "pay_1", good));
        Assert.True(provider.VerifyCheckoutSignature("order_1", "pay_1", good.ToUpperInvariant()));
        Assert.False(provider.VerifyCheckoutSignature("order_1", "pay_2", good));
        Assert.False(provider.VerifyCheckoutSignature("order_2", "pay_1", good));
        Assert.False(provider.VerifyCheckoutSignature("order_1", "pay_1", ""));
        Assert.False(provider.VerifyCheckoutSignature("order_1", "pay_1", Sign("another-secret", "order_1|pay_1")));
    }

    [Fact]
    public void ASignatureIsNeverAccepted_WhenTheSecretIsEmpty()
    {
        var emptySecret = Sign("", "order_1|pay_1");
        Assert.False(RazorpayPaymentProvider.SignatureMatches("", "order_1|pay_1", emptySecret));
    }

    [Fact]
    public void Webhook_WithAGenuineSignature_IsReadIntoANeutralEvent()
    {
        var result = Provider().ParseWebhook(Captured, Headers(Captured));

        Assert.True(result.SignatureValid);
        var e = result.Event!;
        Assert.Equal("evt_1", e.ProviderEventId);
        Assert.Equal(PaymentEventType.PaymentSucceeded, e.Type);
        Assert.Equal("order_1", e.ProviderOrderId);
        Assert.Equal("pay_1", e.ProviderPaymentId);
        Assert.Equal(Money.Of(499m, "INR"), e.Amount);
    }

    [Fact]
    public void Webhook_WithABadOrMissingSignature_IsRefused()
    {
        var provider = Provider();
        Assert.False(provider.ParseWebhook(Captured, Headers(Captured, secret: "wrong")).SignatureValid);
        Assert.False(provider.ParseWebhook(Captured, new Dictionary<string, string>()).SignatureValid);
        Assert.False(provider.ParseWebhook(Captured + " ", Headers(Captured)).SignatureValid);
    }

    [Fact]
    public void Webhook_WithoutAnEventIdHeader_UsesAHashOfTheBody()
    {
        var a = Provider().ParseWebhook(Captured, Headers(Captured, eventId: null)).Event!.ProviderEventId;
        var b = Provider().ParseWebhook(Captured, Headers(Captured, eventId: null)).Event!.ProviderEventId;
        Assert.StartsWith("body:", a);
        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData("order.paid", PaymentEventType.PaymentSucceeded)]
    [InlineData("payment.failed", PaymentEventType.PaymentFailed)]
    [InlineData("refund.processed", PaymentEventType.RefundDone)]
    public void Webhook_EventNames_MapToNeutralTypes(string name, PaymentEventType expected)
    {
        var body = $"{{\"event\":\"{name}\",\"payload\":{{\"payment\":{{\"entity\":{{\"id\":\"pay_1\",\"order_id\":\"order_1\"}}}}}}}}";
        Assert.Equal(expected, Provider().ParseWebhook(body, Headers(body)).Event!.Type);
    }

    [Fact]
    public void Webhook_OfAnEventWeDoNotUse_IsGenuineButHasNothingToApply()
    {
        var body = "{\"event\":\"subscription.charged\",\"payload\":{}}";
        var result = Provider().ParseWebhook(body, Headers(body));
        Assert.True(result.SignatureValid);
        Assert.Null(result.Event);
    }

    [Fact]
    public void Webhook_WithAGenuineSignatureButUnreadableBody_HasNothingToApply()
    {
        var body = "not json";
        var result = Provider().ParseWebhook(body, Headers(body));
        Assert.True(result.SignatureValid);
        Assert.Null(result.Event);
    }

    [Fact]
    public async Task CreateCheckout_SendsMinorUnitsCurrencyAndOurReference_AndNoPersonalData()
    {
        _http.Response = "{\"id\":\"order_9\"}";

        var session = await Provider().CreateCheckoutAsync(new CheckoutRequest(Money.Of(499m, "INR"), "ref123"), default);

        Assert.Equal("order_9", session.ProviderOrderId);
        Assert.Equal(KeyId, session.PublicKeyId);
        Assert.Equal("Razorpay", session.Provider);
        var (request, body) = Assert.Single(_http.Requests);
        Assert.Equal("https://api.razorpay.com/v1/orders", request.RequestUri!.ToString());
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes($"{KeyId}:{KeySecret}")), request.Headers.Authorization.Parameter);
        Assert.Contains("\"amount\":49900", body);
        Assert.Contains("\"currency\":\"INR\"", body);
        Assert.Contains("\"receipt\":\"ref123\"", body);
        Assert.DoesNotContain("email", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCheckout_WhenTheProviderRefuses_ThrowsWithoutALeak()
    {
        _http.Status = HttpStatusCode.BadRequest;
        _http.Response = "{\"error\":{\"description\":\"details\"}}";

        var ex = await Assert.ThrowsAsync<PaymentProviderException>(() => Provider().CreateCheckoutAsync(new CheckoutRequest(Money.Of(1m, "INR"), "r"), default));

        Assert.DoesNotContain(KeySecret, ex.Message);
        Assert.Single(_http.Requests); // an order is never created twice
    }

    [Fact]
    public async Task CreateCheckout_WhenTheProviderCannotBeReached_ThrowsAProviderException()
    {
        _http.Throw = new HttpRequestException("down");
        await Assert.ThrowsAsync<PaymentProviderException>(() => Provider().CreateCheckoutAsync(new CheckoutRequest(Money.Of(1m, "INR"), "r"), default));
    }

    [Fact]
    public async Task CreateCheckout_WithoutAnOrderIdInTheAnswer_Throws()
    {
        _http.Response = "{}";
        await Assert.ThrowsAsync<PaymentProviderException>(() => Provider().CreateCheckoutAsync(new CheckoutRequest(Money.Of(1m, "INR"), "r"), default));
    }

    [Fact]
    public async Task Refund_SendsOnlyTheAmount_NotTheAdministratorsReason()
    {
        _http.Response = "{\"id\":\"rfnd_1\"}";

        var result = await Provider().RefundAsync("pay_1", Money.Of(100m, "INR"), "customer asked, name John", default);

        Assert.True(result.Success);
        Assert.Equal("rfnd_1", result.ProviderRefundId);
        var (request, body) = Assert.Single(_http.Requests);
        Assert.EndsWith("/v1/payments/pay_1/refund", request.RequestUri!.ToString());
        Assert.Equal("{\"amount\":10000}", body);
    }

    [Fact]
    public async Task Refund_WhenRefused_ReportsFailure()
    {
        _http.Status = HttpStatusCode.BadRequest;
        var result = await Provider().RefundAsync("pay_1", Money.Of(100m, "INR"), "x", default);
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}

public class Msg91SmsSenderTests
{
    private readonly FakeHttpHandler _http = new();

    private Msg91SmsSender Sender(string countryCode = "+91", bool withTemplate = true, string templateId = "tpl-77")
    {
        var values = new Dictionary<string, string?>
        {
            ["Sms:Msg91:AuthKey"] = "auth-key-value",
            ["Sms:DefaultCountryCode"] = countryCode,
        };
        if (withTemplate)
        {
            values["Sms:Templates:renewal-reminder:ProviderTemplateId"] = templateId;
            values["Sms:Templates:renewal-reminder:Text"] = "Your BannerAI plan for {#var#} ends on {#var#}.";
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new Msg91SmsSender(new HttpClient(_http), config, new SmsTemplateCatalog(config), NullLogger<Msg91SmsSender>.Instance);
    }

    [Fact]
    public async Task SendsTheRegisteredTemplate_ToANormalisedNumber_WithTheAuthKeyInAHeader()
    {
        _http.Response = "{\"type\":\"success\",\"message\":\"id\"}";

        var sent = await Sender().SendTemplateAsync("98765 43210", "renewal-reminder", new[] { "Gold", "1 Dec 2026" });

        Assert.True(sent);
        var (request, body) = Assert.Single(_http.Requests);
        Assert.Equal("https://control.msg91.com/api/v5/flow", request.RequestUri!.ToString());
        Assert.Equal("auth-key-value", request.Headers.GetValues("authkey").Single());
        Assert.Contains("\"template_id\":\"tpl-77\"", body);
        Assert.Contains("\"mobiles\":\"919876543210\"", body);
        Assert.Contains("\"var1\":\"Gold\"", body);
        Assert.Contains("\"var2\":\"1 Dec 2026\"", body);
    }

    [Fact]
    public async Task AnotherCountryIsOnlyASetting()
    {
        _http.Response = "{\"type\":\"success\"}";
        Assert.True(await Sender("+1").SendTemplateAsync("(415) 555-0100", "renewal-reminder", new[] { "a", "b" }));
        Assert.Contains("\"mobiles\":\"14155550100\"", _http.Requests.Single().body);
    }

    [Fact]
    public async Task FreeText_IsNeverSent()
    {
        Assert.False(await Sender().SendAsync("9876543210", "Any words"));
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task AnUnknownTemplate_OrOneWithoutAnId_IsNotSent()
    {
        Assert.False(await Sender().SendTemplateAsync("9876543210", "missing", Array.Empty<string>()));
        Assert.False(await Sender(templateId: "").SendTemplateAsync("9876543210", "renewal-reminder", new[] { "a", "b" }));
        Assert.False(await Sender(withTemplate: false).SendTemplateAsync("9876543210", "renewal-reminder", new[] { "a", "b" }));
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task AnInvalidNumber_IsNotSent()
    {
        Assert.False(await Sender().SendTemplateAsync("12", "renewal-reminder", new[] { "a", "b" }));
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task WhenTheProviderRefuses_ReportsFalse()
    {
        _http.Response = "{\"type\":\"error\",\"message\":\"bad template\"}";
        Assert.False(await Sender().SendTemplateAsync("9876543210", "renewal-reminder", new[] { "a", "b" }));

        _http.Status = HttpStatusCode.Unauthorized;
        _http.Response = "{\"type\":\"success\"}";
        Assert.False(await Sender().SendTemplateAsync("9876543210", "renewal-reminder", new[] { "a", "b" }));
    }

    [Fact]
    public async Task WhenTheProviderCannotBeReached_ReportsFalseWithoutThrowing()
    {
        _http.Throw = new HttpRequestException("down");
        Assert.False(await Sender().SendTemplateAsync("9876543210", "renewal-reminder", new[] { "a", "b" }));
    }
}

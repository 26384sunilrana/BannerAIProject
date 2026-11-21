namespace BannerService.Infrastructure.Services;

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Interfaces;
using Domain.ValueObjects;

/// <summary>
/// Razorpay behind the payment contract (ADR-004). Settings: Payments:Razorpay:KeyId, KeySecret, WebhookSecret and optionally ApiBase.
/// Written to Razorpay's published API (orders, payment signature, webhooks, refunds) and tested against fixed signatures and a fake
/// HTTP handler; the first check against a live or test account belongs to bolt 044. Order creation is not repeated on failure
/// because Razorpay would make a second order.
/// </summary>
public class RazorpayPaymentProvider : IPaymentProvider
{
    public const string ProviderName = "Razorpay";

    private readonly HttpClient _http;
    private readonly string _keyId;
    private readonly string _keySecret;
    private readonly string _webhookSecret;
    private readonly string _apiBase;
    private readonly ILogger<RazorpayPaymentProvider> _logger;

    public RazorpayPaymentProvider(HttpClient http, IConfiguration configuration, ILogger<RazorpayPaymentProvider> logger)
    {
        _http = http;
        _logger = logger;
        _keyId = configuration["Payments:Razorpay:KeyId"] ?? string.Empty;
        _keySecret = configuration["Payments:Razorpay:KeySecret"] ?? string.Empty;
        _webhookSecret = configuration["Payments:Razorpay:WebhookSecret"] ?? string.Empty;
        var apiBase = configuration["Payments:Razorpay:ApiBase"];
        _apiBase = (string.IsNullOrWhiteSpace(apiBase) ? "https://api.razorpay.com" : apiBase).TrimEnd('/');
        if (_http.Timeout == Timeout.InfiniteTimeSpan || _http.Timeout > TimeSpan.FromSeconds(10))
            _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public string Name => ProviderName;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_keyId) && !string.IsNullOrWhiteSpace(_keySecret) && !string.IsNullOrWhiteSpace(_webhookSecret);

    public async Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new
        {
            amount = request.Amount.ToMinorUnits(),
            currency = request.Amount.Currency,
            receipt = request.Reference,
            payment_capture = 1,
        });

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase}/v1/orders")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        message.Headers.Authorization = BasicAuth();

        using var response = await SendAsync(message, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Razorpay refused to create an order: HTTP {Status}", (int)response.StatusCode);
            throw new PaymentProviderException($"Razorpay answered HTTP {(int)response.StatusCode} when creating an order");
        }

        string? orderId;
        try
        {
            using var doc = JsonDocument.Parse(json);
            orderId = doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
        }
        catch (JsonException ex)
        {
            throw new PaymentProviderException("Razorpay's answer could not be read", ex);
        }

        if (string.IsNullOrEmpty(orderId))
            throw new PaymentProviderException("Razorpay's answer had no order id");

        return new CheckoutSession(Name, orderId, request.Amount, _keyId);
    }

    /// <summary>Razorpay signs "order_id|payment_id" with the key secret (hex HMAC-SHA256).</summary>
    public bool VerifyCheckoutSignature(string providerOrderId, string providerPaymentId, string signature) =>
        SignatureMatches(_keySecret, $"{providerOrderId}|{providerPaymentId}", signature);

    public WebhookParseResult ParseWebhook(string rawBody, IReadOnlyDictionary<string, string> headers)
    {
        headers.TryGetValue("x-razorpay-signature", out var signature);
        if (!SignatureMatches(_webhookSecret, rawBody, signature))
            return new WebhookParseResult(false, null);

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            var name = root.TryGetProperty("event", out var e) ? e.GetString() : null;

            var type = name switch
            {
                "payment.captured" or "order.paid" => (PaymentEventType?)PaymentEventType.PaymentSucceeded,
                "payment.failed" => PaymentEventType.PaymentFailed,
                "refund.processed" => PaymentEventType.RefundDone,
                _ => null,
            };
            if (type == null)
                return new WebhookParseResult(true, null);

            var payment = Entity(root, "payment");
            var orderEntity = Entity(root, "order");
            var orderId = Text(payment, "order_id") ?? Text(orderEntity, "id");
            var paymentId = Text(payment, "id");

            Money? amount = null;
            var currency = Text(payment, "currency") ?? Text(orderEntity, "currency");
            var minor = payment is { } p && p.TryGetProperty("amount", out var a) && a.TryGetInt64(out var v) ? v
                : orderEntity is { } o && o.TryGetProperty("amount_paid", out var ap) && ap.TryGetInt64(out var w) ? w
                : (long?)null;
            if (minor != null && CurrencyCodes.IsValid(currency))
                amount = Money.FromMinorUnits(minor.Value, currency!);

            // Razorpay sends a unique id for every delivery of the same event in this header
            var eventId = headers.TryGetValue("x-razorpay-event-id", out var header) && !string.IsNullOrWhiteSpace(header)
                ? header
                : "body:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody)));

            var reason = type == PaymentEventType.PaymentFailed ? Text(payment, "error_code") ?? "payment_failed" : null;
            return new WebhookParseResult(true, new PaymentEvent(eventId, type.Value, orderId, paymentId, amount, reason));
        }
        catch (JsonException)
        {
            // genuine signature but unreadable body: nothing to apply
            return new WebhookParseResult(true, null);
        }
    }

    public async Task<ProviderRefundResult> RefundAsync(string providerPaymentId, Money amount, string reason, CancellationToken cancellationToken)
    {
        // The administrator's reason stays with us: nothing free-form goes to the provider
        var body = JsonSerializer.Serialize(new { amount = amount.ToMinorUnits() });
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase}/v1/payments/{Uri.EscapeDataString(providerPaymentId)}/refund")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        message.Headers.Authorization = BasicAuth();

        using var response = await SendAsync(message, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Razorpay refused a refund: HTTP {Status}", (int)response.StatusCode);
            return new ProviderRefundResult(false, null, $"Razorpay answered HTTP {(int)response.StatusCode}");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var id = doc.RootElement.TryGetProperty("id", out var el) ? el.GetString() : null;
            return new ProviderRefundResult(true, id, null);
        }
        catch (JsonException)
        {
            return new ProviderRefundResult(true, null, null);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        try
        {
            return await _http.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new PaymentProviderException("Razorpay could not be reached", ex);
        }
    }

    private AuthenticationHeaderValue BasicAuth() =>
        new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}")));

    /// <summary>Hex HMAC-SHA256 compared in constant time. An empty secret or signature never matches.</summary>
    public static bool SignatureMatches(string secret, string payload, string? signature)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signature))
            return false;
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant()));
    }

    private static JsonElement? Entity(JsonElement root, string name) =>
        root.TryGetProperty("payload", out var payload) && payload.TryGetProperty(name, out var holder) && holder.TryGetProperty("entity", out var entity)
            ? entity
            : null;

    private static string? Text(JsonElement? element, string property) =>
        element is { } e && e.ValueKind == JsonValueKind.Object && e.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

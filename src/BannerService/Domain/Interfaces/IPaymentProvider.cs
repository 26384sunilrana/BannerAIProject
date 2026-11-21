namespace BannerService.Domain.Interfaces;

using ValueObjects;

public enum PaymentEventType
{
    PaymentSucceeded = 1,
    PaymentFailed = 2,
    RefundDone = 3,
}

/// <summary>A provider's webhook turned into a neutral form, so the rest of the application never sees a provider's own format.</summary>
public sealed record PaymentEvent(
    string ProviderEventId,
    PaymentEventType Type,
    string? ProviderOrderId,
    string? ProviderPaymentId,
    Money? Amount,
    string? FailureReason = null);

/// <summary>What to charge. Reference is our own identifier for the payment (it is shown on the provider's side); it never carries names or addresses.</summary>
public sealed record CheckoutRequest(Money Amount, string Reference);

/// <summary>What the browser needs to open the provider's own payment form. PublicKeyId is safe to show in a page.</summary>
public sealed record CheckoutSession(string Provider, string ProviderOrderId, Money Amount, string PublicKeyId);

public sealed record ProviderRefundResult(bool Success, string? ProviderRefundId, string? Error);

public sealed record WebhookParseResult(bool SignatureValid, PaymentEvent? Event);

/// <summary>The provider could not be reached or refused the request. The message is safe to log and never holds a secret.</summary>
public class PaymentProviderException : Exception
{
    public PaymentProviderException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// The contract between the application and a company that takes payments (ADR-004). One implementation per provider.
/// Card details are entered on the provider's own form and never reach this application.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>The name used in settings (Payments:Provider), for example Razorpay.</summary>
    string Name { get; }

    /// <summary>True only when every setting the provider needs has a value.</summary>
    bool IsConfigured { get; }

    Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken);

    /// <summary>Checks what the browser got back from the provider's payment form (order, payment, signature).</summary>
    bool VerifyCheckoutSignature(string providerOrderId, string providerPaymentId, string signature);

    /// <summary>Checks the signature of a webhook and, when it is genuine, reads it. Headers are keyed in lower case.</summary>
    WebhookParseResult ParseWebhook(string rawBody, IReadOnlyDictionary<string, string> headers);

    Task<ProviderRefundResult> RefundAsync(string providerPaymentId, Money amount, string reason, CancellationToken cancellationToken);
}

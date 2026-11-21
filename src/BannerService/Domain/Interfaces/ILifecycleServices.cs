namespace BannerService.Domain.Interfaces;

using Entities;

public interface ISmsSender
{
    /// <summary>Sends a text message. Returns false (without throwing) when it could not be sent.</summary>
    Task<bool> SendAsync(string phoneNumber, string message);

    /// <summary>
    /// Sends a registered message (in India the sender and every text must be registered, so free text is not allowed). Values fill the
    /// template's gaps in order. Returns false (without throwing) when the template is unknown or the message could not be sent.
    /// </summary>
    Task<bool> SendTemplateAsync(string phoneNumber, string templateKey, IReadOnlyList<string> values);
}

/// <summary>A registered text message with gaps. ProviderTemplateId is the provider's (in India, the DLT) identifier.</summary>
public sealed record SmsTemplate(string Key, string ProviderTemplateId, string Text, IReadOnlyList<string> VariableNames);

public interface ISmsTemplateCatalog
{
    /// <summary>Looks a template up by key (renewal-reminder, plan-ended, payment-received). Null when it is not defined.</summary>
    SmsTemplate? Find(string key);

    /// <summary>The template's text with the values put into its gaps; used for logs and the test screen.</summary>
    string Render(SmsTemplate template, IReadOnlyList<string> values);
}

public class PaymentResult
{
    public bool Success { get; init; }
    public string? Reference { get; init; }
    public string? Error { get; init; }
}

/// <summary>Takes the money for an automatic renewal.</summary>
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(Subscription subscription, decimal amount);
}

public interface ISubscriptionNotificationRepository
{
    /// <summary>
    /// Claims the right to send this message today. Returns false when it was already sent (or claimed by
    /// another server), in which case the caller must not send it.
    /// </summary>
    Task<bool> TryClaimAsync(SubscriptionNotification notification);

    Task MarkResultAsync(Guid notificationId, bool delivered, string? error);
}

namespace BannerService.Domain.Interfaces;

using Entities;

public interface ISmsSender
{
    /// <summary>Sends a text message. Returns false (without throwing) when it could not be sent.</summary>
    Task<bool> SendAsync(string phoneNumber, string message);
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

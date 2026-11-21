namespace BannerService.Domain.Interfaces;

using Entities;

public interface IPaymentRepository
{
    Task<PaymentAttempt> AddAttemptAsync(PaymentAttempt attempt);
    Task<PaymentAttempt?> GetAttemptAsync(Guid attemptId);
    Task<PaymentAttempt?> GetAttemptByOrderIdAsync(string provider, string providerOrderId);
    Task<List<PaymentAttempt>> ListForShopAsync(Guid shopId);

    /// <summary>Saves changes to an attempt. Throws <see cref="PaymentConcurrencyException"/> when someone else changed it first.</summary>
    Task UpdateAttemptAsync(PaymentAttempt attempt);

    /// <summary>The refund is saved together with the attempt it belongs to.</summary>
    Task AddRefundAsync(Refund refund);

    /// <summary>
    /// Records that this webhook event arrived. Returns the receipt and whether it was already applied
    /// (then the caller must do nothing). A receipt that was recorded but never applied is returned as not applied, so it is retried.
    /// </summary>
    Task<(WebhookReceipt receipt, bool alreadyProcessed)> RecordReceiptAsync(string provider, string providerEventId, string eventType);

    Task UpdateReceiptAsync(WebhookReceipt receipt);

    /// <summary>Removes attempts still Created after 7 days and receipts older than 180 days. Returns how many rows went.</summary>
    Task<int> CleanUpAsync(DateTime nowUtc);
}

/// <summary>Two requests tried to change the same payment at once; the second one must treat the payment as already handled.</summary>
public class PaymentConcurrencyException : Exception
{
    public PaymentConcurrencyException(Exception inner) : base("The payment was changed by another request", inner) { }
}

namespace BannerService.Domain.Entities;

public enum PaymentAttemptStatus
{
    Created = 1,
    Paid = 2,
    Failed = 3,
    PartlyRefunded = 4,
    Refunded = 5,
}

public enum RefundStatus
{
    Requested = 1,
    Done = 2,
    Failed = 3,
}

/// <summary>One try to pay for a renewal through a payment provider. Paid and Failed are final.</summary>
public class PaymentAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The invoice is made when the payment succeeds, so it is empty until then.</summary>
    public Guid? InvoiceId { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid ShopId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderOrderId { get; set; } = string.Empty;

    /// <summary>Stored encrypted.</summary>
    public string? ProviderPaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public PaymentAttemptStatus Status { get; set; } = PaymentAttemptStatus.Created;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    /// <summary>Stops two deliveries of the same payment from both renewing the plan.</summary>
    public byte[]? RowVersion { get; set; }

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public bool IsPaid => Status is PaymentAttemptStatus.Paid or PaymentAttemptStatus.PartlyRefunded or PaymentAttemptStatus.Refunded;

    public decimal RefundedAmount => Refunds.Where(r => r.Status == RefundStatus.Done).Sum(r => r.Amount);

    public decimal RefundableAmount => IsPaid ? Amount - RefundedAmount : 0;

    public void MarkPaid(string? providerPaymentId, Guid invoiceId)
    {
        if (IsPaid)
            return;
        if (Status != PaymentAttemptStatus.Created)
            throw new InvalidOperationException($"A {Status} payment cannot be marked paid");
        Status = PaymentAttemptStatus.Paid;
        ProviderPaymentId = providerPaymentId;
        InvoiceId = invoiceId;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        if (Status != PaymentAttemptStatus.Created)
            return;
        Status = PaymentAttemptStatus.Failed;
        FailureReason = reason.Length > 256 ? reason[..256] : reason;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>Records a finished refund and moves the payment to PartlyRefunded or Refunded.</summary>
    public void ApplyRefund(Refund refund)
    {
        if (!IsPaid)
            throw new InvalidOperationException("Only a paid payment can be refunded");
        if (refund.Amount <= 0 || refund.Amount > RefundableAmount)
            throw new InvalidOperationException("The refund is more than what is left to refund");
        refund.Status = RefundStatus.Done;
        Refunds.Add(refund);
        Status = RefundableAmount <= 0 ? PaymentAttemptStatus.Refunded : PaymentAttemptStatus.PartlyRefunded;
    }
}

public class Refund
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentAttemptId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Reason { get; set; } = string.Empty;
    public Guid RequestedByUserId { get; set; }
    public string? ProviderRefundId { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Requested;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Proof that a provider's webhook was seen. (Provider, ProviderEventId) is unique, so a repeat is recognised.</summary>
public class WebhookReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = string.Empty;
    public string ProviderEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Empty until the event has been applied, so a delivery that failed halfway is tried again when the provider repeats it.</summary>
    public DateTime? ProcessedAt { get; set; }
    public string? Outcome { get; set; }
}

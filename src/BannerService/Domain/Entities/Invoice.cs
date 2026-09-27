namespace BannerService.Domain.Entities
{
    public class Invoice
    {
        public Guid Id { get; set; }
        public Guid SubscriptionId { get; set; }
        public virtual Subscription? Subscription { get; set; }
        public Guid ShopId { get; set; }
        public virtual Shop? Shop { get; set; }

        public decimal Amount { get; set; }
        public InvoiceStatus Status { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;

        // Dates
        public DateTime IssuedDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }

        // Details
        public string Description { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public string? StripeInvoiceId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Helper properties
        public bool IsPaid => Status == InvoiceStatus.Paid;
        public bool IsDue => DueDate <= DateTime.UtcNow && !IsPaid;
        public bool IsOverdue => IsDue && Status != InvoiceStatus.Cancelled;
        public int DaysOverdue => IsOverdue ? (int)(DateTime.UtcNow - DueDate).TotalDays : 0;
        public decimal AmountDue => IsPaid ? 0 : Amount;

        public void MarkAsPaid(string? paymentReference = null)
        {
            Status = InvoiceStatus.Paid;
            PaidDate = DateTime.UtcNow;
            PaymentReference = paymentReference;
        }

        public void MarkAsOverdue()
        {
            if (!IsPaid)
                Status = InvoiceStatus.Overdue;
        }

        public void Cancel()
        {
            Status = InvoiceStatus.Cancelled;
        }

        public void Refund()
        {
            Status = InvoiceStatus.Refunded;
        }

        public string GetStatusDisplay() =>
            Status switch
            {
                InvoiceStatus.Draft => "Draft",
                InvoiceStatus.Issued => "Issued",
                InvoiceStatus.Overdue => "Overdue",
                InvoiceStatus.Paid => "Paid",
                InvoiceStatus.Cancelled => "Cancelled",
                InvoiceStatus.Refunded => "Refunded",
                _ => "Unknown"
            };
    }

    public enum InvoiceStatus
    {
        Draft = 1,
        Issued = 2,
        Overdue = 3,
        Paid = 4,
        Cancelled = 5,
        Refunded = 6
    }
}

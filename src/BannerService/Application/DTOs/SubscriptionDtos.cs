namespace BannerService.Application.DTOs
{
    using BannerService.Domain.Entities;

    public class CreateSubscriptionDto
    {
        public Guid ShopId { get; set; }
        public Guid PlanId { get; set; }
        public BillingPeriod BillingPeriod { get; set; }
        public string? PaymentMethodId { get; set; }
        public int? TrialDays { get; set; }
    }

    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public Guid PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }
        public BillingPeriod BillingPeriod { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime RenewalDate { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public DateTime? CancellationDate { get; set; }
        public int PaymentFailureCount { get; set; }
        public DateTime? LastPaymentAttempt { get; set; }
        public bool AutoRenew { get; set; }
        /// <summary>End of the grace week after an unpaid renewal date; logins are switched off then.</summary>
        public DateTime? GraceEndsAt { get; set; }
        public Guid? PendingPlanId { get; set; }
        public DateTime? PendingPlanEffectiveAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public SubscriptionFeaturesDto Features { get; set; } = new();
    }

    public class SubscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public decimal AnnualPrice { get; set; }
        public SubscriptionFeaturesDto Features { get; set; } = new();
        public bool IsPopular { get; set; }
        public bool IsActive { get; set; }
    }

    public class SubscriptionFeaturesDto
    {
        public int MaxBanners { get; set; }
        public int MaxShops { get; set; }
        public int MaxUsers { get; set; }
        public int MaxStorageGB { get; set; }
        public bool ApiAccess { get; set; }
        public bool CustomDomain { get; set; }
        public bool AdvancedAnalytics { get; set; }
        public bool DedicatedSupport { get; set; }
        public decimal SlaPercentage { get; set; }
        public bool PriorityQueue { get; set; }
    }

    public class InvoiceDto
    {
        public Guid Id { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid ShopId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ChangePlanDto
    {
        public Guid NewPlanId { get; set; }
    }

    public class SetAutoRenewDto
    {
        public bool AutoRenew { get; set; }
    }

    public class ChangeBillingPeriodDto
    {
        public BillingPeriod NewPeriod { get; set; }
    }

    public class RetryPaymentDto
    {
        public string? PaymentMethodId { get; set; }
    }

    public class SubscriptionSummaryDto
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }
        public DateTime RenewalDate { get; set; }
        public int DaysUntilRenewal { get; set; }
        public bool IsRenewingSoon { get; set; }
    }
}

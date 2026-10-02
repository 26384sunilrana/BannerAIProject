namespace BannerService.Domain.Entities
{
    public class Subscription
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public virtual Shop? Shop { get; set; }
        public Guid PlanId { get; set; }
        public virtual SubscriptionPlan? Plan { get; set; }
        public SubscriptionStatus Status { get; set; }
        public BillingPeriod BillingPeriod { get; set; }

        // Dates
        public DateTime StartDate { get; set; }
        public DateTime RenewalDate { get; set; }
        public DateTime? CancellationDate { get; set; }
        public DateTime? TrialEndDate { get; set; }

        // Payment tracking
        public decimal CurrentPrice { get; set; }
        public int PaymentFailureCount { get; set; }
        public DateTime? LastPaymentAttempt { get; set; }
        public string? PaymentMethodId { get; set; }

        // Plan changes
        public Guid? PreviousPlanId { get; set; }
        public DateTime? PlanChangedAt { get; set; }

        // Auto renewal and scheduled plan change (effective from the next day)
        public bool AutoRenew { get; set; } = true;
        // After the renewal date passes unpaid the shop has a week to renew before logins are switched off
        public const int GraceDays = 7;
        public DateTime? GraceEndsAt { get; set; }

        public Guid? PendingPlanId { get; set; }
        public decimal? PendingPrice { get; set; }
        public DateTime? PendingPlanEffectiveAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Helper properties
        public bool IsOnTrial => TrialEndDate.HasValue && DateTime.UtcNow < TrialEndDate;
        public bool IsActive => Status == SubscriptionStatus.Active;
        public bool IsExpired => RenewalDate < DateTime.UtcNow && !IsActive;
        public bool IsInGracePeriod => Status == SubscriptionStatus.GracePeriod;
        public int DaysUntilRenewal => (int)Math.Ceiling((RenewalDate - DateTime.UtcNow).TotalDays);
        public bool IsRenewingSoon => DaysUntilRenewal <= 7 && DaysUntilRenewal > 0;
        public bool NeedsRenewalToday => RenewalDate.Date == DateTime.UtcNow.Date;

        public void SchedulePlanChange(Guid newPlanId, decimal newPrice, DateTime now)
        {
            PendingPlanId = newPlanId;
            PendingPrice = newPrice;
            PendingPlanEffectiveAt = now.Date.AddDays(1);
            UpdatedAt = now;
        }

        public bool ApplyPendingPlanChange(DateTime now)
        {
            if (PendingPlanId == null || PendingPlanEffectiveAt == null || PendingPlanEffectiveAt > now)
                return false;

            PreviousPlanId = PlanId;
            PlanId = PendingPlanId.Value;
            CurrentPrice = PendingPrice ?? CurrentPrice;
            PlanChangedAt = now;
            PendingPlanId = null;
            PendingPrice = null;
            PendingPlanEffectiveAt = null;
            UpdatedAt = now;
            return true;
        }

        public void SetStatus(SubscriptionStatus status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void RecordPaymentAttempt()
        {
            LastPaymentAttempt = DateTime.UtcNow;
            PaymentFailureCount++;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ResetPaymentFailures()
        {
            PaymentFailureCount = 0;
            LastPaymentAttempt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetRenewalDate(DateTime date)
        {
            RenewalDate = date;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangePlan(Guid newPlanId, decimal newPrice)
        {
            PreviousPlanId = PlanId;
            PlanId = newPlanId;
            CurrentPrice = newPrice;
            PlanChangedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            SetStatus(SubscriptionStatus.Cancelled);
            CancellationDate = DateTime.UtcNow;
        }

        public void MoveToGracePeriod()
        {
            SetStatus(SubscriptionStatus.GracePeriod);
        }

        /// <summary>The renewal date has passed without payment: a week of grace starts from that date.</summary>
        public void EnterGracePeriod()
        {
            GraceEndsAt = RenewalDate.AddDays(GraceDays);
            MoveToGracePeriod();
        }

        /// <summary>Paid for another period starting at <paramref name="from"/>; any grace period is over.</summary>
        public void RenewFrom(DateTime from)
        {
            RenewalDate = BillingPeriod.AddPeriod(from);
            GraceEndsAt = null;
            PaymentFailureCount = 0;
            LastPaymentAttempt = null;
            SetStatus(SubscriptionStatus.Active);
        }

        /// <summary>True once the grace week is over or the subscription was suspended.</summary>
        public bool IsLocked => Status == SubscriptionStatus.Expired || Status == SubscriptionStatus.Suspended;

        /// <summary>True when the shop must show only its own default banner.</summary>
        public bool ShowsDefaultBannerOnly =>
            Status == SubscriptionStatus.GracePeriod || Status == SubscriptionStatus.Expired || Status == SubscriptionStatus.Suspended;

        public void Expire()
        {
            SetStatus(SubscriptionStatus.Expired);
        }

        public void MarkAsPaymentFailed()
        {
            SetStatus(SubscriptionStatus.PaymentFailed);
            RecordPaymentAttempt();
        }

        public void Suspend()
        {
            SetStatus(SubscriptionStatus.Suspended);
        }
    }

    public enum SubscriptionStatus
    {
        Trial = 1,
        Active = 2,
        RenewalPending = 3,
        PaymentFailed = 4,
        Suspended = 5,
        GracePeriod = 6,
        Expired = 7,
        Cancelled = 8
    }
}

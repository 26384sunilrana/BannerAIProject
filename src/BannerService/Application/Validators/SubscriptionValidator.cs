namespace BannerService.Application.Validators
{
    using BannerService.Domain.Entities;

    public class SubscriptionValidator
    {
        private const decimal MinPrice = 0.01m;
        private const decimal MaxPrice = 100000m;
        private const int MinTrialDays = 0;
        private const int MaxTrialDays = 90;

        public static (bool isValid, string? errorMessage) ValidateCreateSubscription(
            Guid planId,
            BillingPeriod billingPeriod,
            int? trialDays)
        {
            if (planId == Guid.Empty)
                return (false, "Plan ID is required");

            if (billingPeriod != BillingPeriod.Monthly && billingPeriod != BillingPeriod.Annual)
                return (false, "Invalid billing period");

            if (trialDays.HasValue && (trialDays < MinTrialDays || trialDays > MaxTrialDays))
                return (false, $"Trial days must be between {MinTrialDays} and {MaxTrialDays}");

            return (true, null);
        }

        public static (bool isValid, string? errorMessage) ValidatePlanPrice(
            decimal monthlyPrice,
            decimal annualPrice)
        {
            if (monthlyPrice < MinPrice || monthlyPrice > MaxPrice)
                return (false, $"Monthly price must be between {MinPrice} and {MaxPrice}");

            if (annualPrice < MinPrice || annualPrice > MaxPrice)
                return (false, $"Annual price must be between {MinPrice} and {MaxPrice}");

            if (annualPrice < monthlyPrice * 11)
                return (false, "Annual price should be at least 11x the monthly price");

            return (true, null);
        }

        public static (bool isValid, string? errorMessage) ValidatePlanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Plan name is required");

            if (name.Length < 2 || name.Length > 50)
                return (false, "Plan name must be between 2 and 50 characters");

            return (true, null);
        }

        public static (bool isValid, string? errorMessage) ValidateFeatures(
            SubscriptionFeatures features)
        {
            if (features.MaxBanners < 0)
                return (false, "Max banners cannot be negative");

            if (features.MaxShops < 1)
                return (false, "Max shops must be at least 1");

            if (features.MaxUsers < 1)
                return (false, "Max users must be at least 1");

            if (features.MaxStorageGB < 0)
                return (false, "Max storage cannot be negative");

            if (features.SlaPercentage < 99.0m || features.SlaPercentage > 100.0m)
                return (false, "SLA percentage must be between 99.0 and 100.0");

            return (true, null);
        }

        public static (bool isValid, string? errorMessage) ValidatePlanUpgrade(
            SubscriptionPlan currentPlan,
            SubscriptionPlan newPlan)
        {
            if (currentPlan.Id == newPlan.Id)
                return (false, "Cannot upgrade to the same plan");

            if (GetPlanTier(currentPlan.Name) >= GetPlanTier(newPlan.Name))
                return (false, "New plan must be a higher tier");

            return (true, null);
        }

        public static (bool isValid, string? errorMessage) ValidatePlanDowngrade(
            SubscriptionPlan currentPlan,
            SubscriptionPlan newPlan)
        {
            if (currentPlan.Id == newPlan.Id)
                return (false, "Cannot downgrade to the same plan");

            if (GetPlanTier(currentPlan.Name) <= GetPlanTier(newPlan.Name))
                return (false, "New plan must be a lower tier");

            return (true, null);
        }

        public static (bool isValid, string? errorMessage) ValidatePaymentMethodId(
            string? paymentMethodId)
        {
            if (string.IsNullOrWhiteSpace(paymentMethodId))
                return (false, "Payment method ID is required for paid subscriptions");

            if (paymentMethodId.Length < 3 || paymentMethodId.Length > 256)
                return (false, "Invalid payment method ID format");

            return (true, null);
        }

        public static (bool isValid, string? errorMessage) ValidateSubscriptionCancellation(
            Subscription subscription)
        {
            if (subscription.Status == SubscriptionStatus.Cancelled)
                return (false, "Subscription is already cancelled");

            if (subscription.Status == SubscriptionStatus.Expired)
                return (false, "Cannot cancel an expired subscription");

            return (true, null);
        }

        public static (bool isValid, string? errorMessage) ValidateInvoicePayment(
            Invoice invoice)
        {
            if (invoice.IsPaid)
                return (false, "Invoice is already paid");

            if (invoice.Status == InvoiceStatus.Cancelled)
                return (false, "Cannot pay a cancelled invoice");

            if (invoice.Status == InvoiceStatus.Refunded)
                return (false, "Cannot pay a refunded invoice");

            return (true, null);
        }

        private static int GetPlanTier(string planName) =>
            planName switch
            {
                "Basic" => 1,
                "Silver" => 2,
                "Gold" => 3,
                "Platinum" => 4,
                _ => 0
            };
    }
}

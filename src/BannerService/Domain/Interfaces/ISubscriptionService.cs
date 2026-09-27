namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface ISubscriptionService
    {
        // Subscription Plans
        Task<SubscriptionPlan?> GetPlanByIdAsync(Guid planId);
        Task<List<SubscriptionPlan>> GetAllPlansAsync();
        Task<SubscriptionPlan?> GetPlanByNameAsync(string name);

        // Subscription Queries
        Task<Subscription?> GetSubscriptionByIdAsync(Guid subscriptionId);
        Task<Subscription?> GetCurrentSubscriptionAsync(Guid shopId);
        Task<List<Subscription>> GetShopSubscriptionsAsync(Guid shopId);
        Task<List<Subscription>> GetExpiringSubscriptionsAsync();
        Task<List<Subscription>> GetRenewingSubscriptionsAsync();

        // Subscription Operations
        Task<(bool success, string message, Subscription? subscription)> CreateSubscriptionAsync(
            Guid shopId,
            Guid planId,
            BillingPeriod billingPeriod,
            string? paymentMethodId,
            int? trialDays,
            Guid createdByUserId);

        Task<(bool success, string message)> UpgradePlanAsync(
            Guid subscriptionId,
            Guid newPlanId);

        Task<(bool success, string message)> DowngradePlanAsync(
            Guid subscriptionId,
            Guid newPlanId);

        Task<(bool success, string message)> ChangeBillingPeriodAsync(
            Guid subscriptionId,
            BillingPeriod newPeriod);

        Task<(bool success, string message)> CancelSubscriptionAsync(
            Guid subscriptionId);

        Task<(bool success, string message)> RenewSubscriptionAsync(
            Guid subscriptionId);

        // Invoice Operations
        Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId);
        Task<List<Invoice>> GetShopInvoicesAsync(Guid shopId);
        Task<List<Invoice>> GetSubscriptionInvoicesAsync(Guid subscriptionId);

        // Renewal/Billing
        Task<(bool success, string message)> ProcessRenewalAsync(Guid subscriptionId);
        Task<(bool success, string message)> RetryPaymentAsync(Guid invoiceId);
        Task<decimal> CalculateProratedAmountAsync(
            Guid subscriptionId,
            Guid newPlanId);
    }
}

namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByIdAsync(Guid subscriptionId);
        Task<Subscription?> GetByShopIdAsync(Guid shopId);
        Task<List<Subscription>> GetActiveByShopIdAsync(Guid shopId);
        Task<List<Subscription>> GetByStatusAsync(SubscriptionStatus status);
        Task<List<Subscription>> GetByPlanIdAsync(Guid planId);
        Task<List<Subscription>> GetExpiringTodayAsync();
        Task<List<Subscription>> GetRenewingSoonAsync(int daysThreshold = 7);
        Task<List<Subscription>> GetWithPaymentFailuresAsync();
        Task<List<Subscription>> GetInGracePeriodAsync();
        Task<List<Subscription>> GetAllAsync();
        /// <summary>Platform-wide listing for admins, newest renewal date first.</summary>
        Task<(List<Subscription> items, int total)> GetPagedAsync(SubscriptionStatus? status, string? shopSearch, int page, int pageSize);
        Task<int> GetCountByStatusAsync(SubscriptionStatus status);
        Task<int> GetCountAsync();
        Task<Subscription> CreateAsync(Subscription subscription);
        Task<Subscription> UpdateAsync(Subscription subscription);
        Task<bool> DeleteAsync(Guid subscriptionId);
        Task<bool> ExistsAsync(Guid subscriptionId);
    }
}

namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface ISubscriptionPlanRepository
    {
        Task<SubscriptionPlan?> GetByIdAsync(Guid planId);
        Task<SubscriptionPlan?> GetByNameAsync(string name);
        Task<List<SubscriptionPlan>> GetAllAsync();
        Task<List<SubscriptionPlan>> GetActiveAsync();
        Task<int> GetCountAsync();
        Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan);
        Task<SubscriptionPlan> UpdateAsync(SubscriptionPlan plan);
        Task<bool> DeleteAsync(Guid planId);
        Task<bool> ExistsAsync(Guid planId);
    }
}

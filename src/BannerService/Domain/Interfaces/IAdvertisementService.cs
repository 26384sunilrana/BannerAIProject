namespace BannerService.Domain.Interfaces
{
    using Domain.Entities;

    public interface IAdvertisementService
    {
        Task<Advertisement> CreateAdAsync(Guid shopId, string title, string description, AdType type, Guid userId);
        Task<Advertisement> GetAdAsync(Guid adId);
        Task<List<Advertisement>> GetShopAdsAsync(Guid shopId);
        Task<List<Advertisement>> GetActiveAdsAsync();
        Task<Advertisement> UpdateAdAsync(Guid adId, string title, string description, Guid userId);
        Task<Advertisement> SetAdTargetAsync(Guid adId, Domain.ValueObjects.AdTarget target, Guid userId);
        Task<Advertisement> SetBudgetAsync(Guid adId, decimal budgetLimit, decimal dailyBudget, Guid userId);
        Task<Advertisement> SetDatesAsync(Guid adId, DateTime startDate, DateTime endDate, Guid userId);
        Task<Advertisement> ActivateAdAsync(Guid adId, Guid userId);
        Task<Advertisement> PauseAdAsync(Guid adId, Guid userId);
        Task<Advertisement> CompleteAdAsync(Guid adId, Guid userId);
        Task<Advertisement> CancelAdAsync(Guid adId, Guid userId);
        Task<Advertisement> ArchiveAdAsync(Guid adId, Guid userId);
        Task<bool> DeleteAdAsync(Guid adId, Guid userId);
        Task<List<Advertisement>> GetExpiringAdsAsync(int daysThreshold = 7);
        Task UpdateMetricsAsync(Guid adId, long impressions, long clicks, long conversions, decimal spend);
        Task<decimal> CalculateROIAsync(Guid adId);
        Task<List<Advertisement>> SearchAdsAsync(Guid shopId, string searchTerm);
    }
}

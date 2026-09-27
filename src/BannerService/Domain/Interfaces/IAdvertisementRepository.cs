namespace BannerService.Domain.Interfaces
{
    using Domain.Entities;

    public interface IAdvertisementRepository
    {
        Task<Advertisement?> GetByIdAsync(Guid adId);
        Task<List<Advertisement>> GetByShopIdAsync(Guid shopId);
        Task<List<Advertisement>> GetByStatusAsync(AdStatus status);
        Task<List<Advertisement>> GetActiveAdsAsync();
        Task<List<Advertisement>> GetByCreatorAsync(Guid userId);
        Task<List<Advertisement>> GetExpiringAdsAsync(int daysThreshold = 7);
        Task<List<Advertisement>> GetAllAsync();
        Task<Advertisement> CreateAsync(Advertisement ad);
        Task<Advertisement> UpdateAsync(Advertisement ad);
        Task<bool> DeleteAsync(Guid adId);
        Task<List<Advertisement>> GetByTypeAsync(AdType type);
        Task<List<Advertisement>> GetAdsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> GetCountByShopAsync(Guid shopId);
        Task<int> GetCountByStatusAsync(AdStatus status);
    }
}

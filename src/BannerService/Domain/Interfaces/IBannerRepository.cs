namespace BannerService.Domain.Interfaces;

using Entities;

public interface IBannerRepository
{
    Task<Banner> CreateAsync(Banner banner);
    Task<Banner?> GetByIdAsync(Guid bannerId, Guid shopId);
    Task<List<Banner>> GetAllByShopAsync(Guid shopId, int page = 1, int pageSize = 20);
    Task<List<Banner>> GetScheduledByShopAsync(Guid shopId);
    /// <summary>Saves layer changes, including swaps, which a unique (banner, layer) index would otherwise reject.</summary>
    Task SaveLayerChangesAsync(Banner banner);
    Task<Banner> UpdateAsync(Banner banner);
    Task<bool> DeleteAsync(Guid bannerId, Guid shopId);
    Task<bool> ExistsAsync(Guid bannerId, Guid shopId);
}

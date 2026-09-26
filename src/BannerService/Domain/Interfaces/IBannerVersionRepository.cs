namespace BannerService.Domain.Interfaces;

using Entities;

public interface IBannerVersionRepository
{
    Task<BannerVersion> SaveAsync(BannerVersion version);
    Task<List<BannerVersion>> GetVersionsAsync(Guid bannerId, Guid shopId);
    Task<BannerVersion?> GetVersionAsync(Guid bannerId, int versionNumber, Guid shopId);
    Task<int> GetNextVersionNumberAsync(Guid bannerId, Guid shopId);
}

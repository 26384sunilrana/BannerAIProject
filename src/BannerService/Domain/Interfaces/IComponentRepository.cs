namespace BannerService.Domain.Interfaces;

using Entities;

public interface IComponentRepository
{
    Task<Component> CreateAsync(Component component);
    Task<Component?> GetByIdAsync(Guid componentId);
    Task<List<Component>> GetByBannerIdAsync(Guid bannerId);
    Task<Component> UpdateAsync(Component component);
    Task<bool> DeleteAsync(Guid componentId);
    Task<bool> ExistsByZIndexAsync(Guid bannerId, int zIndex);
}

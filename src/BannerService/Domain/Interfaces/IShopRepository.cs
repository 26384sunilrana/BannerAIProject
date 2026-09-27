namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface IShopRepository
    {
        Task<Shop?> GetByIdAsync(Guid shopId);
        Task<Shop?> GetByNameAsync(string name);
        Task<List<Shop>> GetAllAsync();
        Task<List<Shop>> GetActiveAsync();
        Task<List<Shop>> GetByOwnerAsync(Guid ownerId);
        Task<List<Shop>> GetByParentAsync(Guid parentShopId);
        Task<Shop?> GetHierarchyAsync(Guid shopId, int maxDepth = 10);
        Task<List<Shop>> GetDescendantsAsync(Guid shopId, int maxDepth = 10);
        Task<List<Shop>> GetAncestorsAsync(Guid shopId);
        Task<List<Shop>> SearchAsync(string searchTerm, string? city = null, ShopStatus? status = null);
        Task<List<Shop>> GetPaginatedAsync(int pageNumber, int pageSize, string? city = null, ShopStatus? status = null);
        Task<Shop> CreateAsync(Shop shop);
        Task<Shop> UpdateAsync(Shop shop);
        Task<bool> DeleteAsync(Guid shopId); // Soft delete to Archived
        Task<bool> ExistsAsync(Guid shopId);
        Task<bool> ExistsByNameAsync(string name);
        Task<int> GetCountAsync();
        Task<int> GetCountByStatusAsync(ShopStatus status);
        Task<int> GetCountByOwnerAsync(Guid ownerId);
    }
}

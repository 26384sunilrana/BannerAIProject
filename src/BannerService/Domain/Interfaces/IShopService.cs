namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface IShopService
    {
        Task<Shop?> GetByIdAsync(Guid shopId);
        Task<List<Shop>> GetAllAsync();
        Task<List<Shop>> GetByOwnerAsync(Guid ownerId);
        Task<Shop?> GetHierarchyAsync(Guid shopId);
        Task<List<Shop>> SearchAsync(string searchTerm, string? city = null, ShopStatus? status = null);
        Task<List<Shop>> GetPaginatedAsync(int pageNumber, int pageSize, string? city = null, ShopStatus? status = null);
        Task<(bool success, string message, Shop? shop)> CreateAsync(
            string name,
            string? description,
            Guid? parentShopId,
            string? address,
            string? city,
            string? countryCode,
            int? stateId,
            int? districtId,
            string? postalCode,
            double? latitude,
            double? longitude,
            string? phoneNumber,
            string? website,
            Guid? ownerId,
            Guid createdByUserId);
        Task<(bool success, string message)> UpdateAsync(
            Guid shopId,
            string name,
            string? description,
            string? address,
            string? city,
            string? countryCode,
            int? stateId,
            int? districtId,
            string? postalCode,
            double? latitude,
            double? longitude,
            string? phoneNumber,
            string? website,
            ShopStatus status);
        Task<(bool success, string message)> AssignOwnerAsync(Guid shopId, Guid userId);
        Task<(bool success, string message)> RemoveOwnerAsync(Guid shopId);
        Task<(bool success, string message)> DeleteAsync(Guid shopId);
        Task<(bool success, string message)> ArchiveAsync(Guid shopId);
    }
}

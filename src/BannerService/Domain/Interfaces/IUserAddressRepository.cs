using BannerService.Domain.Entities;

namespace BannerService.Domain.Interfaces
{
    public interface IUserAddressRepository
    {
        Task<UserAddress?> GetByIdAsync(Guid id);
        Task<List<UserAddress>> GetByUserIdAsync(string userId);
        Task<UserAddress?> GetPrimaryByUserIdAsync(string userId);
        Task<UserAddress> CreateAsync(UserAddress address);
        Task<UserAddress> UpdateAsync(UserAddress address);
        Task<bool> DeleteAsync(Guid id);
        Task<int> CountByUserAsync(string userId);
        Task<bool> ExistsAsync(Guid id);
    }
}

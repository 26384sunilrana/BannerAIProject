using BannerService.Application.DTOs;

namespace BannerService.Application.Services
{
    public interface IUserAddressService
    {
        Task<UserAddressDto> CreateAsync(string userId, CreateUserAddressDto dto);
        Task<UserAddressDto> UpdateAsync(string userId, Guid addressId, UpdateUserAddressDto dto);
        Task<bool> DeleteAsync(string userId, Guid addressId);
        Task<UserAddressDto> GetByIdAsync(string userId, Guid addressId);
        Task<List<UserAddressDto>> GetAllByUserAsync(string userId);
        Task<UserAddressDto> SetPrimaryAsync(string userId, Guid addressId);
    }
}

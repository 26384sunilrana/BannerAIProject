using BannerService.Application.DTOs;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;

namespace BannerService.Application.Services
{
    public class UserAddressService : IUserAddressService
    {
        private readonly IUserAddressRepository _userAddressRepository;
        private readonly IAddressRepository _addressRepository;

        public UserAddressService(
            IUserAddressRepository userAddressRepository,
            IAddressRepository addressRepository)
        {
            _userAddressRepository = userAddressRepository;
            _addressRepository = addressRepository;
        }

        public async Task<UserAddressDto> CreateAsync(string userId, CreateUserAddressDto dto)
        {
            // Validate FK existence
            if (!string.IsNullOrEmpty(dto.CountryCode))
            {
                var country = await _addressRepository.GetCountryAsync(dto.CountryCode);
                if (country == null)
                    throw new InvalidOperationException($"Country with code '{dto.CountryCode}' not found");
            }

            if (dto.StateId.HasValue)
            {
                var state = await _addressRepository.GetStateAsync(dto.StateId.Value);
                if (state == null)
                    throw new InvalidOperationException($"State with id '{dto.StateId}' not found");
            }

            if (dto.DistrictId.HasValue)
            {
                var district = await _addressRepository.GetDistrictAsync(dto.DistrictId.Value);
                if (district == null)
                    throw new InvalidOperationException($"District with id '{dto.DistrictId}' not found");
            }

            // Create the address
            var address = new UserAddress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AddressLine = dto.AddressLine,
                City = dto.City,
                CountryCode = dto.CountryCode,
                StateId = dto.StateId,
                DistrictId = dto.DistrictId,
                PostalCode = dto.PostalCode,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Label = dto.Label,
                IsPrimary = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // If this is the first address or explicitly marked as primary, make it primary
            var existingAddresses = await _userAddressRepository.GetByUserIdAsync(userId);
            if (existingAddresses.Count == 0 || dto.IsPrimary)
            {
                // Unset any existing primary address
                foreach (var existing in existingAddresses.Where(a => a.IsPrimary))
                {
                    existing.UnmarkAsPrimary();
                    await _userAddressRepository.UpdateAsync(existing);
                }

                // Mark this one as primary
                address.MarkAsPrimary();
            }

            // Save
            await _userAddressRepository.CreateAsync(address);

            return MapToDto(address);
        }

        public async Task<UserAddressDto> UpdateAsync(string userId, Guid addressId, UpdateUserAddressDto dto)
        {
            var address = await _userAddressRepository.GetByIdAsync(addressId);
            if (address == null || address.UserId != userId)
                throw new KeyNotFoundException($"Address not found or does not belong to user");

            // Validate FK existence if changing
            if (dto.CountryCode != null && dto.CountryCode != address.CountryCode)
            {
                if (!string.IsNullOrEmpty(dto.CountryCode))
                {
                    var country = await _addressRepository.GetCountryAsync(dto.CountryCode);
                    if (country == null)
                        throw new InvalidOperationException($"Country with code '{dto.CountryCode}' not found");
                }
            }

            if (dto.StateId.HasValue && dto.StateId != address.StateId)
            {
                var state = await _addressRepository.GetStateAsync(dto.StateId.Value);
                if (state == null)
                    throw new InvalidOperationException($"State with id '{dto.StateId}' not found");
            }

            if (dto.DistrictId.HasValue && dto.DistrictId != address.DistrictId)
            {
                var district = await _addressRepository.GetDistrictAsync(dto.DistrictId.Value);
                if (district == null)
                    throw new InvalidOperationException($"District with id '{dto.DistrictId}' not found");
            }

            // Update fields
            address.UpdateAddress(
                dto.AddressLine ?? address.AddressLine,
                dto.City ?? address.City,
                dto.CountryCode ?? address.CountryCode,
                dto.StateId ?? address.StateId,
                dto.DistrictId ?? address.DistrictId,
                dto.PostalCode ?? address.PostalCode,
                dto.Latitude ?? address.Latitude,
                dto.Longitude ?? address.Longitude
            );

            if (dto.Label != null)
                address.UpdateLabel(dto.Label);

            await _userAddressRepository.UpdateAsync(address);
            return MapToDto(address);
        }

        public async Task<bool> DeleteAsync(string userId, Guid addressId)
        {
            var address = await _userAddressRepository.GetByIdAsync(addressId);
            if (address == null || address.UserId != userId)
                throw new KeyNotFoundException($"Address not found or does not belong to user");

            var wasPrimary = address.IsPrimary;
            var deleteResult = await _userAddressRepository.DeleteAsync(addressId);

            if (deleteResult && wasPrimary)
            {
                // If deleted address was primary, promote the earliest remaining address
                var remainingAddresses = await _userAddressRepository.GetByUserIdAsync(userId);
                if (remainingAddresses.Count > 0)
                {
                    var newPrimary = remainingAddresses.OrderBy(a => a.CreatedAt).First();
                    newPrimary.MarkAsPrimary();
                    await _userAddressRepository.UpdateAsync(newPrimary);
                }
            }

            return deleteResult;
        }

        public async Task<UserAddressDto> GetByIdAsync(string userId, Guid addressId)
        {
            var address = await _userAddressRepository.GetByIdAsync(addressId);
            if (address == null || address.UserId != userId)
                throw new KeyNotFoundException($"Address not found or does not belong to user");

            return MapToDto(address);
        }

        public async Task<List<UserAddressDto>> GetAllByUserAsync(string userId)
        {
            var addresses = await _userAddressRepository.GetByUserIdAsync(userId);
            return addresses.Select(MapToDto).ToList();
        }

        public async Task<UserAddressDto> SetPrimaryAsync(string userId, Guid addressId)
        {
            var address = await _userAddressRepository.GetByIdAsync(addressId);
            if (address == null || address.UserId != userId)
                throw new KeyNotFoundException($"Address not found or does not belong to user");

            // Unset current primary
            var currentPrimary = await _userAddressRepository.GetPrimaryByUserIdAsync(userId);
            if (currentPrimary != null && currentPrimary.Id != addressId)
            {
                currentPrimary.UnmarkAsPrimary();
                await _userAddressRepository.UpdateAsync(currentPrimary);
            }

            // Set new primary
            address.MarkAsPrimary();
            await _userAddressRepository.UpdateAsync(address);

            return MapToDto(address);
        }

        private UserAddressDto MapToDto(UserAddress address)
        {
            return new UserAddressDto
            {
                Id = address.Id,
                UserId = address.UserId,
                AddressLine = address.AddressLine,
                City = address.City,
                CountryCode = address.CountryCode,
                CountryName = address.CountryNav?.Name,
                StateId = address.StateId,
                StateName = address.StateNav?.Name,
                DistrictId = address.DistrictId,
                DistrictName = address.DistrictNav?.Name,
                PostalCode = address.PostalCode,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                Label = address.Label,
                IsPrimary = address.IsPrimary,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };
        }
    }
}

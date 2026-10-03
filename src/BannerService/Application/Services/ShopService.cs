namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.Services;
    using Validators;

    public class ShopService : IShopService
    {
        private readonly IShopRepository _shopRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ShopService> _logger;

        public ShopService(
            IShopRepository shopRepository,
            IUserRepository userRepository,
            ILogger<ShopService> logger)
        {
            _shopRepository = shopRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Shop?> GetByIdAsync(Guid shopId)
        {
            return await _shopRepository.GetByIdAsync(shopId);
        }

        public async Task<List<Shop>> GetAllAsync()
        {
            return await _shopRepository.GetAllAsync();
        }

        public async Task<List<Shop>> GetByOwnerAsync(Guid ownerId)
        {
            return await _shopRepository.GetByOwnerAsync(ownerId);
        }

        public async Task<Shop?> GetHierarchyAsync(Guid shopId)
        {
            return await _shopRepository.GetHierarchyAsync(shopId);
        }

        public async Task<List<Shop>> SearchAsync(string searchTerm, string? city = null, ShopStatus? status = null)
        {
            return await _shopRepository.SearchAsync(searchTerm, city, status);
        }

        public async Task<List<Shop>> GetPaginatedAsync(int pageNumber, int pageSize, string? city = null, ShopStatus? status = null)
        {
            return await _shopRepository.GetPaginatedAsync(pageNumber, pageSize, city, status);
        }

        public async Task<(bool success, string message, Shop? shop)> CreateAsync(
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
            Guid createdByUserId)
        {
            // Validate shop data
            var (isValid, errorMessage) = ShopValidator.ValidateCreateShop(
                name, description, phoneNumber, website, latitude, longitude);

            if (!isValid)
                return (false, errorMessage!, null);

            // Names may repeat; one name at one address is one shop
            if (await FindSameShopAsync(null, name, address, postalCode) != null)
                return (false, SameShopMessage, null);

            // Validate parent shop exists if specified
            if (parentShopId.HasValue)
            {
                var parentShop = await _shopRepository.GetByIdAsync(parentShopId.Value);
                if (parentShop == null)
                    return (false, "Parent shop does not exist", null);

                if (parentShop.IsArchived)
                    return (false, "Cannot add child shop to archived parent", null);
            }

            // Validate owner exists if specified
            if (ownerId.HasValue)
            {
                var owner = await _userRepository.GetByIdAsync(ownerId.Value.ToString());
                if (owner == null)
                    return (false, "Specified owner user does not exist", null);
            }

            var shop = new Shop
            {
                Name = name,
                Description = description,
                ParentShopId = parentShopId,
                CountryCode = countryCode,
                StateId = stateId,
                DistrictId = districtId,
                Address = address,
                City = city,
                PostalCode = postalCode,
                Latitude = latitude,
                Longitude = longitude,
                PhoneNumber = phoneNumber,
                Website = website,
                OwnerUserId = ownerId,
                CreatedByUserId = createdByUserId,
                Status = ShopStatus.Active
            };

            var createdShop = await _shopRepository.CreateAsync(shop);
            _logger.LogInformation("Shop created: {ShopId} - {ShopName}", createdShop.Id, createdShop.Name);

            return (true, "Shop created successfully", createdShop);
        }

        public async Task<(bool success, string message)> UpdateAsync(
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
            ShopStatus status)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null)
                return (false, "Shop not found");

            // Validate new data
            var (isValid, errorMessage) = ShopValidator.ValidateCreateShop(
                name, description, phoneNumber, website, latitude, longitude);

            if (!isValid)
                return (false, errorMessage!);

            // Names may repeat; one name at one address is one shop (a shop that changed hands asks for a takeover instead)
            if (await FindSameShopAsync(shopId, name, address, postalCode) != null)
                return (false, SameShopMessage);

            // Validate status transition
            var (validTransition, transitionError) = ShopValidator.ValidateStatusTransition(shop.Status, status);
            if (!validTransition)
                return (false, transitionError!);

            // If archiving, check for active child shops
            if (status == ShopStatus.Archived && shop.IsActive)
            {
                var hasActiveChildren = shop.ChildShops.Any(c => c.Status == ShopStatus.Active);
                if (hasActiveChildren)
                    return (false, "Cannot archive shop with active child shops");
            }

            shop.UpdateBasicInfo(name, description);
            shop.UpdateLocation(address, city, countryCode, stateId, districtId, postalCode, latitude, longitude);
            shop.UpdateContactInfo(phoneNumber, website);
            shop.SetStatus(status);

            await _shopRepository.UpdateAsync(shop);
            _logger.LogInformation("Shop updated: {ShopId} - {ShopName}", shopId, name);

            return (true, "Shop updated successfully");
        }

        public const string SameShopMessage =
            "A shop with this name and address already exists. If that shop changed hands and is yours now, ask for a takeover on the Takeover page instead.";

        /// <summary>An active shop, other than this one, with the same name at the same address.</summary>
        private async Task<Shop?> FindSameShopAsync(Guid? exceptShopId, string name, string? address, string? postalCode)
        {
            if (!ShopIdentity.HasAddress(address)) return null;
            return (await _shopRepository.ListByNameAsync(name.Trim()))
                .FirstOrDefault(s => s.Id != exceptShopId && s.Status == ShopStatus.Active && ShopIdentity.Same(name, address, postalCode, s.Name, s.Address, s.PostalCode));
        }

        public async Task<(bool success, string message)> AssignOwnerAsync(Guid shopId, Guid userId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null)
                return (false, "Shop not found");

            var user = await _userRepository.GetByIdAsync(userId.ToString());
            if (user == null)
                return (false, "User not found");

            shop.AssignOwner(userId);
            await _shopRepository.UpdateAsync(shop);
            _logger.LogInformation("Owner assigned to shop {ShopId}: {UserId}", shopId, userId);

            return (true, "Owner assigned successfully");
        }

        public async Task<(bool success, string message)> RemoveOwnerAsync(Guid shopId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null)
                return (false, "Shop not found");

            shop.RemoveOwner();
            await _shopRepository.UpdateAsync(shop);
            _logger.LogInformation("Owner removed from shop {ShopId}", shopId);

            return (true, "Owner removed successfully");
        }

        public async Task<(bool success, string message)> DeleteAsync(Guid shopId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null)
                return (false, "Shop not found");

            var deleted = await _shopRepository.DeleteAsync(shopId);
            if (!deleted)
                return (false, "Failed to delete shop");

            _logger.LogInformation("Shop archived: {ShopId}", shopId);
            return (true, "Shop archived successfully");
        }

        public async Task<(bool success, string message)> ArchiveAsync(Guid shopId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null)
                return (false, "Shop not found");

            var (success, message) = await UpdateAsync(
                shopId,
                shop.Name,
                shop.Description,
                shop.Address,
                shop.City,
                shop.CountryCode,
                shop.StateId,
                shop.DistrictId,
                shop.PostalCode,
                shop.Latitude,
                shop.Longitude,
                shop.PhoneNumber,
                shop.Website,
                ShopStatus.Archived);

            return (success, message);
        }
    }
}

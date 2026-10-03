namespace BannerService.Presentation.Controllers
{
    using Application.DTOs;
    using Application.Services;
    using Domain.Entities;
    using Domain.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShopsController : ControllerBase
    {
        private readonly IShopService _shopService;
        private readonly ILogger<ShopsController> _logger;

        public ShopsController(IShopService shopService, ILogger<ShopsController> logger)
        {
            _shopService = shopService;
            _logger = logger;
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        [Authorize]
        public async Task<IActionResult> CreateShop([FromBody] CreateShopDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            if (!Guid.TryParse(userId, out var userGuid))
                return BadRequest(new { message = "Invalid user ID format" });

            var (success, message, shop) = await _shopService.CreateAsync(
                request.Name,
                request.Description,
                request.ParentShopId,
                request.Address,
                request.City,
                request.CountryCode,
                request.StateId,
                request.DistrictId,
                request.PostalCode,
                request.Latitude,
                request.Longitude,
                request.PhoneNumber,
                request.Website,
                request.OwnerUserId,
                userGuid);

            if (!success)
                return BadRequest(new { message });

            var shopDto = MapToDto(shop!);
            return CreatedAtAction(nameof(GetShopById), new { shopId = shop!.Id }, shopDto);
        }

        [HttpGet("{shopId}")]
        [Authorize]
        public async Task<IActionResult> GetShopById(Guid shopId)
        {
            var shop = await _shopService.GetByIdAsync(shopId);
            if (shop == null)
                return NotFound(new { message = "Shop not found" });

            var shopDto = MapToDto(shop);
            return Ok(shopDto);
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        [Authorize]
        public async Task<IActionResult> ListShops(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? city = null,
            [FromQuery] ShopStatus? status = null)
        {
            if (pageNumber < 1 || pageSize < 1)
                return BadRequest(new { message = "Page number and size must be greater than 0" });

            var all = (await _shopService.GetAllAsync())
                .Where(s => (status == null || s.Status == status)
                    && (string.IsNullOrWhiteSpace(city) || (s.City != null && s.City.Equals(city, StringComparison.OrdinalIgnoreCase))))
                .ToList();
            var shopDtos = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();

            return Ok(new
            {
                pageNumber,
                pageSize,
                items = shopDtos,
                total = all.Count
            });
        }

        [HttpGet("my-shops")]
        [Authorize]
        public async Task<IActionResult> GetMyShops(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] ShopStatus? status = null)
        {
            if (pageNumber < 1 || pageSize < 1)
                return BadRequest(new { message = "Page number and size must be greater than 0" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized(new { message = "User not authenticated" });

            var shops = await _shopService.GetByOwnerAsync(userGuid);

            if (status.HasValue)
                shops = shops.Where(s => s.Status == status.Value).ToList();

            var paginatedShops = shops
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            return Ok(new
            {
                pageNumber,
                pageSize,
                items = paginatedShops,
                total = shops.Count
            });
        }

        [HttpPut("{shopId}")]
        [Authorize(Roles = "Admin,ShopOwner")]
        public async Task<IActionResult> UpdateShop(Guid shopId, [FromBody] UpdateShopDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var shop = await _shopService.GetByIdAsync(shopId);
            if (shop == null)
                return NotFound(new { message = "Shop not found" });

            var (success, message) = await _shopService.UpdateAsync(
                shopId,
                request.Name,
                request.Description,
                request.Address,
                request.City,
                request.CountryCode,
                request.StateId,
                request.DistrictId,
                request.PostalCode,
                request.Latitude,
                request.Longitude,
                request.PhoneNumber,
                request.Website,
                request.Status);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [HttpDelete("{shopId}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        [Authorize]
        public async Task<IActionResult> DeleteShop(Guid shopId)
        {
            var shop = await _shopService.GetByIdAsync(shopId);
            if (shop == null)
                return NotFound(new { message = "Shop not found" });

            var (success, message) = await _shopService.DeleteAsync(shopId);
            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [HttpGet("{shopId}/hierarchy")]
        [Authorize]
        public async Task<IActionResult> GetShopHierarchy(Guid shopId)
        {
            var shop = await _shopService.GetHierarchyAsync(shopId);
            if (shop == null)
                return NotFound(new { message = "Shop not found" });

            var hierarchyDto = MapToHierarchyDto(shop);
            return Ok(hierarchyDto);
        }

        [HttpPost("{shopId}/assign-owner")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        [Authorize]
        public async Task<IActionResult> AssignOwner(Guid shopId, [FromBody] AssignOwnerDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var shop = await _shopService.GetByIdAsync(shopId);
            if (shop == null)
                return NotFound(new { message = "Shop not found" });

            var (success, message) = await _shopService.AssignOwnerAsync(shopId, request.UserId);
            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [HttpDelete("{shopId}/remove-owner")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        [Authorize]
        public async Task<IActionResult> RemoveOwner(Guid shopId)
        {
            var shop = await _shopService.GetByIdAsync(shopId);
            if (shop == null)
                return NotFound(new { message = "Shop not found" });

            var (success, message) = await _shopService.RemoveOwnerAsync(shopId);
            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [HttpGet("search")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        [Authorize]
        public async Task<IActionResult> SearchShops(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? city = null,
            [FromQuery] ShopStatus? status = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest(new { message = "Search term is required" });

            var shops = await _shopService.SearchAsync(searchTerm, city, status);
            var shopDtos = shops.Select(MapToDto).ToList();

            return Ok(new { items = shopDtos, total = shopDtos.Count });
        }

        [HttpGet("{shopId}/locations")]
        [Authorize]
        public async Task<IActionResult> GetShopLocations(Guid shopId)
        {
            var shop = await _shopService.GetByIdAsync(shopId);
            if (shop == null)
                return NotFound(new { message = "Shop not found" });

            var locations = new List<ShopLocationDto> { new ShopLocationDto
            {
                ShopId = shop.Id,
                Name = shop.Name,
                City = shop.City,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude
            }};

            // Add child shops locations
            var descendants = new List<Shop>();
            var hierarchy = await _shopService.GetHierarchyAsync(shopId);
            if (hierarchy != null)
            {
                CollectDescendants(hierarchy, descendants);
                foreach (var descendant in descendants)
                {
                    if (!string.IsNullOrEmpty(descendant.City))
                    {
                        locations.Add(new ShopLocationDto
                        {
                            ShopId = descendant.Id,
                            Name = descendant.Name,
                            City = descendant.City,
                            Latitude = descendant.Latitude,
                            Longitude = descendant.Longitude
                        });
                    }
                }
            }

            return Ok(new { items = locations, total = locations.Count });
        }

        private ShopDto MapToDto(Shop shop)
        {
            return new ShopDto
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                ParentShopId = shop.ParentShopId,
                ChildShopsCount = shop.ChildShops?.Count ?? 0,
                CountryCode = shop.CountryCode,
                StateId = shop.StateId,
                DistrictId = shop.DistrictId,
                CountryName = shop.CountryNav?.Name,
                StateName = shop.StateNav?.Name,
                DistrictName = shop.DistrictNav?.Name,
                UniqueId = shop.UniqueId,
                CityId = shop.CityId,
                GroupId = shop.GroupId,
                CityUniqueId = shop.CityNav?.UniqueId,
                GroupName = shop.GroupNav?.Name,
                GroupUniqueId = shop.GroupNav?.UniqueId,
                TimeZoneId = Domain.Services.ShopTimeZone.Resolve(shop).Id,
                TimeZoneSource = Domain.Services.ShopTimeZone.Resolve(shop).Source,
                OwnTimeZoneId = shop.TimeZoneId,
                Address = shop.Address,
                City = shop.City,
                PostalCode = shop.PostalCode,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                Status = shop.Status,
                PhoneNumber = shop.PhoneNumber,
                Website = shop.Website,
                OwnerUserId = shop.OwnerUserId,
                OwnerUserName = shop.Owner?.FirstName + " " + shop.Owner?.LastName,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            };
        }

        private ShopHierarchyDto MapToHierarchyDto(Shop shop)
        {
            var childDtos = shop.ChildShops?
                .Select(MapToHierarchyDto)
                .ToList() ?? new List<ShopHierarchyDto>();

            return new ShopHierarchyDto
            {
                Shop = MapToDto(shop),
                ChildShops = childDtos
            };
        }

        private void CollectDescendants(Shop shop, List<Shop> descendants)
        {
            if (shop.ChildShops != null)
            {
                foreach (var child in shop.ChildShops)
                {
                    descendants.Add(child);
                    CollectDescendants(child, descendants);
                }
            }
        }
    }
}

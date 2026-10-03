namespace BannerService.Application.DTOs
{
    using BannerService.Domain.Entities;

    public class CreateShopDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentShopId { get; set; }

        // Address with master data
        public string? CountryCode { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }

        // Address details
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public Guid? OwnerUserId { get; set; }
    }

    public class UpdateShopDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Address with master data
        public string? CountryCode { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }

        // Address details
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public ShopStatus Status { get; set; }
    }

    public class ShopDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentShopId { get; set; }
        public int ChildShopsCount { get; set; }

        // Address with master data
        public string? CountryCode { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }

        // Address display
        public string? CountryName { get; set; }
        public string? StateName { get; set; }
        public string? DistrictName { get; set; }

        // Location hierarchy: Country > State > City > Group > Shop
        public string? UniqueId { get; set; }
        public int? CityId { get; set; }
        public int? GroupId { get; set; }
        public string? CityUniqueId { get; set; }
        public string? GroupName { get; set; }
        public string? GroupUniqueId { get; set; }

        /// <summary>The time zone the shop works in (its own, its city's, its country's, or UTC) and which of those it came from.</summary>
        public string TimeZoneId { get; set; } = "UTC";
        public string TimeZoneSource { get; set; } = "default";
        public string? OwnTimeZoneId { get; set; }

        // Address details
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public ShopStatus Status { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public Guid? OwnerUserId { get; set; }
        public string? OwnerUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ShopHierarchyDto
    {
        public ShopDto Shop { get; set; } = null!;
        public List<ShopHierarchyDto> ChildShops { get; set; } = new();
    }

    public class AssignOwnerDto
    {
        public Guid UserId { get; set; }
    }

    public class CreateChildShopDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class ShopLocationDto
    {
        public Guid ShopId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class ShopListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
        public ShopStatus Status { get; set; }
        public int ChildShopsCount { get; set; }
    }
}

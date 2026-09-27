namespace BannerService.Domain.Entities
{
    public class Shop
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Hierarchy
        public Guid? ParentShopId { get; set; }
        public virtual Shop? ParentShop { get; set; }
        public virtual ICollection<Shop> ChildShops { get; set; } = new List<Shop>();

        // Location
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Status & Contact
        public ShopStatus Status { get; set; } = ShopStatus.Active;
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }

        // Management
        public Guid? OwnerUserId { get; set; }
        public virtual User? Owner { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid CreatedByUserId { get; set; }

        public void UpdateBasicInfo(string name, string? description)
        {
            Name = name;
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateLocation(string? address, string? city, string? state, string? country, string? postalCode, double? latitude, double? longitude)
        {
            Address = address;
            City = city;
            State = state;
            Country = country;
            PostalCode = postalCode;
            Latitude = latitude;
            Longitude = longitude;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateContactInfo(string? phoneNumber, string? website)
        {
            PhoneNumber = phoneNumber;
            Website = website;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetStatus(ShopStatus status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AssignOwner(Guid userId)
        {
            OwnerUserId = userId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveOwner()
        {
            OwnerUserId = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsArchived => Status == ShopStatus.Archived;
        public bool IsActive => Status == ShopStatus.Active;
        public bool IsInactive => Status == ShopStatus.Inactive;
    }

    public enum ShopStatus
    {
        Active = 1,
        Inactive = 2,
        Archived = 3
    }
}

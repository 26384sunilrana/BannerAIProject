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

        // Location - Master Data References
        public string? CountryCode { get; set; } // FK to Country.ISOCode
        public int? StateId { get; set; } // FK to State.Id
        public int? DistrictId { get; set; } // FK to District.Id
        public int? CityId { get; set; } // FK to City.Id
        public int? GroupId { get; set; } // FK to LocationGroup.Id; the group belongs to CityId

        /// <summary>Platform-wide identifier (SHP-...) given when the shop takes a subscription. Never changed.</summary>
        public string? UniqueId { get; set; }

        /// <summary>The shop's own time zone (IANA name). Null means "use my city's, then my country's".</summary>
        public string? TimeZoneId { get; set; }

        // The shop's own default board, shown when no banner is live (colours are #rrggbb)
        public string? DefaultBoardMessage { get; set; }
        public string? DefaultBoardBackground { get; set; }
        public string? DefaultBoardTextColor { get; set; }
        public Guid? DefaultBoardLogoMediaId { get; set; }

        // Location - Details
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Navigation Properties for Master Data
        public virtual Country? CountryNav { get; set; }
        public virtual State? StateNav { get; set; }
        public virtual District? DistrictNav { get; set; }
        public virtual City? CityNav { get; set; }
        public virtual LocationGroup? GroupNav { get; set; }

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

        public void UpdateLocation(string? address, string? city, string? countryCode, int? stateId, int? districtId, string? postalCode, double? latitude, double? longitude)
        {
            Address = address;
            City = city;
            CountryCode = countryCode;
            StateId = stateId;
            DistrictId = districtId;
            PostalCode = postalCode;
            Latitude = latitude;
            Longitude = longitude;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Places the shop in a city and, optionally, a location group inside that city.</summary>
        public void SetLocation(City city, LocationGroup? group)
        {
            CityId = city.Id;
            City = city.Name;
            StateId = city.StateId;
            CountryCode = city.State?.CountryCode ?? CountryCode;
            GroupId = group?.Id;
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

        // Sales executives per shop, in addition to the owner
        public const int MaxSalesExecutives = 2;

        // Who may approve new or changed banners: the owner, either executive, or both
        /// <summary>
        /// Set when the shop changes owner while its associates stay. Nothing can be approved until the new owner has looked at who approves
        /// (Team screen) and confirmed it.
        /// </summary>
        public bool ApprovalReviewRequired { get; set; }

        public bool OwnerIsApprover { get; private set; } = true;
        public List<string> ApproverUserIds { get; private set; } = new();

        /// <summary>Sales executives the owner allows to approve the ads other executives book. The owner always can.</summary>
        public List<string> AdApproverUserIds { get; private set; } = new();

        public bool CanApproveAds(Guid userId) =>
            OwnerUserId == userId || AdApproverUserIds.Contains(userId.ToString(), StringComparer.OrdinalIgnoreCase);

        public void SetAdApprovers(IEnumerable<string> userIds)
        {
            AdApproverUserIds = userIds.Select(i => i.ToLowerInvariant()).Distinct().ToList();
            UpdatedAt = DateTime.UtcNow;
        }

        public bool CanApprove(Guid userId)
        {
            if (OwnerIsApprover && OwnerUserId == userId)
                return true;

            return ApproverUserIds.Contains(userId.ToString(), StringComparer.OrdinalIgnoreCase);
        }

        public void SetApprovers(bool ownerIsApprover, IEnumerable<string> approverUserIds)
        {
            ApprovalReviewRequired = false; // saving the setup is the confirmation
            var ids = approverUserIds.Select(i => i.ToLowerInvariant()).Distinct().ToList();
            if (!ownerIsApprover && ids.Count == 0)
                throw new InvalidOperationException("A shop needs at least one approver");

            OwnerIsApprover = ownerIsApprover;
            ApproverUserIds = ids;
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveApprover(string userId)
        {
            ApproverUserIds = ApproverUserIds.Where(i => !string.Equals(i, userId, StringComparison.OrdinalIgnoreCase)).ToList();
            AdApproverUserIds = AdApproverUserIds.Where(i => !string.Equals(i, userId, StringComparison.OrdinalIgnoreCase)).ToList();

            // Never leave a shop without anyone able to approve banners
            if (!OwnerIsApprover && ApproverUserIds.Count == 0)
                OwnerIsApprover = true;

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

    // sent to the web app as its name (Active, Inactive, Archived); numbers are still accepted when reading
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public enum ShopStatus
    {
        Active = 1,
        Inactive = 2,
        Archived = 3
    }
}

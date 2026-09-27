namespace BannerService.Domain.Entities
{
    public class UserAddress
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty; // FK to User.Id (string, not Guid)
        public virtual User? User { get; set; }

        // Location - Master Data References (nullable, like Shop)
        public string? CountryCode { get; set; } // FK to Country.ISOCode
        public int? StateId { get; set; }        // FK to State.Id
        public int? DistrictId { get; set; }     // FK to District.Id

        // Location - Details
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Navigation Properties for Master Data
        public virtual Country? CountryNav { get; set; }
        public virtual State? StateNav { get; set; }
        public virtual District? DistrictNav { get; set; }

        // Address Metadata
        public string? Label { get; set; } // e.g., "Home", "Work"
        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public void UpdateAddress(string? addressLine, string? city, string? countryCode, int? stateId, int? districtId, string? postalCode, double? latitude, double? longitude)
        {
            AddressLine = addressLine;
            City = city;
            CountryCode = countryCode;
            StateId = stateId;
            DistrictId = districtId;
            PostalCode = postalCode;
            Latitude = latitude;
            Longitude = longitude;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsPrimary()
        {
            IsPrimary = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UnmarkAsPrimary()
        {
            IsPrimary = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateLabel(string? label)
        {
            Label = label;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

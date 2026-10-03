namespace BannerService.Domain.Entities
{
    public class Country
    {
        public string ISOCode { get; set; } = string.Empty; // "IN", "US", "BR"
        public string Name { get; set; } = string.Empty;
        public string? RegionName { get; set; }
        public string? PhoneCode { get; set; } // "+91" for India
        /// <summary>Platform-wide identifier, e.g. CNT-7KQ2-9M4D. Assigned once and never changed.</summary>
        public string? UniqueId { get; set; }
        /// <summary>Time zone (IANA name, e.g. Asia/Kolkata) shops here use unless their city or they say otherwise.</summary>
        public string? TimeZoneId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<State> States { get; set; } = new List<State>();
        public virtual ICollection<Shop> Shops { get; set; } = new List<Shop>();
    }
}

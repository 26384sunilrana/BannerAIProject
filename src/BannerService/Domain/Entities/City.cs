namespace BannerService.Domain.Entities
{
    /// <summary>A city inside a state. Location groups, and through them shops, hang under a city.</summary>
    public class City
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>Platform-wide identifier, e.g. CTY-7KQ2-9M4D. Assigned once and never changed.</summary>
        public string UniqueId { get; set; } = string.Empty;
        /// <summary>Time zone (IANA name) for shops in this city; falls back to the country's.</summary>
        public string? TimeZoneId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual State? State { get; set; }
        public virtual ICollection<LocationGroup> Groups { get; set; } = new List<LocationGroup>();
        public virtual ICollection<Shop> Shops { get; set; } = new List<Shop>();
    }
}

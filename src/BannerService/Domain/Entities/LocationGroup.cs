namespace BannerService.Domain.Entities
{
    /// <summary>A group of shops that share a location (an area or locality) inside one city.</summary>
    public class LocationGroup
    {
        public int Id { get; set; }
        public int CityId { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>Platform-wide identifier, e.g. GRP-7KQ2-9M4D. Assigned once and never changed.</summary>
        public string UniqueId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual City? City { get; set; }
        public virtual ICollection<Shop> Shops { get; set; } = new List<Shop>();
    }
}

namespace BannerService.Domain.Entities
{
    public class State
    {
        public int Id { get; set; }
        public string CountryCode { get; set; } = string.Empty; // "IN", "US"
        public string Code { get; set; } = string.Empty; // "MH", "DL", "TN"
        public string Name { get; set; } = string.Empty; // Maharashtra, Delhi
        public string? RegionType { get; set; } // "State", "Union Territory"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Country? Country { get; set; }
        public virtual ICollection<District> Districts { get; set; } = new List<District>();
        public virtual ICollection<Shop> Shops { get; set; } = new List<Shop>();
    }
}

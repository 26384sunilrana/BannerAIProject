namespace BannerService.Domain.Entities
{
    public class District
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public string Code { get; set; } = string.Empty; // District code
        public string Name { get; set; } = string.Empty; // District name (e.g., Mumbai, Pune)
        public string? RegionType { get; set; } // "District", "Municipality", "Corporation"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual State? State { get; set; }
        public virtual ICollection<Shop> Shops { get; set; } = new List<Shop>();
    }
}

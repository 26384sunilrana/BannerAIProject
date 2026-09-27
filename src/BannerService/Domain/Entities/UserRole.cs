namespace BannerService.Domain.Entities
{
    public class UserRole
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
        public Role? Role { get; set; }
    }
}

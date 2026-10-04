namespace BannerService.Domain.Entities
{
    public enum AdminDeletionStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Completed = 3
    }

    public class AdminDeletionRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AdminIdToDelete { get; set; } = string.Empty;
        public string RequestedByAdminId { get; set; } = string.Empty;
        public AdminDeletionStatus Status { get; set; } = AdminDeletionStatus.Pending;
        public string? ApprovedByAdminId { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        // Navigation properties
        public User? AdminToDelete { get; set; }
        public User? RequestedByAdmin { get; set; }
        public User? ApprovedByAdmin { get; set; }
    }
}

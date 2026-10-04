namespace BannerService.Application.Services;

public class AdminDeletionRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string AdminIdToDelete { get; set; } = string.Empty;
    public string AdminToDeleteEmail { get; set; } = string.Empty;
    public string AdminToDeleteName { get; set; } = string.Empty;
    public string RequestedByAdminId { get; set; } = string.Empty;
    public string RequestedByAdminEmail { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? ApprovedByAdminId { get; set; }
    public string? ApprovedByAdminEmail { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

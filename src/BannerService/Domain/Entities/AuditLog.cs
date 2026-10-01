namespace BannerService.Domain.Entities;

/// <summary>
/// Who called what and with what outcome. Deliberately holds no request or response bodies,
/// so the log itself never contains personal or health information.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? ShopId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? IpAddress { get; set; }
    public int DurationMs { get; set; }
}

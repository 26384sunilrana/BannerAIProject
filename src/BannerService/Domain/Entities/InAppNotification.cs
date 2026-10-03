namespace BannerService.Domain.Entities;

/// <summary>A message shown inside the application to one user (the bell). One row per recipient, so each person reads and clears their own.</summary>
public class InAppNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;

    /// <summary>A short code for what happened, for example WorkflowSubmitted, WorkflowApproved, WorkflowRejected, AdOverridden.</summary>
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>A page of this application the message points to, such as /approvals. Always starts with a single slash.</summary>
    public string? LinkUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

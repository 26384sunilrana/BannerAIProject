namespace BannerService.Application.DTOs
{
    public class PublishEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public Guid ActorId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string? Comment { get; set; }
        public string? Outcome { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class PublishWorkflowDto
    {
        public Guid Id { get; set; }
        public Guid BannerId { get; set; }
        public Guid ShopId { get; set; }
        public Guid SubmittedByUserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }
        public List<PublishEventDto> Events { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ApprovalRequestDto
    {
        public Guid Id { get; set; }
        public Guid PublishWorkflowId { get; set; }
        public Guid ReviewerId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerEmail { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public string? DecisionComment { get; set; }
        public DateTime? DecisionMadeAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsPending { get; set; }
    }

    public class PublishWorkflowDetailDto
    {
        public PublishWorkflowDto Workflow { get; set; } = new();
        public List<ApprovalRequestDto> ApprovalRequests { get; set; } = new();
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }
    }

    public class SubmitForApprovalDto
    {
        public Guid BannerId { get; set; }
    }

    public class ApproveWorkflowDto
    {
        public string? Comment { get; set; }
    }

    public class RejectWorkflowDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class RequestApprovalDto
    {
        public Guid ReviewerId { get; set; }
        public string ReviewerEmail { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }

    public class ApproveRequestDto
    {
        public string? Comment { get; set; }
    }

    public class RejectRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class PublishWorkflowListDto
    {
        public Guid Id { get; set; }
        public Guid BannerId { get; set; }
        public string BannerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid SubmittedByUserId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int PendingApprovalsCount { get; set; }
    }
}

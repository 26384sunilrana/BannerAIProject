namespace BannerService.Domain.Entities
{
    using Domain.ValueObjects;

    public enum PublishStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Approved = 2,
        Published = 3,
        Rejected = 4,
        Unpublished = 5,
        Archived = 6
    }

    public class PublishWorkflow
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BannerId { get; set; }
        public Guid ShopId { get; set; }
        public Guid SubmittedByUserId { get; set; }
        public PublishStatus Status { get; set; } = PublishStatus.Draft;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }
        public List<PublishEvent> Events { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public void SubmitForApproval(Guid userId, string userName)
        {
            Status = PublishStatus.PendingApproval;
            SubmittedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            Events.Add(new PublishEvent
            {
                EventType = PublishEventType.SubmittedForApproval,
                ActorId = userId,
                ActorName = userName,
                OccurredAt = DateTime.UtcNow,
                Outcome = "Success"
            });
        }

        public void Approve(Guid reviewerId, string reviewerName, string? comment = null)
        {
            Status = PublishStatus.Approved;
            ApprovedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            Events.Add(new PublishEvent
            {
                EventType = PublishEventType.ApprovedByReviewer,
                ActorId = reviewerId,
                ActorName = reviewerName,
                OccurredAt = DateTime.UtcNow,
                Comment = comment,
                Outcome = "Approved"
            });
        }

        public void Reject(Guid reviewerId, string reviewerName, string reason)
        {
            Status = PublishStatus.Rejected;
            RejectedAt = DateTime.UtcNow;
            RejectionReason = reason;
            UpdatedAt = DateTime.UtcNow;

            Events.Add(new PublishEvent
            {
                EventType = PublishEventType.RejectedByReviewer,
                ActorId = reviewerId,
                ActorName = reviewerName,
                OccurredAt = DateTime.UtcNow,
                Comment = reason,
                Outcome = "Rejected"
            });
        }

        public void Publish(Guid userId, string userName)
        {
            if (Status != PublishStatus.Approved)
                throw new InvalidOperationException("Can only publish approved workflows");

            Status = PublishStatus.Published;
            PublishedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            Events.Add(new PublishEvent
            {
                EventType = PublishEventType.PublishedSuccessfully,
                ActorId = userId,
                ActorName = userName,
                OccurredAt = DateTime.UtcNow,
                Outcome = "Published"
            });
        }

        public void Unpublish(Guid userId, string userName)
        {
            Status = PublishStatus.Unpublished;
            UpdatedAt = DateTime.UtcNow;

            Events.Add(new PublishEvent
            {
                EventType = PublishEventType.UnpublishedByOwner,
                ActorId = userId,
                ActorName = userName,
                OccurredAt = DateTime.UtcNow,
                Outcome = "Unpublished"
            });
        }

        public bool IsPending => Status == PublishStatus.PendingApproval;
        public bool IsApproved => Status == PublishStatus.Approved;
        public bool IsPublished => Status == PublishStatus.Published;
        public bool IsRejected => Status == PublishStatus.Rejected;
        public bool CanBePublished => Status == PublishStatus.Approved;
        public bool CanBeRejected => Status == PublishStatus.PendingApproval;
    }

    public class ApprovalRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PublishWorkflowId { get; set; }
        public Guid ReviewerId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerEmail { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false;
        public string? DecisionComment { get; set; }
        public DateTime? DecisionMadeAt { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public bool IsPending => DecisionMadeAt == null;

        public void Approve(string? comment = null)
        {
            IsApproved = true;
            DecisionComment = comment;
            DecisionMadeAt = DateTime.UtcNow;
        }

        public void Reject(string reason)
        {
            IsApproved = false;
            DecisionComment = reason;
            DecisionMadeAt = DateTime.UtcNow;
        }
    }
}

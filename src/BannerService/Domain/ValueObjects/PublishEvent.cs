namespace BannerService.Domain.ValueObjects
{
    public enum PublishEventType
    {
        SubmittedForApproval = 0,
        ApprovalRequested = 1,
        ApprovedByReviewer = 2,
        RejectedByReviewer = 3,
        PublishedSuccessfully = 4,
        UnpublishedByOwner = 5,
        ArchiveInitiated = 6
    }

    public class PublishEvent
    {
        public PublishEventType EventType { get; set; }
        public Guid ActorId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string? Comment { get; set; }
        public string? Outcome { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}

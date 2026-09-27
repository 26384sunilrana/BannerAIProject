namespace BannerService.Domain.Entities
{
    using Domain.ValueObjects;

    public enum AdStatus
    {
        Draft = 0,
        Active = 1,
        Paused = 2,
        Completed = 3,
        Cancelled = 4,
        Archived = 5
    }

    public enum AdType
    {
        Banner = 0,
        Text = 1,
        Video = 2,
        Native = 3,
        Carousel = 4
    }

    public class Advertisement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AdType Type { get; set; } = AdType.Banner;
        public AdStatus Status { get; set; } = AdStatus.Draft;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? ClickUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal BudgetLimit { get; set; } = 0m;
        public decimal DailyBudgetLimit { get; set; } = 0m;
        public AdTarget Target { get; set; } = new();
        public AdMetrics Metrics { get; set; } = new();
        public int Priority { get; set; } = 1;
        public bool IsActive { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public void Activate(Guid userId)
        {
            if (Status != AdStatus.Draft && Status != AdStatus.Paused)
                throw new InvalidOperationException("Can only activate draft or paused advertisements");

            Status = AdStatus.Active;
            IsActive = true;
            PublishedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Pause(Guid userId)
        {
            if (Status != AdStatus.Active)
                throw new InvalidOperationException("Can only pause active advertisements");

            Status = AdStatus.Paused;
            IsActive = false;
            PausedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Complete(Guid userId)
        {
            if (Status != AdStatus.Active && Status != AdStatus.Paused)
                throw new InvalidOperationException("Can only complete active or paused advertisements");

            Status = AdStatus.Completed;
            IsActive = false;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel(Guid userId)
        {
            if (Status == AdStatus.Completed || Status == AdStatus.Cancelled || Status == AdStatus.Archived)
                throw new InvalidOperationException("Cannot cancel completed, cancelled, or archived advertisements");

            Status = AdStatus.Cancelled;
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Archive(Guid userId)
        {
            Status = AdStatus.Archived;
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsBudgetExceeded()
        {
            return Metrics.TotalSpent >= BudgetLimit && BudgetLimit > 0;
        }

        public bool IsDailyBudgetExceeded()
        {
            var today = DateTime.UtcNow.Date;
            return false; // Would need to track daily spend separately
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow > EndDate;
        }

        public bool CanBeEdited()
        {
            return Status == AdStatus.Draft;
        }

        public bool CanBePaused()
        {
            return Status == AdStatus.Active;
        }

        public bool CanBeActivated()
        {
            return Status == AdStatus.Draft || Status == AdStatus.Paused;
        }
    }

    public class AdMetricsHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AdvertisementId { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public long Conversions { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public long Views { get; set; }
        public long Engagements { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}

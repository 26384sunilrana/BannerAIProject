namespace BannerService.Domain.Entities
{
    using Domain.ValueObjects;

    public enum ReportType
    {
        Summary = 0,
        Subscription = 1,
        Revenue = 2,
        Banner = 3,
        Advertisement = 4,
        Engagement = 5,
        Performance = 6,
        Comparison = 7
    }

    public enum ReportStatus
    {
        Draft = 0,
        Generated = 1,
        Exported = 2,
        Archived = 3
    }

    public class DashboardReport
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public ReportType Type { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Draft;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ReportMetric> Metrics { get; set; } = new();
        public Dictionary<string, object> ComparisonData { get; set; } = new();
        public Dictionary<string, List<object>> TrendData { get; set; } = new();
        public string? ExportFormat { get; set; } // PDF, Excel, JSON, CSV
        public string? ExportUrl { get; set; }
        public int TotalRecords { get; set; } = 0;
        public string? Summary { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? GeneratedAt { get; set; }
        public DateTime? ExportedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public void Generate()
        {
            Status = ReportStatus.Generated;
            GeneratedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Export(string format, string url)
        {
            if (Status != ReportStatus.Generated)
                throw new InvalidOperationException("Can only export generated reports");

            ExportFormat = format;
            ExportUrl = url;
            Status = ReportStatus.Exported;
            ExportedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Archive()
        {
            Status = ReportStatus.Archived;
            UpdatedAt = DateTime.UtcNow;
        }

        public decimal GetAverageMetricValue()
        {
            if (Metrics.Count == 0)
                return 0;
            return Metrics.Average(m => m.CurrentValue);
        }

        public decimal GetTotalMetricValue()
        {
            return Metrics.Sum(m => m.CurrentValue);
        }

        public List<ReportMetric> GetMetricsByTrend(TrendDirection trend)
        {
            return Metrics.Where(m => m.GetTrend() == trend).ToList();
        }

        public bool IsExpired(int retentionDays = 90)
        {
            return (DateTime.UtcNow - CreatedAt).TotalDays > retentionDays;
        }
    }

    public enum EventType
    {
        BannerCreated = 0,
        BannerPublished = 1,
        BannerViewed = 2,
        BannerClicked = 3,
        AdCreated = 4,
        AdActivated = 5,
        AdPaused = 6,
        SubscriptionCreated = 7,
        SubscriptionCancelled = 8,
        SubscriptionRenewed = 9,
        PaymentProcessed = 10,
        PaymentFailed = 11,
        UserLogin = 12,
        UserLogout = 13,
        ReportGenerated = 14,
        ReportExported = 15
    }

    public class AnalyticsEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public Guid? UserId { get; set; }
        public EventType EventType { get; set; }
        public string EventName { get; set; } = string.Empty;
        public Guid? ResourceId { get; set; }
        public string? ResourceType { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? SessionId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public static AnalyticsEvent CreateViewEvent(Guid shopId, Guid? userId, Guid resourceId, string resourceType)
        {
            return new AnalyticsEvent
            {
                ShopId = shopId,
                UserId = userId,
                EventType = EventType.BannerViewed,
                EventName = "View",
                ResourceId = resourceId,
                ResourceType = resourceType,
                OccurredAt = DateTime.UtcNow
            };
        }

        public static AnalyticsEvent CreateClickEvent(Guid shopId, Guid? userId, Guid resourceId, string resourceType)
        {
            return new AnalyticsEvent
            {
                ShopId = shopId,
                UserId = userId,
                EventType = EventType.BannerClicked,
                EventName = "Click",
                ResourceId = resourceId,
                ResourceType = resourceType,
                OccurredAt = DateTime.UtcNow
            };
        }

        public static AnalyticsEvent CreateConversionEvent(Guid shopId, Guid? userId, Guid resourceId, string resourceType)
        {
            return new AnalyticsEvent
            {
                ShopId = shopId,
                UserId = userId,
                EventType = EventType.PaymentProcessed,
                EventName = "Conversion",
                ResourceId = resourceId,
                ResourceType = resourceType,
                OccurredAt = DateTime.UtcNow
            };
        }
    }
}

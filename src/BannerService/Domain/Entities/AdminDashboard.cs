namespace BannerService.Domain.Entities
{
    using ValueObjects;

    public class AdminDashboard
    {
        public Guid Id { get; set; }
        public DashboardSummary CurrentSummary { get; set; } = new();
        public DateTime LastUpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public AdminDashboard()
        {
            Id = Guid.NewGuid();
            LastUpdatedAt = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateSummary(DashboardSummary summary)
        {
            CurrentSummary = summary;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public bool NeedsRefresh(int refreshIntervalMinutes = 5)
        {
            return DateTime.UtcNow.Subtract(LastUpdatedAt).TotalMinutes >= refreshIntervalMinutes;
        }
    }

    public class DashboardMetricSnapshot
    {
        public Guid Id { get; set; }
        public DateTime SnapshotDate { get; set; }
        public string MetricType { get; set; } = string.Empty;
        public decimal MetricValue { get; set; }
        public Dictionary<string, object> MetadataJson { get; set; } = new();

        public DashboardMetricSnapshot()
        {
            Id = Guid.NewGuid();
            SnapshotDate = DateTime.UtcNow;
        }

        public static DashboardMetricSnapshot CreateFromTrend(DashboardTrend trend)
        {
            return new DashboardMetricSnapshot
            {
                MetricType = trend.MetricName,
                MetricValue = trend.CurrentValue,
                SnapshotDate = trend.MeasuredAt,
                MetadataJson = new()
                {
                    { "PreviousValue", trend.PreviousValue },
                    { "PercentageChange", trend.PercentageChange },
                    { "Trend", trend.Trend }
                }
            };
        }
    }

    public class AdminDashboardAlert
    {
        public Guid Id { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public bool IsResolved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public AdminDashboardAlert()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            IsResolved = false;
        }

        public static AdminDashboardAlert CreateWarning(string alertType, string message)
        {
            return new AdminDashboardAlert
            {
                AlertType = alertType,
                Message = message,
                Severity = AlertSeverity.Warning
            };
        }

        public static AdminDashboardAlert CreateCritical(string alertType, string message)
        {
            return new AdminDashboardAlert
            {
                AlertType = alertType,
                Message = message,
                Severity = AlertSeverity.Critical
            };
        }

        public void Resolve()
        {
            IsResolved = true;
            ResolvedAt = DateTime.UtcNow;
        }
    }

    public enum AlertSeverity
    {
        Info = 0,
        Warning = 1,
        Critical = 2
    }
}

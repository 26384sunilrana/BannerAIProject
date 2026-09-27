namespace BannerService.Domain.Entities
{
    using Domain.ValueObjects;

    public class ShopOwnerDashboard
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public ShopDashboardMetrics CurrentSummary { get; set; } = new();
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public void UpdateSummary(ShopDashboardMetrics summary)
        {
            CurrentSummary = summary;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public bool NeedsRefresh(int intervalMinutes = 5)
        {
            return (DateTime.UtcNow - LastUpdatedAt).TotalMinutes >= intervalMinutes;
        }
    }

    public class ShopDashboardMetricSnapshot
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
        public string MetricType { get; set; } = string.Empty;
        public decimal MetricValue { get; set; }
        public string? MetadataJson { get; set; }
    }

    public enum ShopAlertSeverity
    {
        Info = 0,
        Warning = 1,
        Critical = 2
    }

    public class ShopDashboardAlert
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ShopAlertSeverity Severity { get; set; } = ShopAlertSeverity.Info;
        public bool IsResolved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public static ShopDashboardAlert CreateWarning(Guid shopId, string alertType, string message)
        {
            return new ShopDashboardAlert
            {
                ShopId = shopId,
                AlertType = alertType,
                Message = message,
                Severity = ShopAlertSeverity.Warning
            };
        }

        public static ShopDashboardAlert CreateCritical(Guid shopId, string alertType, string message)
        {
            return new ShopDashboardAlert
            {
                ShopId = shopId,
                AlertType = alertType,
                Message = message,
                Severity = ShopAlertSeverity.Critical
            };
        }

        public void Resolve()
        {
            IsResolved = true;
            ResolvedAt = DateTime.UtcNow;
        }
    }
}

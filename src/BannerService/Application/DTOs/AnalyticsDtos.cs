namespace BannerService.Application.DTOs
{
    public class ReportMetricDto
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercentage { get; set; }
        public string Trend { get; set; } = string.Empty;
        public DateTime MeasuredAt { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? Category { get; set; }
    }

    public class DashboardReportDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ReportMetricDto> Metrics { get; set; } = new();
        public Dictionary<string, object> ComparisonData { get; set; } = new();
        public Dictionary<string, List<object>> TrendData { get; set; } = new();
        public string? ExportFormat { get; set; }
        public string? ExportUrl { get; set; }
        public int TotalRecords { get; set; }
        public string? Summary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public DateTime? ExportedAt { get; set; }
    }

    public class AnalyticsEventDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public Guid? UserId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public Guid? ResourceId { get; set; }
        public string? ResourceType { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public string? IpAddress { get; set; }
        public string? SessionId { get; set; }
        public DateTime OccurredAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GenerateReportDto
    {
        public string ReportType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    public class ExportReportDto
    {
        public string Format { get; set; } = "PDF"; // PDF, Excel, JSON, CSV
    }

    public class RecordEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public Guid? ResourceId { get; set; }
        public string? ResourceType { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
    }

    public class EventCountDto
    {
        public string EventType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class MetricsComparisonDto
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal Period1Value { get; set; }
        public decimal Period2Value { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercentage { get; set; }
        public string Trend { get; set; } = string.Empty;
    }

    public class TrendAnalysisDto
    {
        public string MetricName { get; set; } = string.Empty;
        public List<TrendPointDto> Points { get; set; } = new();
    }

    public class TrendPointDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    public class ReportListDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalRecords { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public DateTime? ExportedAt { get; set; }
    }

    public class ConversionMetricsDto
    {
        public long Views { get; set; }
        public long Clicks { get; set; }
        public long Conversions { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal EngagementRate { get; set; }
    }

    public class ReportSummaryDto
    {
        public Guid ReportId { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public int MetricsCount { get; set; }
        public decimal AverageMetricValue { get; set; }
        public decimal TotalMetricValue { get; set; }
        public List<string> TopMetrics { get; set; } = new();
    }
}

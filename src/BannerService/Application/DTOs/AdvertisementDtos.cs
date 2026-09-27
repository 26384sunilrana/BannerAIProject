namespace BannerService.Application.DTOs
{
    public class AdTargetDto
    {
        public List<string> AudienceSegments { get; set; } = new();
        public List<string> DemographicFilters { get; set; } = new();
        public List<string> TargetLocations { get; set; } = new();
        public List<string> ExcludedLocations { get; set; } = new();
        public List<string> DeviceTypes { get; set; } = new();
        public List<string> OperatingSystems { get; set; } = new();
        public bool TargetAllUsers { get; set; } = false;
        public int? MinimumAge { get; set; }
        public int? MaximumAge { get; set; }
        public List<string> InterestCategories { get; set; } = new();
        public Dictionary<string, string> CustomAttributes { get; set; } = new();
    }

    public class AdMetricsDto
    {
        public long Impressions { get; set; } = 0;
        public long Clicks { get; set; } = 0;
        public long Conversions { get; set; } = 0;
        public decimal TotalSpent { get; set; } = 0m;
        public decimal EstimatedRevenue { get; set; } = 0m;
        public DateTime LastUpdatedAt { get; set; }
        public long Views { get; set; } = 0;
        public long Engagements { get; set; } = 0;
        public decimal ClickThroughRate { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal CostPerClick { get; set; }
        public decimal CostPerConversion { get; set; }
        public decimal ROI { get; set; }
        public decimal EngagementRate { get; set; }
    }

    public class CreateAdvertisementDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = "Banner";
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? ClickUrl { get; set; }
    }

    public class UpdateAdvertisementDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? ClickUrl { get; set; }
    }

    public class SetAdTargetDto
    {
        public AdTargetDto Target { get; set; } = new();
    }

    public class SetBudgetDto
    {
        public decimal BudgetLimit { get; set; }
        public decimal DailyBudgetLimit { get; set; }
    }

    public class SetAdDatesDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class AdvertisementDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? ClickUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal BudgetLimit { get; set; }
        public decimal DailyBudgetLimit { get; set; }
        public AdTargetDto Target { get; set; } = new();
        public AdMetricsDto Metrics { get; set; } = new();
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsBudgetExceeded { get; set; }
        public bool IsExpired { get; set; }
    }

    public class AdvertisementListDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal BudgetLimit { get; set; }
        public decimal TotalSpent { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateAdMetricsDto
    {
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public long Conversions { get; set; }
        public decimal Spend { get; set; }
    }

    public class AdPerformanceDto
    {
        public Guid AdId { get; set; }
        public string Title { get; set; } = string.Empty;
        public AdMetricsDto Metrics { get; set; } = new();
        public decimal BudgetRemaining { get; set; }
        public decimal BudgetUtilization { get; set; }
        public string HealthStatus { get; set; } = string.Empty; // Healthy, Warning, Critical
    }
}

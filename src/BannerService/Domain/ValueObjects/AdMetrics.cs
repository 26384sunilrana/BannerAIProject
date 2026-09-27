namespace BannerService.Domain.ValueObjects
{
    public class AdMetrics
    {
        public long Impressions { get; set; } = 0;
        public long Clicks { get; set; } = 0;
        public long Conversions { get; set; } = 0;
        public decimal TotalSpent { get; set; } = 0m;
        public decimal EstimatedRevenue { get; set; } = 0m;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public long Views { get; set; } = 0;
        public long Engagements { get; set; } = 0;

        public decimal GetClickThroughRate()
        {
            if (Impressions == 0)
                return 0m;
            return (Clicks / (decimal)Impressions) * 100;
        }

        public decimal GetConversionRate()
        {
            if (Clicks == 0)
                return 0m;
            return (Conversions / (decimal)Clicks) * 100;
        }

        public decimal GetCostPerClick()
        {
            if (Clicks == 0)
                return 0m;
            return TotalSpent / Clicks;
        }

        public decimal GetCostPerConversion()
        {
            if (Conversions == 0)
                return 0m;
            return TotalSpent / Conversions;
        }

        public decimal GetROI()
        {
            if (TotalSpent == 0)
                return 0m;
            return ((EstimatedRevenue - TotalSpent) / TotalSpent) * 100;
        }

        public decimal GetEngagementRate()
        {
            if (Impressions == 0)
                return 0m;
            return (Engagements / (decimal)Impressions) * 100;
        }
    }
}

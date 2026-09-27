namespace BannerService.Domain.ValueObjects
{
    public enum TrendDirection
    {
        Up = 0,
        Down = 1,
        Stable = 2
    }

    public class ReportMetric
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal PreviousValue { get; set; }
        public DateTime MeasuredAt { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? Category { get; set; }

        public decimal GetChangeAmount()
        {
            return CurrentValue - PreviousValue;
        }

        public decimal GetChangePercentage()
        {
            if (PreviousValue == 0)
                return CurrentValue > 0 ? 100 : 0;
            return ((CurrentValue - PreviousValue) / PreviousValue) * 100;
        }

        public TrendDirection GetTrend()
        {
            var change = GetChangeAmount();
            if (change > 0)
                return TrendDirection.Up;
            if (change < 0)
                return TrendDirection.Down;
            return TrendDirection.Stable;
        }

        public bool IsPositiveTrend()
        {
            return CurrentValue > PreviousValue;
        }

        public bool IsNegativeTrend()
        {
            return CurrentValue < PreviousValue;
        }
    }
}

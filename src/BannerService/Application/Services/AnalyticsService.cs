namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.ValueObjects;
    using Microsoft.EntityFrameworkCore;

    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _repository;

        public AnalyticsService(IAnalyticsRepository repository)
        {
            _repository = repository;
        }

        public async Task<DashboardReport> GenerateReportAsync(Guid shopId, ReportType type, DateTime startDate, DateTime endDate, Guid userId)
        {
            var report = new DashboardReport
            {
                ShopId = shopId,
                CreatedByUserId = userId,
                Type = type,
                Title = $"{type} Report - {startDate:MMM dd} to {endDate:MMM dd}",
                Description = $"Generated {type.ToString().ToLower()} report for the period",
                StartDate = startDate,
                EndDate = endDate,
                Status = ReportStatus.Draft
            };

            var events = await _repository.GetEventsByShopAsync(shopId, startDate, endDate);
            report.TotalRecords = events.Count;

            // Populate metrics based on events
            await PopulateMetricsAsync(report, shopId, startDate, endDate, type);

            report.Generate();
            return await _repository.CreateReportAsync(report);
        }

        public async Task<DashboardReport> GetReportAsync(Guid reportId)
        {
            var report = await _repository.GetReportByIdAsync(reportId);
            if (report == null)
                throw new KeyNotFoundException($"Report {reportId} not found");
            return report;
        }

        public async Task<List<DashboardReport>> GetShopReportsAsync(Guid shopId)
        {
            return await _repository.GetShopReportsAsync(shopId);
        }

        public async Task<DashboardReport> ExportReportAsync(Guid reportId, string format)
        {
            var report = await GetReportAsync(reportId);
            var url = $"/reports/{reportId}/export.{format.ToLower()}";
            report.Export(format, url);
            return await _repository.UpdateReportAsync(report);
        }

        public async Task<bool> DeleteReportAsync(Guid reportId)
        {
            return await _repository.DeleteReportAsync(reportId);
        }

        public async Task RecordEventAsync(Guid shopId, EventType eventType, Guid? userId = null, Guid? resourceId = null, string? resourceType = null, Dictionary<string, object>? properties = null)
        {
            var evt = new AnalyticsEvent
            {
                ShopId = shopId,
                UserId = userId,
                EventType = eventType,
                EventName = eventType.ToString(),
                ResourceId = resourceId,
                ResourceType = resourceType,
                Properties = properties ?? new(),
                OccurredAt = DateTime.UtcNow
            };

            await _repository.CreateEventAsync(evt);
        }

        public async Task<List<AnalyticsEvent>> GetEventsAsync(Guid shopId, DateTime startDate, DateTime endDate, EventType? filterByType = null)
        {
            var events = await _repository.GetEventsByShopAsync(shopId, startDate, endDate);
            if (filterByType.HasValue)
                events = events.Where(e => e.EventType == filterByType.Value).ToList();
            return events;
        }

        public async Task<Dictionary<string, int>> GetEventCountByTypeAsync(Guid shopId, DateTime startDate, DateTime endDate)
        {
            var events = await _repository.GetEventsByShopAsync(shopId, startDate, endDate);
            return events
                .GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
        }

        public async Task<decimal> GetConversionRateAsync(Guid shopId, DateTime startDate, DateTime endDate)
        {
            var events = await _repository.GetEventsByShopAsync(shopId, startDate, endDate);
            var views = events.Count(e => e.EventType == EventType.BannerViewed);
            var conversions = events.Count(e => e.EventType == EventType.PaymentProcessed);

            if (views == 0)
                return 0;
            return (conversions / (decimal)views) * 100;
        }

        public async Task<decimal> GetEngagementRateAsync(Guid shopId, DateTime startDate, DateTime endDate)
        {
            var events = await _repository.GetEventsByShopAsync(shopId, startDate, endDate);
            var views = events.Count(e => e.EventType == EventType.BannerViewed);
            var clicks = events.Count(e => e.EventType == EventType.BannerClicked);

            if (views == 0)
                return 0;
            return (clicks / (decimal)views) * 100;
        }

        public async Task<Dictionary<string, decimal>> GetMetricsComparisonAsync(Guid shopId, DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End)
        {
            var period1Events = await _repository.GetEventsByShopAsync(shopId, period1Start, period1End);
            var period2Events = await _repository.GetEventsByShopAsync(shopId, period2Start, period2End);

            var comparison = new Dictionary<string, decimal>
            {
                { "Period1_Events", period1Events.Count },
                { "Period2_Events", period2Events.Count },
                { "Event_Growth", ((period2Events.Count - period1Events.Count) / (decimal)(period1Events.Count > 0 ? period1Events.Count : 1)) * 100 },
                { "Period1_ConversionRate", GetConversionRate(period1Events) },
                { "Period2_ConversionRate", GetConversionRate(period2Events) },
                { "Period1_EngagementRate", GetEngagementRate(period1Events) },
                { "Period2_EngagementRate", GetEngagementRate(period2Events) }
            };

            return comparison;
        }

        public Task<List<ReportMetric>> GetTrendAnalysisAsync(Guid shopId, string metricName, int dayCount)
        {
            var trends = new List<ReportMetric>();
            var today = DateTime.UtcNow.Date;

            for (int i = dayCount; i > 0; i--)
            {
                var date = today.AddDays(-i);
                var nextDate = date.AddDays(1);
                var events = _repository.GetEventsByShopAsync(shopId, date, nextDate).Result;

                var value = metricName.ToLower() switch
                {
                    "views" => events.Count(e => e.EventType == EventType.BannerViewed),
                    "clicks" => events.Count(e => e.EventType == EventType.BannerClicked),
                    "conversions" => events.Count(e => e.EventType == EventType.PaymentProcessed),
                    _ => events.Count
                };

                trends.Add(new ReportMetric
                {
                    MetricName = metricName,
                    CurrentValue = value,
                    PreviousValue = trends.LastOrDefault()?.CurrentValue ?? value,
                    MeasuredAt = date,
                    Unit = "count"
                });
            }

            return Task.FromResult(trends);
        }

        public async Task<DashboardReport> GenerateComparisonReportAsync(Guid shopId, DateTime startDate, DateTime endDate, Guid userId)
        {
            var report = await GenerateReportAsync(shopId, ReportType.Comparison, startDate, endDate, userId);
            var midDate = startDate.AddDays((endDate - startDate).TotalDays / 2);
            var comparison = await GetMetricsComparisonAsync(shopId, startDate, midDate, midDate, endDate);
            report.ComparisonData = comparison.ToDictionary(k => k.Key, k => (object)k.Value);
            return await _repository.UpdateReportAsync(report);
        }

        public async Task<DashboardReport> GenerateSubscriptionReportAsync(Guid shopId, DateTime startDate, DateTime endDate, Guid userId)
        {
            var report = await GenerateReportAsync(shopId, ReportType.Subscription, startDate, endDate, userId);
            // Add subscription-specific metrics
            return await _repository.UpdateReportAsync(report);
        }

        public async Task<DashboardReport> GenerateRevenueReportAsync(Guid shopId, DateTime startDate, DateTime endDate, Guid userId)
        {
            var report = await GenerateReportAsync(shopId, ReportType.Revenue, startDate, endDate, userId);
            // Add revenue-specific metrics
            return await _repository.UpdateReportAsync(report);
        }

        public async Task<DashboardReport> GenerateBannerReportAsync(Guid shopId, DateTime startDate, DateTime endDate, Guid userId)
        {
            var report = await GenerateReportAsync(shopId, ReportType.Banner, startDate, endDate, userId);
            var events = await _repository.GetEventsByShopAsync(shopId, startDate, endDate);
            var bannerViews = events.Count(e => e.EventType == EventType.BannerViewed);
            var bannerClicks = events.Count(e => e.EventType == EventType.BannerClicked);

            report.Metrics.Add(new ReportMetric
            {
                MetricName = "Total Views",
                CurrentValue = bannerViews,
                Unit = "views"
            });
            report.Metrics.Add(new ReportMetric
            {
                MetricName = "Total Clicks",
                CurrentValue = bannerClicks,
                Unit = "clicks"
            });

            return await _repository.UpdateReportAsync(report);
        }

        public async Task<DashboardReport> GenerateAdvertisementReportAsync(Guid shopId, DateTime startDate, DateTime endDate, Guid userId)
        {
            var report = await GenerateReportAsync(shopId, ReportType.Advertisement, startDate, endDate, userId);
            var events = await _repository.GetEventsByShopAsync(shopId, startDate, endDate);
            var adClicks = events.Count(e => e.EventType == EventType.BannerClicked && e.ResourceType == "Advertisement");

            report.Metrics.Add(new ReportMetric
            {
                MetricName = "Ad Clicks",
                CurrentValue = adClicks,
                Unit = "clicks"
            });

            return await _repository.UpdateReportAsync(report);
        }

        public async Task CleanupOldEventsAsync(int daysToKeep = 365)
        {
            await _repository.DeleteOldEventsAsync(daysToKeep);
        }

        public async Task CleanupExpiredReportsAsync(int retentionDays = 90)
        {
            var expiredReports = await _repository.GetExpiringReportsAsync(retentionDays);
            foreach (var report in expiredReports)
            {
                await _repository.DeleteReportAsync(report.Id);
            }
        }

        private async Task PopulateMetricsAsync(DashboardReport report, Guid shopId, DateTime startDate, DateTime endDate, ReportType type)
        {
            var events = await _repository.GetEventsByShopAsync(shopId, startDate, endDate);

            var viewCount = events.Count(e => e.EventType == EventType.BannerViewed);
            var clickCount = events.Count(e => e.EventType == EventType.BannerClicked);
            var conversionCount = events.Count(e => e.EventType == EventType.PaymentProcessed);

            if (viewCount > 0)
                report.Metrics.Add(new ReportMetric { MetricName = "Views", CurrentValue = viewCount, Unit = "views" });
            if (clickCount > 0)
                report.Metrics.Add(new ReportMetric { MetricName = "Clicks", CurrentValue = clickCount, Unit = "clicks" });
            if (conversionCount > 0)
                report.Metrics.Add(new ReportMetric { MetricName = "Conversions", CurrentValue = conversionCount, Unit = "conversions" });
        }

        private decimal GetConversionRate(List<AnalyticsEvent> events)
        {
            var views = events.Count(e => e.EventType == EventType.BannerViewed);
            var conversions = events.Count(e => e.EventType == EventType.PaymentProcessed);
            if (views == 0) return 0;
            return (conversions / (decimal)views) * 100;
        }

        private decimal GetEngagementRate(List<AnalyticsEvent> events)
        {
            var views = events.Count(e => e.EventType == EventType.BannerViewed);
            var clicks = events.Count(e => e.EventType == EventType.BannerClicked);
            if (views == 0) return 0;
            return (clicks / (decimal)views) * 100;
        }
    }
}

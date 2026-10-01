namespace BannerService.Presentation.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Application.Services;
    using Application.DTOs;
    using Domain.Interfaces;
    using Domain.Entities;
    using Domain.ValueObjects;

    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly IAnalyticsRepository _repository;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            IAnalyticsService analyticsService,
            IAnalyticsRepository repository,
            ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _repository = repository;
            _logger = logger;
        }

        [HttpPost("reports/generate")]
        public async Task<IActionResult> GenerateReport(Guid shopId, [FromBody] GenerateReportDto dto)
        {
            var userId = User.GetUserId();
            try
            {
                if (!Enum.TryParse<ReportType>(dto.ReportType, out var reportType))
                    return BadRequest(new { success = false, message = "Invalid report type" });

                var report = await _analyticsService.GenerateReportAsync(
                    shopId, reportType, dto.StartDate, dto.EndDate, userId);

                return Ok(new { success = true, data = MapToDto(report) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("reports/{reportId}")]
        public async Task<IActionResult> GetReport(Guid reportId)
        {
            try
            {
                var report = await _analyticsService.GetReportAsync(reportId);
                return Ok(new { success = true, data = MapToDto(report) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("reports/shop/{shopId}")]
        public async Task<IActionResult> GetShopReports(Guid shopId)
        {
            try
            {
                var reports = await _analyticsService.GetShopReportsAsync(shopId);
                var dtos = reports.Select(MapToListDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shop reports");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("reports/{reportId}/export")]
        public async Task<IActionResult> ExportReport(Guid reportId, [FromBody] ExportReportDto dto)
        {
            try
            {
                var report = await _analyticsService.ExportReportAsync(reportId, dto.Format);
                return Ok(new { success = true, data = new { exportUrl = report.ExportUrl } });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("reports/{reportId}")]
        public async Task<IActionResult> DeleteReport(Guid reportId)
        {
            try
            {
                var result = await _analyticsService.DeleteReportAsync(reportId);
                if (!result)
                    return NotFound(new { success = false, message = "Report not found" });

                return Ok(new { success = true, message = "Report deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("events")]
        public async Task<IActionResult> RecordEvent(Guid shopId, [FromBody] RecordEventDto dto)
        {
            try
            {
                if (!Enum.TryParse<EventType>(dto.EventType, out var eventType))
                    return BadRequest(new { success = false, message = "Invalid event type" });

                await _analyticsService.RecordEventAsync(shopId, eventType, dto.UserId, dto.ResourceId, dto.ResourceType, dto.Properties);
                return Ok(new { success = true, message = "Event recorded" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording event");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("events/shop/{shopId}")]
        public async Task<IActionResult> GetEvents(Guid shopId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var events = await _analyticsService.GetEventsAsync(shopId, startDate, endDate);
                var dtos = events.Select(MapEventToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("events/counts/shop/{shopId}")]
        public async Task<IActionResult> GetEventCounts(Guid shopId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var counts = await _analyticsService.GetEventCountByTypeAsync(shopId, startDate, endDate);
                var dtos = counts.Select(kvp => new EventCountDto
                {
                    EventType = kvp.Key,
                    Count = kvp.Value,
                    Percentage = counts.Values.Sum() > 0 ? (kvp.Value / (decimal)counts.Values.Sum()) * 100 : 0
                }).ToList();

                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event counts");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("conversion-rate/shop/{shopId}")]
        public async Task<IActionResult> GetConversionRate(Guid shopId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var rate = await _analyticsService.GetConversionRateAsync(shopId, startDate, endDate);
                return Ok(new { success = true, data = new { conversionRate = rate } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating conversion rate");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("engagement-rate/shop/{shopId}")]
        public async Task<IActionResult> GetEngagementRate(Guid shopId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var rate = await _analyticsService.GetEngagementRateAsync(shopId, startDate, endDate);
                return Ok(new { success = true, data = new { engagementRate = rate } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating engagement rate");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("metrics/comparison/shop/{shopId}")]
        public async Task<IActionResult> GetMetricsComparison(
            Guid shopId,
            [FromQuery] DateTime period1Start, [FromQuery] DateTime period1End,
            [FromQuery] DateTime period2Start, [FromQuery] DateTime period2End)
        {
            try
            {
                var comparison = await _analyticsService.GetMetricsComparisonAsync(shopId, period1Start, period1End, period2Start, period2End);
                var dtos = comparison.Select(kvp => new MetricsComparisonDto
                {
                    MetricName = kvp.Key,
                    Period1Value = kvp.Value,
                    Period2Value = 0,
                    ChangeAmount = 0,
                    ChangePercentage = 0
                }).ToList();

                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving metrics comparison");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("trends/shop/{shopId}")]
        public async Task<IActionResult> GetTrendAnalysis(Guid shopId, [FromQuery] string metricName, [FromQuery] int dayCount = 30)
        {
            try
            {
                var trends = await _analyticsService.GetTrendAnalysisAsync(shopId, metricName, dayCount);
                var dto = new TrendAnalysisDto
                {
                    MetricName = metricName,
                    Points = trends.Select(t => new TrendPointDto
                    {
                        Date = t.MeasuredAt,
                        Value = t.CurrentValue
                    }).ToList()
                };

                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trend analysis");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("cleanup/events")]
        public async Task<IActionResult> CleanupOldEvents([FromQuery] int daysToKeep = 365)
        {
            try
            {
                await _analyticsService.CleanupOldEventsAsync(daysToKeep);
                return Ok(new { success = true, message = "Old events cleaned up" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up events");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("cleanup/reports")]
        public async Task<IActionResult> CleanupExpiredReports([FromQuery] int retentionDays = 90)
        {
            try
            {
                await _analyticsService.CleanupExpiredReportsAsync(retentionDays);
                return Ok(new { success = true, message = "Expired reports cleaned up" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up reports");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #region Mapping Helpers

        private DashboardReportDto MapToDto(DashboardReport report)
        {
            return new DashboardReportDto
            {
                Id = report.Id,
                ShopId = report.ShopId,
                CreatedByUserId = report.CreatedByUserId,
                Type = report.Type.ToString(),
                Status = report.Status.ToString(),
                Title = report.Title,
                Description = report.Description,
                StartDate = report.StartDate,
                EndDate = report.EndDate,
                Metrics = report.Metrics.Select(MapMetricToDto).ToList(),
                ComparisonData = report.ComparisonData,
                TrendData = report.TrendData,
                ExportFormat = report.ExportFormat,
                ExportUrl = report.ExportUrl,
                TotalRecords = report.TotalRecords,
                Summary = report.Summary,
                CreatedAt = report.CreatedAt,
                GeneratedAt = report.GeneratedAt,
                ExportedAt = report.ExportedAt
            };
        }

        private ReportListDto MapToListDto(DashboardReport report)
        {
            return new ReportListDto
            {
                Id = report.Id,
                Type = report.Type.ToString(),
                Status = report.Status.ToString(),
                Title = report.Title,
                StartDate = report.StartDate,
                EndDate = report.EndDate,
                TotalRecords = report.TotalRecords,
                CreatedAt = report.CreatedAt,
                GeneratedAt = report.GeneratedAt,
                ExportedAt = report.ExportedAt
            };
        }

        private ReportMetricDto MapMetricToDto(ReportMetric metric)
        {
            return new ReportMetricDto
            {
                MetricName = metric.MetricName,
                CurrentValue = metric.CurrentValue,
                PreviousValue = metric.PreviousValue,
                ChangeAmount = metric.GetChangeAmount(),
                ChangePercentage = metric.GetChangePercentage(),
                Trend = metric.GetTrend().ToString(),
                MeasuredAt = metric.MeasuredAt,
                Unit = metric.Unit,
                Category = metric.Category
            };
        }

        private AnalyticsEventDto MapEventToDto(AnalyticsEvent evt)
        {
            return new AnalyticsEventDto
            {
                Id = evt.Id,
                ShopId = evt.ShopId,
                UserId = evt.UserId,
                EventType = evt.EventType.ToString(),
                EventName = evt.EventName,
                ResourceId = evt.ResourceId,
                ResourceType = evt.ResourceType,
                Properties = evt.Properties,
                IpAddress = evt.IpAddress,
                SessionId = evt.SessionId,
                OccurredAt = evt.OccurredAt,
                CreatedAt = evt.CreatedAt
            };
        }

        #endregion
    }
}

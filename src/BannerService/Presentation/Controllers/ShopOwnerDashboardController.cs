namespace BannerService.Presentation.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Application.Services;
    using Application.DTOs;
    using Domain.Interfaces;
    using Domain.Entities;
    using Domain.ValueObjects;

    [ApiController]
    [Route("api/shop/{shopId}/dashboard")]
    public class ShopOwnerDashboardController : ControllerBase
    {
        private readonly IShopOwnerDashboardService _dashboardService;
        private readonly IShopOwnerDashboardRepository _dashboardRepository;
        private readonly ILogger<ShopOwnerDashboardController> _logger;

        public ShopOwnerDashboardController(
            IShopOwnerDashboardService dashboardService,
            IShopOwnerDashboardRepository dashboardRepository,
            ILogger<ShopOwnerDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _dashboardRepository = dashboardRepository;
            _logger = logger;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview(Guid shopId, [FromQuery] bool forceRefresh = false)
        {
            try
            {
                var summary = await _dashboardService.GetDashboardSummaryAsync(shopId, forceRefresh);
                var alerts = await _dashboardRepository.GetActiveAlertsAsync(shopId);
                var topBanners = await _dashboardService.GetTopBannersAsync(shopId, 5);
                var trends = await _dashboardService.GetTrendsAsync(shopId, 30);

                var summaryDto = MapSummaryToDto(summary);
                var alertDtos = alerts.Select(MapAlertToDto).ToList();
                var bannerDtos = topBanners.Select(MapBannerToDto).ToList();
                var trendDtos = trends.Select(MapTrendToDto).ToList();

                var overview = new ShopDashboardOverviewDto
                {
                    Summary = summaryDto,
                    ActiveAlerts = alertDtos,
                    KeyTrends = trendDtos,
                    TopBanners = bannerDtos
                };

                return Ok(new { success = true, data = overview });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard overview for shop {ShopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching dashboard" });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(Guid shopId)
        {
            try
            {
                var summary = await _dashboardService.GetDashboardSummaryAsync(shopId);
                var dto = MapSummaryToDto(summary);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard summary for shop {ShopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching summary" });
            }
        }

        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetSubscriptionMetrics(Guid shopId)
        {
            try
            {
                var metrics = await _dashboardService.GetSubscriptionMetricsAsync(shopId);
                var dto = MapSubscriptionMetricsToDto(metrics);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscription metrics for shop {ShopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching metrics" });
            }
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueMetrics(Guid shopId)
        {
            try
            {
                var metrics = await _dashboardService.GetRevenueMetricsAsync(shopId);
                var dto = MapRevenueMetricsToDto(metrics);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching revenue metrics for shop {ShopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching revenue" });
            }
        }

        [HttpGet("banners")]
        public async Task<IActionResult> GetBannerMetrics(Guid shopId)
        {
            try
            {
                var metrics = await _dashboardService.GetBannerMetricsAsync(shopId);
                var dto = MapBannerMetricsToDto(metrics);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching banner metrics for shop {ShopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching banner metrics" });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUserMetrics(Guid shopId)
        {
            try
            {
                var metrics = await _dashboardService.GetUserMetricsAsync(shopId);
                var dto = MapUserMetricsToDto(metrics);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user metrics for shop {ShopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching user metrics" });
            }
        }

        [HttpGet("trends")]
        public async Task<IActionResult> GetTrends(Guid shopId, [FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                    return BadRequest(new { success = false, message = "Days must be between 1 and 365" });

                var trends = await _dashboardService.GetTrendsAsync(shopId, days);
                var dtos = trends.Select(MapTrendToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching trends for shop {ShopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching trends" });
            }
        }

        [HttpGet("top-banners")]
        public async Task<IActionResult> GetTopBanners(Guid shopId, [FromQuery] int limit = 10)
        {
            try
            {
                if (limit < 1 || limit > 100)
                    return BadRequest(new { success = false, message = "Limit must be between 1 and 100" });

                var banners = await _dashboardService.GetTopBannersAsync(shopId, limit);
                var dtos = banners.Select(MapBannerToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top banners for shop {ShopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching banners" });
            }
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts(Guid shopId)
        {
            try
            {
                var alerts = await _dashboardRepository.GetActiveAlertsAsync(shopId);
                var dtos = alerts.Select(MapAlertToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alerts for shop {ShopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching alerts" });
            }
        }

        [HttpPost("alerts/{alertId}/resolve")]
        public async Task<IActionResult> ResolveAlert(Guid shopId, Guid alertId)
        {
            try
            {
                var result = await _dashboardRepository.ResolveAlertAsync(alertId);

                if (!result)
                    return NotFound(new { success = false, message = "Alert not found" });

                return Ok(new { success = true, message = "Alert resolved" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving alert {AlertId} for shop {ShopId}", alertId, shopId);
                return StatusCode(500, new { success = false, message = "Error resolving alert" });
            }
        }

        #region Mapping Helpers

        private ShopDashboardSummaryDto MapSummaryToDto(ShopDashboardMetrics summary)
        {
            return new ShopDashboardSummaryDto
            {
                GeneratedAt = summary.GeneratedAt,
                SubscriptionMetrics = MapSubscriptionMetricsToDto(summary.SubscriptionMetrics),
                RevenueMetrics = MapRevenueMetricsToDto(summary.RevenueMetrics),
                BannerMetrics = MapBannerMetricsToDto(summary.BannerMetrics),
                UserMetrics = MapUserMetricsToDto(summary.UserMetrics)
            };
        }

        private ShopSubscriptionMetricsDto MapSubscriptionMetricsToDto(ShopSubscriptionMetrics metrics)
        {
            return new ShopSubscriptionMetricsDto
            {
                TotalSubscriptions = metrics.TotalSubscriptions,
                ActiveSubscriptions = metrics.ActiveSubscriptions,
                TrialSubscriptions = metrics.TrialSubscriptions,
                ExpiringInDays = metrics.ExpiringInDays,
                PaymentFailedCount = metrics.PaymentFailedCount,
                SuspendedCount = metrics.SuspendedCount,
                AverageSubscriptionValue = metrics.AverageSubscriptionValue
            };
        }

        private ShopRevenueMetricsDto MapRevenueMetricsToDto(ShopRevenueMetrics metrics)
        {
            return new ShopRevenueMetricsDto
            {
                MonthlyCurringRevenue = metrics.MonthlyCurringRevenue,
                AnnualRecurringRevenue = metrics.AnnualRecurringRevenue,
                MonthlyRevenue = metrics.MonthlyRevenue,
                AnnualRevenue = metrics.AnnualRevenue,
                AverageRevenuePerSubscription = metrics.AverageRevenuePerSubscription,
                CollectionRate = metrics.CollectionRate,
                PendingPayments = metrics.PendingPayments,
                TotalOverdueAmount = metrics.TotalOverdueAmount
            };
        }

        private ShopBannerMetricsDto MapBannerMetricsToDto(ShopBannerMetrics metrics)
        {
            return new ShopBannerMetricsDto
            {
                TotalBanners = metrics.TotalBanners,
                PublishedBanners = metrics.PublishedBanners,
                DraftBanners = metrics.DraftBanners,
                BannersWithVersionControl = metrics.BannersWithVersionControl,
                TotalMediaStorageBytes = metrics.TotalMediaStorageBytes,
                TotalComponents = metrics.TotalComponents,
                TotalEffects = metrics.TotalEffects,
                TotalCarousels = metrics.TotalCarousels
            };
        }

        private ShopUserMetricsDto MapUserMetricsToDto(ShopUserMetrics metrics)
        {
            return new ShopUserMetricsDto
            {
                TotalUsers = metrics.TotalUsers,
                ActiveUsers = metrics.ActiveUsers,
                InactiveUsers = metrics.InactiveUsers,
                UsersWithFailedLogin = metrics.UsersWithFailedLogin,
                LockedOutUsers = metrics.LockedOutUsers
            };
        }

        private ShopDashboardTrendDto MapTrendToDto(ShopDashboardTrend trend)
        {
            return new ShopDashboardTrendDto
            {
                MetricName = trend.MetricName,
                CurrentValue = trend.CurrentValue,
                PreviousValue = trend.PreviousValue,
                PercentageChange = trend.PercentageChange,
                Trend = trend.Trend,
                MeasuredAt = trend.MeasuredAt
            };
        }

        private TopBannerByPerformanceDto MapBannerToDto(TopBannerByPerformance banner)
        {
            return new TopBannerByPerformanceDto
            {
                BannerId = banner.BannerId,
                BannerName = banner.BannerName,
                ComponentCount = banner.ComponentCount,
                EffectCount = banner.EffectCount,
                CreatedAt = banner.CreatedAt,
                PublishedAt = banner.PublishedAt
            };
        }

        private ShopDashboardAlertDto MapAlertToDto(ShopDashboardAlert alert)
        {
            return new ShopDashboardAlertDto
            {
                Id = alert.Id,
                AlertType = alert.AlertType,
                Message = alert.Message,
                Severity = alert.Severity.ToString(),
                IsResolved = alert.IsResolved,
                CreatedAt = alert.CreatedAt,
                ResolvedAt = alert.ResolvedAt
            };
        }

        #endregion
    }
}

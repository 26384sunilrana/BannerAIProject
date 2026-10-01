namespace BannerService.Presentation.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Application.Services;
    using Application.DTOs;
    using Domain.Interfaces;
    using Domain.ValueObjects;

    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    [Route("api/admin/dashboard")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IAdminDashboardRepository _dashboardRepository;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            IDashboardService dashboardService,
            IAdminDashboardRepository dashboardRepository,
            ILogger<AdminDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _dashboardRepository = dashboardRepository;
            _logger = logger;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] bool forceRefresh = false)
        {
            try
            {
                var summary = await _dashboardService.GetDashboardSummaryAsync(forceRefresh);
                var alerts = await _dashboardRepository.GetActiveAlertsAsync();
                var topShops = await _dashboardService.GetTopShopsByRevenueAsync(5);
                var trends = await _dashboardService.GetTrendsAsync(30);

                var summaryDto = MapSummaryToDto(summary);
                var alertDtos = alerts.Select(a => MapAlertToDto(a)).ToList();
                var shopDtos = topShops.Select(MapTopShopToDto).ToList();
                var trendDtos = trends.Select(MapTrendToDto).ToList();

                var overview = new DashboardOverviewDto
                {
                    Summary = summaryDto,
                    ActiveAlerts = alertDtos,
                    KeyTrends = trendDtos,
                    TopShops = shopDtos
                };

                return Ok(new { success = true, data = overview });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard overview");
                return StatusCode(500, new { success = false, message = "Error fetching dashboard" });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var summary = await _dashboardService.GetDashboardSummaryAsync();
                var dto = MapSummaryToDto(summary);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard summary");
                return StatusCode(500, new { success = false, message = "Error fetching summary" });
            }
        }

        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetSubscriptionMetrics()
        {
            try
            {
                var metrics = await _dashboardService.GetSubscriptionMetricsAsync();
                var dto = MapSubscriptionMetricsToDto(metrics);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscription metrics");
                return StatusCode(500, new { success = false, message = "Error fetching metrics" });
            }
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueMetrics()
        {
            try
            {
                var metrics = await _dashboardService.GetRevenueMetricsAsync();
                var dto = MapRevenueMetricsToDto(metrics);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching revenue metrics");
                return StatusCode(500, new { success = false, message = "Error fetching revenue" });
            }
        }

        [HttpGet("shops")]
        public async Task<IActionResult> GetShopMetrics()
        {
            try
            {
                var metrics = await _dashboardService.GetShopMetricsAsync();
                var dto = MapShopMetricsToDto(metrics);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shop metrics");
                return StatusCode(500, new { success = false, message = "Error fetching shop metrics" });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUserMetrics()
        {
            try
            {
                var metrics = await _dashboardService.GetUserMetricsAsync();
                var dto = MapUserMetricsToDto(metrics);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user metrics");
                return StatusCode(500, new { success = false, message = "Error fetching user metrics" });
            }
        }

        [HttpGet("banners")]
        public async Task<IActionResult> GetBannerMetrics()
        {
            try
            {
                var metrics = await _dashboardService.GetBannerMetricsAsync();
                var dto = MapBannerMetricsToDto(metrics);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching banner metrics");
                return StatusCode(500, new { success = false, message = "Error fetching banner metrics" });
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetSystemHealth()
        {
            try
            {
                var health = await _dashboardService.GetSystemHealthAsync();
                var dto = MapSystemHealthToDto(health);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching system health");
                return StatusCode(500, new { success = false, message = "Error fetching health" });
            }
        }

        [HttpGet("trends")]
        public async Task<IActionResult> GetTrends([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                    return BadRequest(new { success = false, message = "Days must be between 1 and 365" });

                var trends = await _dashboardService.GetTrendsAsync(days);
                var dtos = trends.Select(MapTrendToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching trends");
                return StatusCode(500, new { success = false, message = "Error fetching trends" });
            }
        }

        [HttpGet("top-shops")]
        public async Task<IActionResult> GetTopShops([FromQuery] int limit = 10)
        {
            try
            {
                if (limit < 1 || limit > 100)
                    return BadRequest(new { success = false, message = "Limit must be between 1 and 100" });

                var shops = await _dashboardService.GetTopShopsByRevenueAsync(limit);
                var dtos = shops.Select(MapTopShopToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top shops");
                return StatusCode(500, new { success = false, message = "Error fetching shops" });
            }
        }

        [HttpGet("subscription-distribution")]
        public async Task<IActionResult> GetSubscriptionDistribution()
        {
            try
            {
                var distribution = await _dashboardService.GetSubscriptionDistributionAsync();
                var dtos = distribution.Select(MapDistributionToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscription distribution");
                return StatusCode(500, new { success = false, message = "Error fetching distribution" });
            }
        }

        [HttpGet("payment-health")]
        public async Task<IActionResult> GetPaymentHealth()
        {
            try
            {
                var health = await _dashboardService.GetPaymentHealthAsync();
                var dto = MapPaymentHealthToDto(health);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment health");
                return StatusCode(500, new { success = false, message = "Error fetching payment health" });
            }
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            try
            {
                var alerts = await _dashboardRepository.GetActiveAlertsAsync();
                var dtos = alerts.Select(MapAlertToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alerts");
                return StatusCode(500, new { success = false, message = "Error fetching alerts" });
            }
        }

        [HttpPost("alerts/{alertId}/resolve")]
        public async Task<IActionResult> ResolveAlert(Guid alertId)
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
                _logger.LogError(ex, "Error resolving alert");
                return StatusCode(500, new { success = false, message = "Error resolving alert" });
            }
        }

        #region Mapping Helpers

        private DashboardSummaryDto MapSummaryToDto(DashboardSummary summary)
        {
            return new DashboardSummaryDto
            {
                GeneratedAt = summary.GeneratedAt,
                SubscriptionMetrics = MapSubscriptionMetricsToDto(summary.SubscriptionMetrics),
                RevenueMetrics = MapRevenueMetricsToDto(summary.RevenueMetrics),
                ShopMetrics = MapShopMetricsToDto(summary.ShopMetrics),
                UserMetrics = MapUserMetricsToDto(summary.UserMetrics),
                BannerMetrics = MapBannerMetricsToDto(summary.BannerMetrics),
                SystemHealth = MapSystemHealthToDto(summary.SystemHealth)
            };
        }

        private SubscriptionMetricsDto MapSubscriptionMetricsToDto(SubscriptionMetrics metrics)
        {
            return new SubscriptionMetricsDto
            {
                TotalSubscriptions = metrics.TotalSubscriptions,
                ActiveSubscriptions = metrics.ActiveSubscriptions,
                TrialSubscriptions = metrics.TrialSubscriptions,
                ExpiringInDays = metrics.ExpiringInDays,
                PaymentFailedCount = metrics.PaymentFailedCount,
                SuspendedCount = metrics.SuspendedCount,
                CancelledThisMonth = metrics.CancelledThisMonth,
                AverageSubscriptionValue = metrics.AverageSubscriptionValue
            };
        }

        private RevenueMetricsDto MapRevenueMetricsToDto(RevenueMetrics metrics)
        {
            return new RevenueMetricsDto
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

        private ShopMetricsDto MapShopMetricsToDto(ShopMetrics metrics)
        {
            return new ShopMetricsDto
            {
                TotalShops = metrics.TotalShops,
                ActiveShops = metrics.ActiveShops,
                InactiveShops = metrics.InactiveShops,
                ShopsWithActiveSubscription = metrics.ShopsWithActiveSubscription,
                TopLevelShops = metrics.TopLevelShops,
                ChildShopsCount = metrics.ChildShopsCount,
                AverageShopsPerUser = metrics.AverageShopsPerUser
            };
        }

        private UserMetricsDto MapUserMetricsToDto(UserMetrics metrics)
        {
            return new UserMetricsDto
            {
                TotalUsers = metrics.TotalUsers,
                ActiveUsers = metrics.ActiveUsers,
                InactiveUsers = metrics.InactiveUsers,
                AdminCount = metrics.AdminCount,
                ShopOwnerCount = metrics.ShopOwnerCount,
                SalesExecutiveCount = metrics.SalesExecutiveCount,
                UsersWithFailedLogin = metrics.UsersWithFailedLogin,
                LockedOutUsers = metrics.LockedOutUsers
            };
        }

        private BannerMetricsDto MapBannerMetricsToDto(BannerMetrics metrics)
        {
            return new BannerMetricsDto
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

        private SystemHealthMetricsDto MapSystemHealthToDto(SystemHealthMetrics metrics)
        {
            return new SystemHealthMetricsDto
            {
                IsDatabaseHealthy = metrics.IsDatabaseHealthy,
                PendingMigrations = metrics.PendingMigrations,
                DatabaseSizeBytes = metrics.DatabaseSizeBytes,
                ErrorLogsLastHour = metrics.ErrorLogsLastHour,
                WarningLogsLastHour = metrics.WarningLogsLastHour,
                LastHealthCheckTime = metrics.LastHealthCheckTime,
                AverageResponseTimeMs = metrics.AverageResponseTimeMs,
                ActiveConnections = metrics.ActiveConnections
            };
        }

        private DashboardTrendDto MapTrendToDto(DashboardTrend trend)
        {
            return new DashboardTrendDto
            {
                MetricName = trend.MetricName,
                CurrentValue = trend.CurrentValue,
                PreviousValue = trend.PreviousValue,
                PercentageChange = trend.PercentageChange,
                Trend = trend.Trend,
                MeasuredAt = trend.MeasuredAt
            };
        }

        private TopShopByRevenueDto MapTopShopToDto(TopShopByRevenue shop)
        {
            return new TopShopByRevenueDto
            {
                ShopId = shop.ShopId,
                ShopName = shop.ShopName,
                MonthlyRevenue = shop.MonthlyRevenue,
                SubscriptionCount = shop.SubscriptionCount,
                AverageSubscriptionValue = shop.AverageSubscriptionValue
            };
        }

        private SubscriptionPlanDistributionDto MapDistributionToDto(SubscriptionPlanDistribution dist)
        {
            return new SubscriptionPlanDistributionDto
            {
                PlanName = dist.PlanName,
                Count = dist.Count,
                Percentage = dist.Percentage,
                TotalRevenue = dist.TotalRevenue
            };
        }

        private PaymentHealthMetricsDto MapPaymentHealthToDto(PaymentHealthMetrics health)
        {
            return new PaymentHealthMetricsDto
            {
                SuccessfulPayments = health.SuccessfulPayments,
                FailedPayments = health.FailedPayments,
                RefundedPayments = health.RefundedPayments,
                SuccessRate = health.SuccessRate,
                AveragePaymentAmount = health.AveragePaymentAmount,
                LastPaymentTime = health.LastPaymentTime,
                PaymentsInGracePeriod = health.PaymentsInGracePeriod
            };
        }

        private AdminDashboardAlertDto MapAlertToDto(Domain.Entities.AdminDashboardAlert alert)
        {
            return new AdminDashboardAlertDto
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

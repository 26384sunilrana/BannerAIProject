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
    [Route("api/advertisements")]
    public class AdvertisementController : ControllerBase
    {
        private readonly IAdvertisementService _adService;
        private readonly IAdvertisementRepository _repository;
        private readonly ILogger<AdvertisementController> _logger;

        public AdvertisementController(
            IAdvertisementService adService,
            IAdvertisementRepository repository,
            ILogger<AdvertisementController> logger)
        {
            _adService = adService;
            _repository = repository;
            _logger = logger;
        }

        [HttpPost("shop/{shopId}")]
        public async Task<IActionResult> CreateAd(Guid shopId, [FromBody] CreateAdvertisementDto dto)
        {
            var userId = User.GetUserId();
            try
            {
                if (!Enum.TryParse<AdType>(dto.Type, out var adType))
                    return BadRequest(new { success = false, message = "Invalid advertisement type" });

                var ad = await _adService.CreateAdAsync(shopId, dto.Title, dto.Description, adType, userId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating advertisement");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{adId}")]
        public async Task<IActionResult> GetAd(Guid adId)
        {
            try
            {
                var ad = await _adService.GetAdAsync(adId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("shop/{shopId}")]
        public async Task<IActionResult> GetShopAds(Guid shopId)
        {
            try
            {
                var ads = await _adService.GetShopAdsAsync(shopId);
                var dtos = ads.Select(MapToListDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shop advertisements");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveAds()
        {
            try
            {
                var ads = await _adService.GetActiveAdsAsync();
                var dtos = ads.Select(MapToListDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active advertisements");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{adId}")]
        public async Task<IActionResult> UpdateAd(Guid adId, [FromBody] UpdateAdvertisementDto dto)
        {
            var userId = User.GetUserId();
            try
            {
                var ad = await _adService.UpdateAdAsync(adId, dto.Title, dto.Description, userId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{adId}/target")]
        public async Task<IActionResult> SetTarget(Guid adId, [FromBody] SetAdTargetDto dto)
        {
            var userId = User.GetUserId();
            try
            {
                var target = MapFromDto(dto.Target);
                var ad = await _adService.SetAdTargetAsync(adId, target, userId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting advertisement target {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{adId}/budget")]
        public async Task<IActionResult> SetBudget(Guid adId, [FromBody] SetBudgetDto dto)
        {
            var userId = User.GetUserId();
            try
            {
                var ad = await _adService.SetBudgetAsync(adId, dto.BudgetLimit, dto.DailyBudgetLimit, userId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting budget for advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{adId}/dates")]
        public async Task<IActionResult> SetDates(Guid adId, [FromBody] SetAdDatesDto dto)
        {
            var userId = User.GetUserId();
            try
            {
                var ad = await _adService.SetDatesAsync(adId, dto.StartDate, dto.EndDate, userId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting dates for advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{adId}/activate")]
        public async Task<IActionResult> ActivateAd(Guid adId)
        {
            var userId = User.GetUserId();
            try
            {
                var ad = await _adService.ActivateAdAsync(adId, userId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{adId}/pause")]
        public async Task<IActionResult> PauseAd(Guid adId)
        {
            var userId = User.GetUserId();
            try
            {
                var ad = await _adService.PauseAdAsync(adId, userId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{adId}/complete")]
        public async Task<IActionResult> CompleteAd(Guid adId)
        {
            var userId = User.GetUserId();
            try
            {
                var ad = await _adService.CompleteAdAsync(adId, userId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{adId}/cancel")]
        public async Task<IActionResult> CancelAd(Guid adId)
        {
            var userId = User.GetUserId();
            try
            {
                var ad = await _adService.CancelAdAsync(adId, userId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{adId}")]
        public async Task<IActionResult> DeleteAd(Guid adId)
        {
            var userId = User.GetUserId();
            try
            {
                var result = await _adService.DeleteAdAsync(adId, userId);
                if (!result)
                    return BadRequest(new { success = false, message = "Can only delete draft advertisements" });

                return Ok(new { success = true, message = "Advertisement deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("expiring/{daysThreshold}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetExpiringAds(int daysThreshold = 7)
        {
            try
            {
                var ads = await _adService.GetExpiringAdsAsync(daysThreshold);
                var dtos = ads.Select(MapToListDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expiring advertisements");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{adId}/metrics")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMetrics(Guid adId, [FromBody] UpdateAdMetricsDto dto)
        {
            try
            {
                await _adService.UpdateMetricsAsync(adId, dto.Impressions, dto.Clicks, dto.Conversions, dto.Spend);
                var ad = await _adService.GetAdAsync(adId);
                return Ok(new { success = true, data = MapToDto(ad) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metrics for advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{adId}/roi")]
        public async Task<IActionResult> GetROI(Guid adId)
        {
            try
            {
                var roi = await _adService.CalculateROIAsync(adId);
                return Ok(new { success = true, data = new { roi } });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating ROI for advertisement {AdId}", adId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("shop/{shopId}/performance")]
        public async Task<IActionResult> GetShopPerformance(Guid shopId)
        {
            try
            {
                var ads = await _adService.GetShopAdsAsync(shopId);
                var performance = ads.Select(a => new AdPerformanceDto
                {
                    AdId = a.Id,
                    Title = a.Title,
                    Metrics = MapMetricsToDto(a.Metrics),
                    BudgetRemaining = Math.Max(0, a.BudgetLimit - a.Metrics.TotalSpent),
                    BudgetUtilization = a.BudgetLimit > 0 ? (a.Metrics.TotalSpent / a.BudgetLimit) * 100 : 0,
                    HealthStatus = GetHealthStatus(a)
                }).ToList();

                return Ok(new { success = true, data = performance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shop performance");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #region Mapping Helpers

        private AdvertisementDto MapToDto(Advertisement ad)
        {
            return new AdvertisementDto
            {
                Id = ad.Id,
                ShopId = ad.ShopId,
                CreatedByUserId = ad.CreatedByUserId,
                Title = ad.Title,
                Description = ad.Description,
                Type = ad.Type.ToString(),
                Status = ad.Status.ToString(),
                Content = ad.Content,
                ImageUrl = ad.ImageUrl,
                ClickUrl = ad.ClickUrl,
                StartDate = ad.StartDate,
                EndDate = ad.EndDate,
                BudgetLimit = ad.BudgetLimit,
                DailyBudgetLimit = ad.DailyBudgetLimit,
                Target = MapTargetToDto(ad.Target),
                Metrics = MapMetricsToDto(ad.Metrics),
                Priority = ad.Priority,
                IsActive = ad.IsActive,
                CreatedAt = ad.CreatedAt,
                UpdatedAt = ad.UpdatedAt,
                PublishedAt = ad.PublishedAt,
                PausedAt = ad.PausedAt,
                CompletedAt = ad.CompletedAt,
                IsBudgetExceeded = ad.IsBudgetExceeded(),
                IsExpired = ad.IsExpired()
            };
        }

        private AdvertisementListDto MapToListDto(Advertisement ad)
        {
            return new AdvertisementListDto
            {
                Id = ad.Id,
                Title = ad.Title,
                Type = ad.Type.ToString(),
                Status = ad.Status.ToString(),
                StartDate = ad.StartDate,
                EndDate = ad.EndDate,
                BudgetLimit = ad.BudgetLimit,
                TotalSpent = ad.Metrics.TotalSpent,
                Impressions = ad.Metrics.Impressions,
                Clicks = ad.Metrics.Clicks,
                CreatedAt = ad.CreatedAt
            };
        }

        private AdTargetDto MapTargetToDto(AdTarget target)
        {
            return new AdTargetDto
            {
                AudienceSegments = target.AudienceSegments,
                DemographicFilters = target.DemographicFilters,
                TargetLocations = target.TargetLocations,
                ExcludedLocations = target.ExcludedLocations,
                DeviceTypes = target.DeviceTypes,
                OperatingSystems = target.OperatingSystems,
                TargetAllUsers = target.TargetAllUsers,
                MinimumAge = target.MinimumAge,
                MaximumAge = target.MaximumAge,
                InterestCategories = target.InterestCategories,
                CustomAttributes = target.CustomAttributes
            };
        }

        private AdMetricsDto MapMetricsToDto(AdMetrics metrics)
        {
            return new AdMetricsDto
            {
                Impressions = metrics.Impressions,
                Clicks = metrics.Clicks,
                Conversions = metrics.Conversions,
                TotalSpent = metrics.TotalSpent,
                EstimatedRevenue = metrics.EstimatedRevenue,
                LastUpdatedAt = metrics.LastUpdatedAt,
                Views = metrics.Views,
                Engagements = metrics.Engagements,
                ClickThroughRate = (decimal)metrics.GetClickThroughRate(),
                ConversionRate = (decimal)metrics.GetConversionRate(),
                CostPerClick = metrics.GetCostPerClick(),
                CostPerConversion = metrics.GetCostPerConversion(),
                ROI = (decimal)metrics.GetROI(),
                EngagementRate = (decimal)metrics.GetEngagementRate()
            };
        }

        private AdTarget MapFromDto(AdTargetDto dto)
        {
            return new AdTarget
            {
                AudienceSegments = dto.AudienceSegments,
                DemographicFilters = dto.DemographicFilters,
                TargetLocations = dto.TargetLocations,
                ExcludedLocations = dto.ExcludedLocations,
                DeviceTypes = dto.DeviceTypes,
                OperatingSystems = dto.OperatingSystems,
                TargetAllUsers = dto.TargetAllUsers,
                MinimumAge = dto.MinimumAge,
                MaximumAge = dto.MaximumAge,
                InterestCategories = dto.InterestCategories,
                CustomAttributes = dto.CustomAttributes
            };
        }

        private string GetHealthStatus(Advertisement ad)
        {
            if (ad.Status == AdStatus.Draft)
                return "Draft";

            if (ad.IsBudgetExceeded())
                return "Critical";

            var utilization = ad.BudgetLimit > 0 ? (ad.Metrics.TotalSpent / ad.BudgetLimit) * 100 : 0;
            if (utilization >= 80)
                return "Warning";

            return "Healthy";
        }

        #endregion
    }
}

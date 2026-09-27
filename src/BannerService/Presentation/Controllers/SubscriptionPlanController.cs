namespace BannerService.Presentation.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Domain.Entities;
    using Domain.Interfaces;

    [ApiController]
    [Route("api/admin/subscription-plans")]
    public class SubscriptionPlanController : ControllerBase
    {
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ILogger<SubscriptionPlanController> _logger;

        public SubscriptionPlanController(
            ISubscriptionPlanRepository planRepository,
            ISubscriptionRepository subscriptionRepository,
            ILogger<SubscriptionPlanController> logger)
        {
            _planRepository = planRepository;
            _subscriptionRepository = subscriptionRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPlans([FromQuery] bool includeInactive = false)
        {
            try
            {
                var plans = await _planRepository.GetAllAsync();
                if (!includeInactive)
                    plans = plans.Where(p => p.IsActive).ToList();

                var dtos = plans.Select(MapToDto).OrderBy(p => p.DisplayOrder).ToList();
                return Ok(new { success = true, count = dtos.Count, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription plans");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{planId}")]
        public async Task<IActionResult> GetPlanById(Guid planId)
        {
            try
            {
                var plan = await _planRepository.GetByIdAsync(planId);
                if (plan == null)
                    return NotFound(new { success = false, message = "Plan not found" });

                var activeSubscriptions = await _subscriptionRepository.GetByPlanIdAsync(planId);
                var activeSubs = activeSubscriptions.Where(s => s.Status == SubscriptionStatus.Active).Count();

                return Ok(new
                {
                    success = true,
                    data = MapToDto(plan),
                    activeSubscriptions = activeSubs,
                    metadata = new
                    {
                        canDelete = activeSubs == 0,
                        deleteWarning = activeSubs > 0 ? $"{activeSubs} active subscriptions using this plan" : null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plan");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequest(new { success = false, message = "Plan name is required" });

                if (dto.MonthlyPrice < 0 || dto.AnnualPrice < 0)
                    return BadRequest(new { success = false, message = "Prices cannot be negative" });

                var existingPlan = (await _planRepository.GetAllAsync())
                    .FirstOrDefault(p => p.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase));

                if (existingPlan != null)
                    return BadRequest(new { success = false, message = "Plan name already exists" });

                var plan = new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Description = dto.Description ?? string.Empty,
                    MonthlyPrice = dto.MonthlyPrice,
                    AnnualPrice = dto.AnnualPrice,
                    Features = dto.Features ?? new Domain.Entities.SubscriptionFeatures(),
                    IsActive = dto.IsActive,
                    DisplayOrder = dto.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                await _planRepository.CreateAsync(plan);
                _logger.LogInformation("Subscription plan created: {PlanName} (${Price}/mo)", plan.Name, plan.MonthlyPrice);

                return Ok(new { success = true, data = MapToDto(plan), message = "Plan created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating plan");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{planId}")]
        public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] UpdatePlanDto dto)
        {
            try
            {
                var plan = await _planRepository.GetByIdAsync(planId);
                if (plan == null)
                    return NotFound(new { success = false, message = "Plan not found" });

                var priceChanged = plan.MonthlyPrice != dto.MonthlyPrice || plan.AnnualPrice != dto.AnnualPrice;

                if (priceChanged)
                {
                    // Get all active subscriptions on this plan
                    var activeSubscriptions = await _subscriptionRepository.GetByPlanIdAsync(planId);
                    var activeSubs = activeSubscriptions.Where(s => s.Status == SubscriptionStatus.Active).ToList();

                    if (activeSubs.Count > 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Cannot modify pricing with active subscriptions",
                            details = new
                            {
                                reason = "Existing subscriptions are grandfathered at current pricing",
                                activeSubscriptionCount = activeSubs.Count,
                                action = "Deactivate plan and create new plan with updated pricing, or wait until all subscriptions expire"
                            }
                        });
                    }
                }

                // Update allowed fields
                plan.Description = dto.Description ?? plan.Description;
                plan.MonthlyPrice = dto.MonthlyPrice;
                plan.AnnualPrice = dto.AnnualPrice;
                plan.DisplayOrder = dto.DisplayOrder;
                plan.IsActive = dto.IsActive;

                if (dto.Features != null)
                {
                    plan.Features = dto.Features;
                }

                await _planRepository.UpdateAsync(plan);

                if (priceChanged)
                {
                    _logger.LogWarning("Subscription plan pricing updated: {PlanName} (${OldPrice} → ${NewPrice}/mo)",
                        plan.Name, plan.MonthlyPrice, dto.MonthlyPrice);
                }

                return Ok(new { success = true, data = MapToDto(plan), message = "Plan updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating plan");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{planId}")]
        public async Task<IActionResult> DeletePlan(Guid planId)
        {
            try
            {
                var plan = await _planRepository.GetByIdAsync(planId);
                if (plan == null)
                    return NotFound(new { success = false, message = "Plan not found" });

                // Check for active subscriptions
                var subscriptions = await _subscriptionRepository.GetByPlanIdAsync(planId);
                var activeSubs = subscriptions.Where(s => s.Status == SubscriptionStatus.Active).ToList();

                if (activeSubs.Count > 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete plan with active subscriptions",
                        activeSubscriptionCount = activeSubs.Count,
                        recommendedAction = "Deactivate plan instead, or mark as inactive"
                    });
                }

                // Mark as inactive instead of deleting
                plan.IsActive = false;
                await _planRepository.UpdateAsync(plan);

                _logger.LogInformation("Subscription plan deactivated: {PlanName}", plan.Name);

                return Ok(new { success = true, message = "Plan deactivated successfully (soft delete)" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting plan");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{planId}/subscriptions")]
        public async Task<IActionResult> GetPlanSubscriptions(Guid planId, [FromQuery] string status = "all")
        {
            try
            {
                var plan = await _planRepository.GetByIdAsync(planId);
                if (plan == null)
                    return NotFound(new { success = false, message = "Plan not found" });

                var subscriptions = await _subscriptionRepository.GetByPlanIdAsync(planId);

                if (status != "all" && Enum.TryParse<SubscriptionStatus>(status, out var enumStatus))
                {
                    subscriptions = subscriptions.Where(s => s.Status == enumStatus).ToList();
                }

                return Ok(new
                {
                    success = true,
                    planName = plan.Name,
                    totalSubscriptions = subscriptions.Count,
                    byStatus = subscriptions.GroupBy(s => s.Status)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    subscriptions = subscriptions.Select(s => new
                    {
                        s.Id,
                        s.Status,
                        s.StartDate,
                        s.RenewalDate,
                        s.CurrentPrice
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plan subscriptions");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{planId}/deactivate")]
        public async Task<IActionResult> DeactivatePlan(Guid planId)
        {
            try
            {
                var plan = await _planRepository.GetByIdAsync(planId);
                if (plan == null)
                    return NotFound(new { success = false, message = "Plan not found" });

                plan.IsActive = false;
                await _planRepository.UpdateAsync(plan);

                var subscriptions = await _subscriptionRepository.GetByPlanIdAsync(planId);
                var activeSubs = subscriptions.Count(s => s.Status == SubscriptionStatus.Active);

                return Ok(new
                {
                    success = true,
                    message = "Plan deactivated",
                    details = new
                    {
                        existingActiveSubscriptions = activeSubs,
                        note = "Existing subscriptions continue at current pricing (grandfathered)",
                        newSubscriptionsAllowed = false
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating plan");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #region DTOs

        public class CreatePlanDto
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal MonthlyPrice { get; set; }
            public decimal AnnualPrice { get; set; }
            public Domain.Entities.SubscriptionFeatures? Features { get; set; }
            public bool IsActive { get; set; } = true;
            public int DisplayOrder { get; set; }
        }

        public class UpdatePlanDto
        {
            public string? Description { get; set; }
            public decimal MonthlyPrice { get; set; }
            public decimal AnnualPrice { get; set; }
            public Domain.Entities.SubscriptionFeatures? Features { get; set; }
            public bool IsActive { get; set; }
            public int DisplayOrder { get; set; }
        }

        private SubscriptionPlanDto MapToDto(SubscriptionPlan plan)
        {
            return new SubscriptionPlanDto
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                MonthlyPrice = plan.MonthlyPrice,
                AnnualPrice = plan.AnnualPrice,
                Features = plan.Features,
                IsActive = plan.IsActive,
                DisplayOrder = plan.DisplayOrder,
                CreatedAt = plan.CreatedAt
            };
        }

        public class SubscriptionPlanDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal MonthlyPrice { get; set; }
            public decimal AnnualPrice { get; set; }
            public Domain.Entities.SubscriptionFeatures Features { get; set; } = new();
            public bool IsActive { get; set; }
            public int DisplayOrder { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        #endregion
    }
}

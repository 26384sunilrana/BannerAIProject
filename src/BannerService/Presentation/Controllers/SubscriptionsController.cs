namespace BannerService.Presentation.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;
    using Application.Services;
    using Application.DTOs;
    using Domain.Entities;

    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/[controller]")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly RenewalService _renewalService;
        private readonly BillingService _billingService;
        private readonly ILogger<SubscriptionsController> _logger;

        public SubscriptionsController(
            SubscriptionService subscriptionService,
            RenewalService renewalService,
            BillingService billingService,
            ILogger<SubscriptionsController> logger)
        {
            _subscriptionService = subscriptionService;
            _renewalService = renewalService;
            _billingService = billingService;
            _logger = logger;
        }

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            try
            {
                var plans = await _subscriptionService.GetAllPlansAsync();
                var planDtos = plans.Select(p => new SubscriptionPlanDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    MonthlyPrice = p.MonthlyPrice,
                    AnnualPrice = p.AnnualPrice,
                    IsActive = p.IsActive
                }).ToList();

                return Ok(new { success = true, data = planDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscription plans");
                return StatusCode(500, new { success = false, message = "Error fetching plans" });
            }
        }

        [HttpGet("plans/{planId}")]
        public async Task<IActionResult> GetPlan(Guid planId)
        {
            try
            {
                var plan = await _subscriptionService.GetPlanByIdAsync(planId);
                if (plan == null)
                    return NotFound(new { success = false, message = "Plan not found" });

                var planDto = new SubscriptionPlanDto
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    Description = plan.Description,
                    MonthlyPrice = plan.MonthlyPrice,
                    AnnualPrice = plan.AnnualPrice,
                    IsActive = plan.IsActive
                };

                return Ok(new { success = true, data = planDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching plan {planId}", planId);
                return StatusCode(500, new { success = false, message = "Error fetching plan" });
            }
        }

        [HttpGet("{shopId}")]
        public async Task<IActionResult> GetShopSubscription(Guid shopId)
        {
            try
            {
                var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(shopId);
                if (subscription == null)
                    return NotFound(new { success = false, message = "No active subscription found" });

                var subscriptionDto = MapToDto(subscription);
                return Ok(new { success = true, data = subscriptionDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscription for shop {shopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching subscription" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                var result = await _subscriptionService.CreateSubscriptionAsync(
                    request.ShopId,
                    request.PlanId,
                    request.BillingPeriod,
                    request.PaymentMethodId,
                    request.TrialDays,
                    Guid.Parse(userId));

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                var subscriptionDto = MapToDto(result.subscription!);
                return CreatedAtAction(nameof(GetShopSubscription),
                    new { shopId = result.subscription!.ShopId },
                    new { success = true, data = subscriptionDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                return StatusCode(500, new { success = false, message = "Error creating subscription" });
            }
        }

        [HttpPost("{subscriptionId}/upgrade")]
        public async Task<IActionResult> UpgradePlan(Guid subscriptionId, [FromBody] ChangePlanDto request)
        {
            try
            {
                var result = await _subscriptionService.UpgradePlanAsync(subscriptionId, request.NewPlanId);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                var subscription = await _subscriptionService.GetSubscriptionByIdAsync(subscriptionId);
                var subscriptionDto = MapToDto(subscription!);

                return Ok(new { success = true, message = result.message, data = subscriptionDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upgrading subscription {subscriptionId}", subscriptionId);
                return StatusCode(500, new { success = false, message = "Error upgrading plan" });
            }
        }

        [HttpPost("{subscriptionId}/downgrade")]
        public async Task<IActionResult> DowngradePlan(Guid subscriptionId, [FromBody] ChangePlanDto request)
        {
            try
            {
                var result = await _subscriptionService.DowngradePlanAsync(subscriptionId, request.NewPlanId);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                var subscription = await _subscriptionService.GetSubscriptionByIdAsync(subscriptionId);
                var subscriptionDto = MapToDto(subscription!);

                return Ok(new { success = true, message = result.message, data = subscriptionDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downgrading subscription {subscriptionId}", subscriptionId);
                return StatusCode(500, new { success = false, message = "Error downgrading plan" });
            }
        }

        [HttpPut("{subscriptionId}/auto-renew")]
        public async Task<IActionResult> SetAutoRenew(Guid subscriptionId, [FromBody] SetAutoRenewDto request)
        {
            try
            {
                var result = await _subscriptionService.SetAutoRenewAsync(subscriptionId, request.AutoRenew);

                if (!result.success)
                    return NotFound(new { success = false, message = result.message });

                return Ok(new { success = true, message = result.message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating auto renew for subscription {subscriptionId}", subscriptionId);
                return StatusCode(500, new { success = false, message = "Error updating auto renewal" });
            }
        }

        [HttpPost("{subscriptionId}/change-billing")]
        public async Task<IActionResult> ChangeBillingPeriod(Guid subscriptionId, [FromBody] ChangeBillingPeriodDto request)
        {
            try
            {
                var result = await _subscriptionService.ChangeBillingPeriodAsync(subscriptionId, request.NewPeriod);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                var subscription = await _subscriptionService.GetSubscriptionByIdAsync(subscriptionId);
                var subscriptionDto = MapToDto(subscription!);

                return Ok(new { success = true, message = result.message, data = subscriptionDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing billing period {subscriptionId}", subscriptionId);
                return StatusCode(500, new { success = false, message = "Error changing billing period" });
            }
        }

        [HttpPost("{subscriptionId}/cancel")]
        public async Task<IActionResult> CancelSubscription(Guid subscriptionId)
        {
            try
            {
                var result = await _subscriptionService.CancelSubscriptionAsync(subscriptionId);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                return Ok(new { success = true, message = result.message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription {subscriptionId}", subscriptionId);
                return StatusCode(500, new { success = false, message = "Error cancelling subscription" });
            }
        }

        [HttpPost("{subscriptionId}/renew")]
        public async Task<IActionResult> RenewSubscription(Guid subscriptionId)
        {
            try
            {
                var result = await _renewalService.ProcessRenewalAsync(subscriptionId);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                var subscription = await _subscriptionService.GetSubscriptionByIdAsync(subscriptionId);
                var subscriptionDto = MapToDto(subscription!);

                return Ok(new { success = true, message = result.message, data = subscriptionDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing subscription {subscriptionId}", subscriptionId);
                return StatusCode(500, new { success = false, message = "Error renewing subscription" });
            }
        }

        [HttpGet("renewals/pending")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingRenewals()
        {
            try
            {
                var renewals = await _renewalService.GetPendingRenewalsAsync();
                var renewalDtos = renewals.Select(MapToDto).ToList();

                return Ok(new { success = true, data = renewalDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending renewals");
                return StatusCode(500, new { success = false, message = "Error fetching pending renewals" });
            }
        }

        private SubscriptionDto MapToDto(Subscription subscription)
        {
            return new SubscriptionDto
            {
                Id = subscription.Id,
                ShopId = subscription.ShopId,
                PlanId = subscription.PlanId,
                PlanName = subscription.Plan?.Name ?? "Unknown",
                Status = subscription.Status,
                BillingPeriod = subscription.BillingPeriod,
                StartDate = subscription.StartDate,
                RenewalDate = subscription.RenewalDate,
                TrialEndDate = subscription.TrialEndDate,
                CancellationDate = subscription.CancellationDate,
                CurrentPrice = subscription.CurrentPrice,
                PaymentFailureCount = subscription.PaymentFailureCount,
                LastPaymentAttempt = subscription.LastPaymentAttempt,
                AutoRenew = subscription.AutoRenew,
                PendingPlanId = subscription.PendingPlanId,
                PendingPlanEffectiveAt = subscription.PendingPlanEffectiveAt,
                CreatedAt = subscription.CreatedAt,
                UpdatedAt = subscription.UpdatedAt
            };
        }
    }
}

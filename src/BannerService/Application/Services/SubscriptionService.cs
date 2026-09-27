namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;

    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IInvoiceRepository _invoiceRepository;

        public SubscriptionService(
            ISubscriptionPlanRepository planRepository,
            ISubscriptionRepository subscriptionRepository,
            IInvoiceRepository invoiceRepository)
        {
            _planRepository = planRepository;
            _subscriptionRepository = subscriptionRepository;
            _invoiceRepository = invoiceRepository;
        }

        #region Subscription Plans
        public async Task<SubscriptionPlan?> GetPlanByIdAsync(Guid planId)
        {
            return await _planRepository.GetByIdAsync(planId);
        }

        public async Task<List<SubscriptionPlan>> GetAllPlansAsync()
        {
            return await _planRepository.GetActiveAsync();
        }

        public async Task<SubscriptionPlan?> GetPlanByNameAsync(string name)
        {
            return await _planRepository.GetByNameAsync(name);
        }
        #endregion

        #region Subscription Queries
        public async Task<Subscription?> GetSubscriptionByIdAsync(Guid subscriptionId)
        {
            return await _subscriptionRepository.GetByIdAsync(subscriptionId);
        }

        public async Task<Subscription?> GetCurrentSubscriptionAsync(Guid shopId)
        {
            return await _subscriptionRepository.GetByShopIdAsync(shopId);
        }

        public async Task<List<Subscription>> GetShopSubscriptionsAsync(Guid shopId)
        {
            return await _subscriptionRepository.GetActiveByShopIdAsync(shopId);
        }

        public async Task<List<Subscription>> GetExpiringSubscriptionsAsync()
        {
            return await _subscriptionRepository.GetExpiringTodayAsync();
        }

        public async Task<List<Subscription>> GetRenewingSubscriptionsAsync()
        {
            return await _subscriptionRepository.GetRenewingSoonAsync(7);
        }
        #endregion

        #region Subscription Operations
        public async Task<(bool success, string message, Subscription? subscription)> CreateSubscriptionAsync(
            Guid shopId,
            Guid planId,
            BillingPeriod billingPeriod,
            string? paymentMethodId,
            int? trialDays,
            Guid createdByUserId)
        {
            try
            {
                // Validate plan exists
                var plan = await _planRepository.GetByIdAsync(planId);
                if (plan == null)
                    return (false, "Subscription plan not found", null);

                // Check if shop already has active subscription
                var existing = await _subscriptionRepository.GetByShopIdAsync(shopId);
                if (existing != null && existing.Status == SubscriptionStatus.Active)
                    return (false, "Shop already has an active subscription", null);

                var subscription = new Subscription
                {
                    ShopId = shopId,
                    PlanId = planId,
                    Status = trialDays.HasValue && trialDays > 0 ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
                    BillingPeriod = billingPeriod,
                    StartDate = DateTime.UtcNow,
                    RenewalDate = CalculateRenewalDate(DateTime.UtcNow, billingPeriod),
                    TrialEndDate = trialDays.HasValue ? DateTime.UtcNow.AddDays(trialDays.Value) : null,
                    CurrentPrice = GetPrice(plan, billingPeriod),
                    PaymentMethodId = paymentMethodId,
                };

                var created = await _subscriptionRepository.CreateAsync(subscription);

                // Create initial invoice if not trial
                if (!trialDays.HasValue || trialDays == 0)
                {
                    await CreateInvoiceForSubscriptionAsync(created, plan);
                }

                return (true, "Subscription created successfully", created);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating subscription: {ex.Message}", null);
            }
        }

        public async Task<(bool success, string message)> UpgradePlanAsync(
            Guid subscriptionId,
            Guid newPlanId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                var currentPlan = subscription.Plan;
                var newPlan = await _planRepository.GetByIdAsync(newPlanId);
                if (newPlan == null)
                    return (false, "New plan not found");

                // Calculate proration
                var proratedAmount = await CalculateProratedAmountAsync(subscriptionId, newPlanId);

                // Store previous plan info
                subscription.PreviousPlanId = currentPlan?.Id;
                subscription.PlanChangedAt = DateTime.UtcNow;

                // Update to new plan
                subscription.PlanId = newPlanId;
                subscription.CurrentPrice = GetPrice(newPlan, subscription.BillingPeriod);

                await _subscriptionRepository.UpdateAsync(subscription);

                // Create adjustment invoice for prorated amount if needed
                if (proratedAmount > 0)
                {
                    await CreateAdjustmentInvoiceAsync(subscription, proratedAmount, "Upgrade proration");
                }

                return (true, "Plan upgraded successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error upgrading plan: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DowngradePlanAsync(
            Guid subscriptionId,
            Guid newPlanId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                var currentPlan = subscription.Plan;
                var newPlan = await _planRepository.GetByIdAsync(newPlanId);
                if (newPlan == null)
                    return (false, "New plan not found");

                // Store previous plan info
                subscription.PreviousPlanId = currentPlan?.Id;
                subscription.PlanChangedAt = DateTime.UtcNow;

                // Update to new plan
                subscription.PlanId = newPlanId;
                subscription.CurrentPrice = GetPrice(newPlan, subscription.BillingPeriod);

                await _subscriptionRepository.UpdateAsync(subscription);

                // Downgrade typically doesn't charge immediately
                return (true, "Plan downgraded successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error downgrading plan: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ChangeBillingPeriodAsync(
            Guid subscriptionId,
            BillingPeriod newPeriod)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                var plan = subscription.Plan;
                if (plan == null)
                    return (false, "Subscription plan not found");

                // Calculate prorated adjustment
                var oldPrice = GetPrice(plan, subscription.BillingPeriod);
                var newPrice = GetPrice(plan, newPeriod);

                subscription.BillingPeriod = newPeriod;
                subscription.CurrentPrice = newPrice;
                subscription.RenewalDate = CalculateRenewalDate(DateTime.UtcNow, newPeriod);

                await _subscriptionRepository.UpdateAsync(subscription);

                // Create adjustment invoice if price difference exists
                if (newPrice != oldPrice)
                {
                    var difference = newPrice - oldPrice;
                    if (difference > 0)
                    {
                        await CreateAdjustmentInvoiceAsync(subscription, difference, "Billing period change");
                    }
                }

                return (true, "Billing period changed successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error changing billing period: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> CancelSubscriptionAsync(
            Guid subscriptionId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                subscription.Cancel();
                await _subscriptionRepository.UpdateAsync(subscription);

                return (true, "Subscription cancelled successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error cancelling subscription: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> RenewSubscriptionAsync(
            Guid subscriptionId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                var plan = subscription.Plan;
                if (plan == null)
                    return (false, "Subscription plan not found");

                subscription.Status = SubscriptionStatus.Active;
                subscription.RenewalDate = CalculateRenewalDate(DateTime.UtcNow, subscription.BillingPeriod);
                subscription.PaymentFailureCount = 0;
                subscription.LastPaymentAttempt = null;

                await _subscriptionRepository.UpdateAsync(subscription);

                // Create renewal invoice
                await CreateInvoiceForSubscriptionAsync(subscription, plan);

                return (true, "Subscription renewed successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error renewing subscription: {ex.Message}");
            }
        }
        #endregion

        #region Invoice Operations
        public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId)
        {
            return await _invoiceRepository.GetByIdAsync(invoiceId);
        }

        public async Task<List<Invoice>> GetShopInvoicesAsync(Guid shopId)
        {
            return await _invoiceRepository.GetByShopIdAsync(shopId);
        }

        public async Task<List<Invoice>> GetSubscriptionInvoicesAsync(Guid subscriptionId)
        {
            return await _invoiceRepository.GetBySubscriptionIdAsync(subscriptionId);
        }
        #endregion

        #region Renewal/Billing
        public async Task<(bool success, string message)> ProcessRenewalAsync(Guid subscriptionId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                var plan = subscription.Plan;
                if (plan == null)
                    return (false, "Subscription plan not found");

                // Update status and renewal date
                subscription.Status = SubscriptionStatus.RenewalPending;
                subscription.RenewalDate = CalculateRenewalDate(DateTime.UtcNow, subscription.BillingPeriod);

                await _subscriptionRepository.UpdateAsync(subscription);

                // Create renewal invoice
                await CreateInvoiceForSubscriptionAsync(subscription, plan);

                // Simulate payment processing
                subscription.Status = SubscriptionStatus.Active;
                subscription.PaymentFailureCount = 0;
                subscription.LastPaymentAttempt = DateTime.UtcNow;

                await _subscriptionRepository.UpdateAsync(subscription);

                return (true, "Renewal processed successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error processing renewal: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> RetryPaymentAsync(Guid invoiceId)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
                if (invoice == null)
                    return (false, "Invoice not found");

                var subscription = await _subscriptionRepository.GetByIdAsync(invoice.SubscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                // Simulate payment retry
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.UtcNow;

                await _invoiceRepository.UpdateAsync(invoice);

                // Reset payment failure count
                subscription.PaymentFailureCount = 0;
                subscription.Status = SubscriptionStatus.Active;
                subscription.LastPaymentAttempt = DateTime.UtcNow;

                await _subscriptionRepository.UpdateAsync(subscription);

                return (true, "Payment retry processed successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error retrying payment: {ex.Message}");
            }
        }

        public async Task<decimal> CalculateProratedAmountAsync(
            Guid subscriptionId,
            Guid newPlanId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return 0;

                var newPlan = await _planRepository.GetByIdAsync(newPlanId);
                if (newPlan == null)
                    return 0;

                var currentPrice = subscription.CurrentPrice;
                var newPrice = GetPrice(newPlan, subscription.BillingPeriod);

                // Calculate days remaining in current period
                var daysTotal = GetDaysInBillingPeriod(subscription.BillingPeriod);
                var daysRemaining = (subscription.RenewalDate.Date - DateTime.UtcNow.Date).Days;
                if (daysRemaining < 0)
                    daysRemaining = 0;

                // Prorated amount = (new price - current price) * (days remaining / days total)
                var proratedAmount = (newPrice - currentPrice) * (daysRemaining / (decimal)daysTotal);

                return Math.Max(0, proratedAmount);
            }
            catch
            {
                return 0;
            }
        }
        #endregion

        #region Helper Methods
        private DateTime CalculateRenewalDate(DateTime fromDate, BillingPeriod period)
        {
            return period switch
            {
                BillingPeriod.Monthly => fromDate.AddMonths(1),
                BillingPeriod.Annual => fromDate.AddYears(1),
                _ => fromDate.AddMonths(1)
            };
        }

        private decimal GetPrice(SubscriptionPlan plan, BillingPeriod period)
        {
            return period switch
            {
                BillingPeriod.Monthly => plan.MonthlyPrice,
                BillingPeriod.Annual => plan.AnnualPrice,
                _ => plan.MonthlyPrice
            };
        }

        private int GetDaysInBillingPeriod(BillingPeriod period)
        {
            return period switch
            {
                BillingPeriod.Monthly => 30,
                BillingPeriod.Annual => 365,
                _ => 30
            };
        }

        private async Task CreateInvoiceForSubscriptionAsync(Subscription subscription, SubscriptionPlan plan)
        {
            var invoiceNumber = await _invoiceRepository.GenerateInvoiceNumberAsync();
            var dueDate = DateTime.UtcNow.AddDays(30);

            var invoice = new Invoice
            {
                SubscriptionId = subscription.Id,
                ShopId = subscription.ShopId,
                Amount = subscription.CurrentPrice,
                Status = InvoiceStatus.Issued,
                InvoiceNumber = invoiceNumber,
                IssuedDate = DateTime.UtcNow,
                DueDate = dueDate,
                Description = $"{plan.Name} - {subscription.BillingPeriod} Subscription",
            };

            await _invoiceRepository.CreateAsync(invoice);
        }

        private async Task CreateAdjustmentInvoiceAsync(Subscription subscription, decimal amount, string description)
        {
            var invoiceNumber = await _invoiceRepository.GenerateInvoiceNumberAsync();
            var dueDate = DateTime.UtcNow.AddDays(30);

            var invoice = new Invoice
            {
                SubscriptionId = subscription.Id,
                ShopId = subscription.ShopId,
                Amount = amount,
                Status = InvoiceStatus.Issued,
                InvoiceNumber = invoiceNumber,
                IssuedDate = DateTime.UtcNow,
                DueDate = dueDate,
                Description = description,
            };

            await _invoiceRepository.CreateAsync(invoice);
        }
        #endregion
    }
}

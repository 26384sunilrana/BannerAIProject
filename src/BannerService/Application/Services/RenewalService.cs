namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;

    public class RenewalService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ISubscriptionPlanRepository _planRepository;

        public RenewalService(
            ISubscriptionRepository subscriptionRepository,
            IInvoiceRepository invoiceRepository,
            ISubscriptionPlanRepository planRepository)
        {
            _subscriptionRepository = subscriptionRepository;
            _invoiceRepository = invoiceRepository;
            _planRepository = planRepository;
        }

        public async Task<List<Subscription>> GetPendingRenewalsAsync()
        {
            return await _subscriptionRepository.GetRenewingSoonAsync(7);
        }

        public async Task<List<Subscription>> GetExpiringTodayAsync()
        {
            return await _subscriptionRepository.GetExpiringTodayAsync();
        }

        public async Task<(bool success, string message)> ProcessRenewalAsync(Guid subscriptionId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                // Check if renewal date has arrived
                if (subscription.RenewalDate > DateTime.UtcNow)
                    return (false, "Renewal date not reached yet");

                var plan = subscription.Plan;
                if (plan == null)
                    return (false, "Subscription plan not found");

                // Update subscription status and renewal date
                subscription.Status = SubscriptionStatus.RenewalPending;
                subscription.RenewalDate = subscription.RenewalDate.AddMonths(1);

                await _subscriptionRepository.UpdateAsync(subscription);

                // Create invoice for renewal
                var invoiceNumber = await _invoiceRepository.GenerateInvoiceNumberAsync();
                var invoice = new Invoice
                {
                    SubscriptionId = subscription.Id,
                    ShopId = subscription.ShopId,
                    Amount = subscription.CurrentPrice,
                    Status = InvoiceStatus.Issued,
                    InvoiceNumber = invoiceNumber,
                    IssuedDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    Description = $"Renewal - {plan.Name} ({subscription.BillingPeriod})",
                };

                await _invoiceRepository.CreateAsync(invoice);

                return (true, "Renewal processed successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error processing renewal: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> HandlePaymentFailureAsync(
            Guid subscriptionId,
            string reason)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                subscription.PaymentFailureCount++;
                subscription.LastPaymentAttempt = DateTime.UtcNow;

                // After 3 failed attempts, move to grace period or suspend
                if (subscription.PaymentFailureCount >= 3)
                {
                    subscription.Status = SubscriptionStatus.GracePeriod;
                }
                else
                {
                    subscription.Status = SubscriptionStatus.PaymentFailed;
                }

                await _subscriptionRepository.UpdateAsync(subscription);

                return (true, "Payment failure recorded");
            }
            catch (Exception ex)
            {
                return (false, $"Error handling payment failure: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ProcessGracePeriodAsync(Guid subscriptionId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                if (subscription.Status != SubscriptionStatus.GracePeriod)
                    return (false, "Subscription is not in grace period");

                // Calculate grace period end (typically 7 days after renewal date)
                var gracePeriodEnd = subscription.RenewalDate.AddDays(7);

                if (DateTime.UtcNow > gracePeriodEnd)
                {
                    // Grace period expired, suspend subscription
                    subscription.Status = SubscriptionStatus.Suspended;
                    await _subscriptionRepository.UpdateAsync(subscription);
                    return (true, "Subscription suspended - grace period expired");
                }

                return (true, "Subscription still in grace period");
            }
            catch (Exception ex)
            {
                return (false, $"Error processing grace period: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> RetryPaymentAsync(
            Guid invoiceId)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
                if (invoice == null)
                    return (false, "Invoice not found");

                if (invoice.Status == InvoiceStatus.Paid)
                    return (false, "Invoice already paid");

                var subscription = await _subscriptionRepository.GetByIdAsync(invoice.SubscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                // Simulate payment retry
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.UtcNow;

                await _invoiceRepository.UpdateAsync(invoice);

                // Reset payment failure count and update subscription
                subscription.PaymentFailureCount = 0;
                subscription.Status = SubscriptionStatus.Active;
                subscription.LastPaymentAttempt = DateTime.UtcNow;

                await _subscriptionRepository.UpdateAsync(subscription);

                return (true, "Payment retry successful");
            }
            catch (Exception ex)
            {
                return (false, $"Error retrying payment: {ex.Message}");
            }
        }

        public async Task<List<Invoice>> GetOverdueInvoicesAsync()
        {
            return await _invoiceRepository.GetOverdueAsync();
        }

        public async Task<List<Invoice>> GetUnpaidInvoicesAsync()
        {
            return await _invoiceRepository.GetUnpaidAsync();
        }

        public async Task<(bool success, string message)> MarkInvoiceOverdueAsync(Guid invoiceId)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
                if (invoice == null)
                    return (false, "Invoice not found");

                if (invoice.Status != InvoiceStatus.Issued)
                    return (false, "Invoice must be in Issued status");

                invoice.Status = InvoiceStatus.Overdue;
                await _invoiceRepository.UpdateAsync(invoice);

                return (true, "Invoice marked as overdue");
            }
            catch (Exception ex)
            {
                return (false, $"Error marking invoice overdue: {ex.Message}");
            }
        }
    }
}

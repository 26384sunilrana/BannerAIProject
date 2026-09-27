namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;

    public class BillingService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ISubscriptionPlanRepository _planRepository;

        public BillingService(
            ISubscriptionRepository subscriptionRepository,
            IInvoiceRepository invoiceRepository,
            ISubscriptionPlanRepository planRepository)
        {
            _subscriptionRepository = subscriptionRepository;
            _invoiceRepository = invoiceRepository;
            _planRepository = planRepository;
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId)
        {
            return await _invoiceRepository.GetByIdAsync(invoiceId);
        }

        public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            return await _invoiceRepository.GetByInvoiceNumberAsync(invoiceNumber);
        }

        public async Task<List<Invoice>> GetShopInvoicesAsync(Guid shopId)
        {
            return await _invoiceRepository.GetByShopIdAsync(shopId);
        }

        public async Task<List<Invoice>> GetSubscriptionInvoicesAsync(Guid subscriptionId)
        {
            return await _invoiceRepository.GetBySubscriptionIdAsync(subscriptionId);
        }

        public async Task<List<Invoice>> GetInvoicesByStatusAsync(InvoiceStatus status)
        {
            return await _invoiceRepository.GetByStatusAsync(status);
        }

        public async Task<List<Invoice>> GetOverdueInvoicesAsync()
        {
            return await _invoiceRepository.GetOverdueAsync();
        }

        public async Task<List<Invoice>> GetUnpaidInvoicesAsync()
        {
            return await _invoiceRepository.GetUnpaidAsync();
        }

        public async Task<List<Invoice>> GetPaginatedInvoicesAsync(
            Guid shopId,
            int pageNumber,
            int pageSize)
        {
            return await _invoiceRepository.GetPaginatedAsync(shopId, pageNumber, pageSize);
        }

        public async Task<int> GetInvoiceCountAsync()
        {
            return await _invoiceRepository.GetCountAsync();
        }

        public async Task<int> GetInvoiceCountByStatusAsync(InvoiceStatus status)
        {
            return await _invoiceRepository.GetCountByStatusAsync(status);
        }

        public async Task<(bool success, string message)> MarkInvoiceAsPaidAsync(
            Guid invoiceId,
            string? paymentReference = null)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
                if (invoice == null)
                    return (false, "Invoice not found");

                if (invoice.Status == InvoiceStatus.Paid)
                    return (false, "Invoice is already paid");

                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(paymentReference))
                {
                    invoice.PaymentReference = paymentReference;
                }

                await _invoiceRepository.UpdateAsync(invoice);

                // Update subscription payment status
                var subscription = await _subscriptionRepository.GetByIdAsync(invoice.SubscriptionId);
                if (subscription != null)
                {
                    subscription.PaymentFailureCount = 0;
                    subscription.LastPaymentAttempt = DateTime.UtcNow;

                    if (subscription.Status == SubscriptionStatus.PaymentFailed ||
                        subscription.Status == SubscriptionStatus.GracePeriod)
                    {
                        subscription.Status = SubscriptionStatus.Active;
                    }

                    await _subscriptionRepository.UpdateAsync(subscription);
                }

                return (true, "Invoice marked as paid");
            }
            catch (Exception ex)
            {
                return (false, $"Error marking invoice as paid: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> MarkInvoiceAsCancelledAsync(
            Guid invoiceId,
            string? reason = null)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
                if (invoice == null)
                    return (false, "Invoice not found");

                if (invoice.Status == InvoiceStatus.Cancelled || invoice.Status == InvoiceStatus.Paid)
                    return (false, "Invoice cannot be cancelled");

                invoice.Status = InvoiceStatus.Cancelled;
                if (!string.IsNullOrEmpty(reason))
                {
                    invoice.Description = $"{invoice.Description} (Cancelled: {reason})";
                }

                await _invoiceRepository.UpdateAsync(invoice);
                return (true, "Invoice cancelled successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error cancelling invoice: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> RefundInvoiceAsync(
            Guid invoiceId,
            decimal amount,
            string? reason = null)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
                if (invoice == null)
                    return (false, "Invoice not found");

                if (invoice.Status != InvoiceStatus.Paid)
                    return (false, "Only paid invoices can be refunded");

                if (amount > invoice.Amount)
                    return (false, "Refund amount exceeds invoice amount");

                invoice.Status = InvoiceStatus.Refunded;
                invoice.Amount = amount;
                if (!string.IsNullOrEmpty(reason))
                {
                    invoice.Description = $"{invoice.Description} (Refund: {reason})";
                }

                await _invoiceRepository.UpdateAsync(invoice);
                return (true, "Invoice refunded successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error refunding invoice: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> CreateInvoiceAsync(
            Guid subscriptionId,
            Guid shopId,
            decimal amount,
            string description)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null)
                    return (false, "Subscription not found");

                var invoiceNumber = await _invoiceRepository.GenerateInvoiceNumberAsync();

                var invoice = new Invoice
                {
                    SubscriptionId = subscriptionId,
                    ShopId = shopId,
                    Amount = amount,
                    Status = InvoiceStatus.Draft,
                    InvoiceNumber = invoiceNumber,
                    IssuedDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    Description = description,
                };

                await _invoiceRepository.CreateAsync(invoice);
                return (true, "Invoice created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating invoice: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> IssueInvoiceAsync(Guid invoiceId)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
                if (invoice == null)
                    return (false, "Invoice not found");

                if (invoice.Status != InvoiceStatus.Draft)
                    return (false, "Only draft invoices can be issued");

                invoice.Status = InvoiceStatus.Issued;
                invoice.IssuedDate = DateTime.UtcNow;

                await _invoiceRepository.UpdateAsync(invoice);
                return (true, "Invoice issued successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error issuing invoice: {ex.Message}");
            }
        }

        public async Task<decimal> CalculateMonthlyRecurringRevenueAsync()
        {
            try
            {
                var activeSubscriptions = await _subscriptionRepository.GetByStatusAsync(SubscriptionStatus.Active);

                decimal mrr = 0m;
                foreach (var sub in activeSubscriptions)
                {
                    if (sub.BillingPeriod == BillingPeriod.Monthly)
                    {
                        mrr += sub.CurrentPrice;
                    }
                    else if (sub.BillingPeriod == BillingPeriod.Annual)
                    {
                        mrr += sub.CurrentPrice / 12;
                    }
                }

                return Math.Round(mrr, 2);
            }
            catch
            {
                return 0m;
            }
        }

        public async Task<decimal> CalculateAnnualRecurringRevenueAsync()
        {
            try
            {
                var activeSubscriptions = await _subscriptionRepository.GetByStatusAsync(SubscriptionStatus.Active);

                decimal arr = 0m;
                foreach (var sub in activeSubscriptions)
                {
                    if (sub.BillingPeriod == BillingPeriod.Monthly)
                    {
                        arr += sub.CurrentPrice * 12;
                    }
                    else if (sub.BillingPeriod == BillingPeriod.Annual)
                    {
                        arr += sub.CurrentPrice;
                    }
                }

                return Math.Round(arr, 2);
            }
            catch
            {
                return 0m;
            }
        }

        public async Task<(decimal monthlyTotal, decimal annualTotal)> GetRevenueAsync()
        {
            try
            {
                var paidInvoices = await _invoiceRepository.GetByStatusAsync(InvoiceStatus.Paid);

                decimal monthlyTotal = 0m;
                decimal annualTotal = 0m;

                var now = DateTime.UtcNow;
                var thisMonth = new DateTime(now.Year, now.Month, 1);

                foreach (var invoice in paidInvoices)
                {
                    if (invoice.PaidDate.HasValue)
                    {
                        var paidMonth = new DateTime(invoice.PaidDate.Value.Year, invoice.PaidDate.Value.Month, 1);
                        if (paidMonth == thisMonth)
                        {
                            monthlyTotal += invoice.Amount;
                        }

                        if (invoice.PaidDate.Value.Year == now.Year)
                        {
                            annualTotal += invoice.Amount;
                        }
                    }
                }

                return (Math.Round(monthlyTotal, 2), Math.Round(annualTotal, 2));
            }
            catch
            {
                return (0m, 0m);
            }
        }

        public async Task<BillingMetrics> GetBillingMetricsAsync()
        {
            try
            {
                var totalInvoices = await _invoiceRepository.GetCountAsync();
                var paidCount = await _invoiceRepository.GetCountByStatusAsync(InvoiceStatus.Paid);
                var overdueCount = await _invoiceRepository.GetCountByStatusAsync(InvoiceStatus.Overdue);
                var draftCount = await _invoiceRepository.GetCountByStatusAsync(InvoiceStatus.Draft);

                var (monthlyRevenue, annualRevenue) = await GetRevenueAsync();
                var mrr = await CalculateMonthlyRecurringRevenueAsync();
                var arr = await CalculateAnnualRecurringRevenueAsync();

                return new BillingMetrics
                {
                    TotalInvoices = totalInvoices,
                    PaidInvoices = paidCount,
                    OverdueInvoices = overdueCount,
                    DraftInvoices = draftCount,
                    MonthlyRevenue = monthlyRevenue,
                    AnnualRevenue = annualRevenue,
                    MonthlyCurringRevenue = mrr,
                    AnnualRecurringRevenue = arr,
                    CollectionRate = totalInvoices > 0 ? (paidCount / (decimal)totalInvoices) * 100 : 0m
                };
            }
            catch
            {
                return new BillingMetrics();
            }
        }
    }

    public class BillingMetrics
    {
        public int TotalInvoices { get; set; }
        public int PaidInvoices { get; set; }
        public int OverdueInvoices { get; set; }
        public int DraftInvoices { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal AnnualRevenue { get; set; }
        public decimal MonthlyCurringRevenue { get; set; }
        public decimal AnnualRecurringRevenue { get; set; }
        public decimal CollectionRate { get; set; }
    }
}

namespace BannerService.Presentation.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Application.Services;
    using Application.DTOs;
    using Domain.Entities;

    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly BillingService _billingService;
        private readonly RenewalService _renewalService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            BillingService billingService,
            RenewalService renewalService,
            ILogger<InvoicesController> logger)
        {
            _billingService = billingService;
            _renewalService = renewalService;
            _logger = logger;
        }

        [HttpGet("{invoiceId}")]
        public async Task<IActionResult> GetInvoice(Guid invoiceId)
        {
            try
            {
                var invoice = await _billingService.GetInvoiceByIdAsync(invoiceId);
                if (invoice == null)
                    return NotFound(new { success = false, message = "Invoice not found" });

                var invoiceDto = MapToDto(invoice);
                return Ok(new { success = true, data = invoiceDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoice {invoiceId}", invoiceId);
                return StatusCode(500, new { success = false, message = "Error fetching invoice" });
            }
        }

        [HttpGet("number/{invoiceNumber}")]
        public async Task<IActionResult> GetInvoiceByNumber(string invoiceNumber)
        {
            try
            {
                var invoice = await _billingService.GetInvoiceByNumberAsync(invoiceNumber);
                if (invoice == null)
                    return NotFound(new { success = false, message = "Invoice not found" });

                var invoiceDto = MapToDto(invoice);
                return Ok(new { success = true, data = invoiceDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoice {invoiceNumber}", invoiceNumber);
                return StatusCode(500, new { success = false, message = "Error fetching invoice" });
            }
        }

        [HttpGet("shop/{shopId}")]
        public async Task<IActionResult> GetShopInvoices(Guid shopId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                    return BadRequest(new { success = false, message = "Page number and size must be greater than 0" });

                var invoices = await _billingService.GetPaginatedInvoicesAsync(shopId, pageNumber, pageSize);
                var invoiceDtos = invoices.Select(MapToDto).ToList();

                return Ok(new
                {
                    success = true,
                    data = invoiceDtos,
                    pagination = new { pageNumber, pageSize, totalCount = await _billingService.GetInvoiceCountAsync() }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoices for shop {shopId}", shopId);
                return StatusCode(500, new { success = false, message = "Error fetching invoices" });
            }
        }

        [HttpGet("subscription/{subscriptionId}")]
        public async Task<IActionResult> GetSubscriptionInvoices(Guid subscriptionId)
        {
            try
            {
                var invoices = await _billingService.GetSubscriptionInvoicesAsync(subscriptionId);
                var invoiceDtos = invoices.Select(MapToDto).ToList();

                return Ok(new { success = true, data = invoiceDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoices for subscription {subscriptionId}", subscriptionId);
                return StatusCode(500, new { success = false, message = "Error fetching invoices" });
            }
        }

        [HttpGet("status/{status}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetInvoicesByStatus(string status)
        {
            try
            {
                if (!Enum.TryParse<InvoiceStatus>(status, true, out var invoiceStatus))
                    return BadRequest(new { success = false, message = "Invalid invoice status" });

                var invoices = await _billingService.GetInvoicesByStatusAsync(invoiceStatus);
                var invoiceDtos = invoices.Select(MapToDto).ToList();

                return Ok(new { success = true, data = invoiceDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoices by status {status}", status);
                return StatusCode(500, new { success = false, message = "Error fetching invoices" });
            }
        }

        [HttpGet("overdue")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOverdueInvoices()
        {
            try
            {
                var invoices = await _renewalService.GetOverdueInvoicesAsync();
                var invoiceDtos = invoices.Select(MapToDto).ToList();

                return Ok(new { success = true, data = invoiceDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching overdue invoices");
                return StatusCode(500, new { success = false, message = "Error fetching overdue invoices" });
            }
        }

        [HttpGet("unpaid")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUnpaidInvoices()
        {
            try
            {
                var invoices = await _renewalService.GetUnpaidInvoicesAsync();
                var invoiceDtos = invoices.Select(MapToDto).ToList();

                return Ok(new { success = true, data = invoiceDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unpaid invoices");
                return StatusCode(500, new { success = false, message = "Error fetching unpaid invoices" });
            }
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _billingService.CreateInvoiceAsync(
                    request.SubscriptionId,
                    request.ShopId,
                    request.Amount,
                    request.Description);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                return Ok(new { success = true, message = result.message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, new { success = false, message = "Error creating invoice" });
            }
        }

        [HttpPost("{invoiceId}/issue")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> IssueInvoice(Guid invoiceId)
        {
            try
            {
                var result = await _billingService.IssueInvoiceAsync(invoiceId);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                var invoice = await _billingService.GetInvoiceByIdAsync(invoiceId);
                var invoiceDto = MapToDto(invoice!);

                return Ok(new { success = true, message = result.message, data = invoiceDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error issuing invoice {invoiceId}", invoiceId);
                return StatusCode(500, new { success = false, message = "Error issuing invoice" });
            }
        }

        [HttpPost("{invoiceId}/mark-paid")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkAsPaid(Guid invoiceId, [FromBody] MarkInvoicePaidDto request)
        {
            try
            {
                var result = await _billingService.MarkInvoiceAsPaidAsync(invoiceId, request.PaymentReference);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                var invoice = await _billingService.GetInvoiceByIdAsync(invoiceId);
                var invoiceDto = MapToDto(invoice!);

                return Ok(new { success = true, message = result.message, data = invoiceDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice paid {invoiceId}", invoiceId);
                return StatusCode(500, new { success = false, message = "Error marking invoice as paid" });
            }
        }

        [HttpPost("{invoiceId}/cancel")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelInvoice(Guid invoiceId, [FromBody] CancelInvoiceDto request)
        {
            try
            {
                var result = await _billingService.MarkInvoiceAsCancelledAsync(invoiceId, request.Reason);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                return Ok(new { success = true, message = result.message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling invoice {invoiceId}", invoiceId);
                return StatusCode(500, new { success = false, message = "Error cancelling invoice" });
            }
        }

        [HttpPost("{invoiceId}/refund")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> RefundInvoice(Guid invoiceId, [FromBody] RefundInvoiceDto request)
        {
            try
            {
                var result = await _billingService.RefundInvoiceAsync(invoiceId, request.Amount, request.Reason);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                return Ok(new { success = true, message = result.message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding invoice {invoiceId}", invoiceId);
                return StatusCode(500, new { success = false, message = "Error refunding invoice" });
            }
        }

        [HttpPost("{invoiceId}/retry-payment")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> RetryPayment(Guid invoiceId)
        {
            try
            {
                var result = await _renewalService.RetryPaymentAsync(invoiceId);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                var invoice = await _billingService.GetInvoiceByIdAsync(invoiceId);
                var invoiceDto = MapToDto(invoice!);

                return Ok(new { success = true, message = result.message, data = invoiceDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying payment for invoice {invoiceId}", invoiceId);
                return StatusCode(500, new { success = false, message = "Error retrying payment" });
            }
        }

        [HttpPost("{invoiceId}/mark-overdue")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkOverdue(Guid invoiceId)
        {
            try
            {
                var result = await _renewalService.MarkInvoiceOverdueAsync(invoiceId);

                if (!result.success)
                    return BadRequest(new { success = false, message = result.message });

                return Ok(new { success = true, message = result.message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice overdue {invoiceId}", invoiceId);
                return StatusCode(500, new { success = false, message = "Error marking invoice overdue" });
            }
        }

        [HttpGet("metrics")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetBillingMetrics()
        {
            try
            {
                var metrics = await _billingService.GetBillingMetricsAsync();

                return Ok(new { success = true, data = metrics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching billing metrics");
                return StatusCode(500, new { success = false, message = "Error fetching billing metrics" });
            }
        }

        private InvoiceDto MapToDto(Invoice invoice)
        {
            return new InvoiceDto
            {
                Id = invoice.Id,
                SubscriptionId = invoice.SubscriptionId,
                ShopId = invoice.ShopId,
                Amount = invoice.Amount,
                Status = invoice.Status,
                InvoiceNumber = invoice.InvoiceNumber,
                IssuedDate = invoice.IssuedDate,
                DueDate = invoice.DueDate,
                PaidDate = invoice.PaidDate,
                Description = invoice.Description,
                PaymentReference = invoice.PaymentReference,
                CreatedAt = invoice.CreatedAt
            };
        }
    }

    public class CreateInvoiceDto
    {
        public Guid SubscriptionId { get; set; }
        public Guid ShopId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class MarkInvoicePaidDto
    {
        public string? PaymentReference { get; set; }
    }

    public class CancelInvoiceDto
    {
        public string? Reason { get; set; }
    }

    public class RefundInvoiceDto
    {
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
    }
}

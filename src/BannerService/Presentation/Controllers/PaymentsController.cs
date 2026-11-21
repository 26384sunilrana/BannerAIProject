namespace BannerService.Presentation.Controllers;

using Application.Services;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security;

public class ConfirmPaymentDto
{
    public string OrderId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public class RefundPaymentDto
{
    /// <summary>Empty means everything that is left.</summary>
    public decimal? Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Online payment for renewals (ADR-004, ADR-006). Without a provider every call except config answers 409 no_provider.</summary>
[ApiController]
[Route("api")]
public class PaymentsController : ControllerBase
{
    public const int MaxWebhookBytes = 64 * 1024;

    private readonly PaymentService _payments;
    private readonly IPaymentRepository _repository;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(PaymentService payments, IPaymentRepository repository, ILogger<PaymentsController> logger)
    {
        _payments = payments;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Whether online payment is on, and what the browser needs. Never a secret.</summary>
    [HttpGet("payments/config")]
    [Authorize]
    public IActionResult GetConfig()
    {
        var registry = _payments.Registry;
        var provider = registry.Current;
        return Ok(new
        {
            enabled = provider != null,
            provider = provider?.Name,
            currency = registry.DefaultCurrency,
            country = registry.DefaultCountry,
        });
    }

    [HttpPost("subscriptions/{subscriptionId:guid}/checkout")]
    [Authorize]
    public async Task<IActionResult> StartCheckout(Guid subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _payments.StartCheckoutAsync(subscriptionId, cancellationToken);
            return Ok(new
            {
                provider = session.Provider,
                orderId = session.ProviderOrderId,
                amount = session.Amount.Amount,
                amountMinor = session.Amount.ToMinorUnits(),
                currency = session.Amount.Currency,
                publicKeyId = session.PublicKeyId,
            });
        }
        catch (PaymentRequestException ex)
        {
            return Refused(ex);
        }
    }

    /// <summary>The browser's report after the provider's payment form. Idempotent: the webhook may have applied the payment already.</summary>
    [HttpPost("subscriptions/{subscriptionId:guid}/checkout/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmCheckout(Guid subscriptionId, [FromBody] ConfirmPaymentDto request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId) || string.IsNullOrWhiteSpace(request.PaymentId) || string.IsNullOrWhiteSpace(request.Signature))
            return BadRequest(new { message = "Order, payment and signature are required", code = "invalid" });

        try
        {
            var outcome = await _payments.ConfirmAsync(subscriptionId, request.OrderId, request.PaymentId, request.Signature);
            return outcome is PaymentOutcome.Applied or PaymentOutcome.AlreadyApplied
                ? Ok(new { paid = true, outcome = outcome.ToString() })
                : Conflict(new { paid = false, outcome = outcome.ToString(), message = "The payment could not be applied. Contact support." });
        }
        catch (PaymentRequestException ex)
        {
            return Refused(ex);
        }
    }

    /// <summary>
    /// The provider's call. No sign-in: the signature is the proof. 200 for anything genuine (also repeats and events we ignore), so the
    /// provider stops retrying; 400 for a bad signature; 404 when no provider is set up; 413 for a big body.
    /// </summary>
    [HttpPost("payments/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        if (!_payments.Registry.HasRealProvider)
            return NotFound();

        if (Request.ContentLength > MaxWebhookBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);

        string body;
        using (var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true))
        {
            var buffer = new char[MaxWebhookBytes + 1];
            var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
            if (read > MaxWebhookBytes)
                return StatusCode(StatusCodes.Status413PayloadTooLarge);
            body = new string(buffer, 0, read);
        }

        var headers = Request.Headers.ToDictionary(h => h.Key.ToLowerInvariant(), h => h.Value.ToString());

        try
        {
            var outcome = await _payments.HandleWebhookAsync(body, headers);
            return Ok(new { outcome = outcome.ToString() });
        }
        catch (PaymentRequestException ex) when (ex.Code == "bad_signature")
        {
            return BadRequest(new { message = "Invalid signature", code = "bad_signature" });
        }
        catch (Exception ex)
        {
            // a 500 makes the provider repeat the event later; the receipt is not marked done, so it is applied then
            _logger.LogError(ex, "A payment webhook could not be applied");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Payments and refunds of one shop (the owner of that shop, or an administrator). Provider ids are not shown.</summary>
    [HttpGet("shops/{shopId:guid}/payments")]
    [Authorize]
    public async Task<IActionResult> ListForShop(Guid shopId)
    {
        var attempts = await _repository.ListForShopAsync(shopId);
        return Ok(attempts.Select(a => new
        {
            id = a.Id,
            subscriptionId = a.SubscriptionId,
            invoiceId = a.InvoiceId,
            provider = a.Provider,
            amount = a.Amount,
            currency = a.Currency,
            status = a.Status.ToString(),
            refundable = a.RefundableAmount,
            createdAt = a.CreatedAt,
            completedAt = a.CompletedAt,
            refunds = a.Refunds.Select(r => new { id = r.Id, amount = r.Amount, currency = r.Currency, reason = r.Reason, status = r.Status.ToString(), createdAt = r.CreatedAt }),
        }));
    }

    [HttpPost("admin/payments/{paymentAttemptId:guid}/refund")]
    [Authorize(Roles = TenantAccess.AdminRole)]
    public async Task<IActionResult> Refund(Guid paymentAttemptId, [FromBody] RefundPaymentDto request, CancellationToken cancellationToken)
    {
        try
        {
            var refund = await _payments.RefundAsync(paymentAttemptId, request.Amount, request.Reason, User.GetUserId(), cancellationToken);
            return Ok(new { id = refund.Id, amount = refund.Amount, currency = refund.Currency, status = refund.Status.ToString() });
        }
        catch (PaymentRequestException ex)
        {
            return Refused(ex);
        }
        catch (PaymentProviderException ex)
        {
            _logger.LogWarning(ex, "A refund could not be made");
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "The payment service could not be reached", code = "provider_error" });
        }
    }

    private IActionResult Refused(PaymentRequestException ex)
    {
        var body = new { message = ex.Message, code = ex.Code };
        return ex.Code switch
        {
            "not_found" => NotFound(body),
            "provider_error" => StatusCode(StatusCodes.Status502BadGateway, body),
            "provider_refused" => StatusCode(StatusCodes.Status502BadGateway, body),
            "too_much" or "reason_required" or "bad_signature" or "invalid" => BadRequest(body),
            _ => Conflict(body),
        };
    }
}

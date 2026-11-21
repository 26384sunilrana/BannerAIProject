namespace BannerService.Application.Services;

using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Monitoring;

public enum PaymentOutcome
{
    Applied,
    AlreadyApplied,
    UnknownOrder,
    AmountMismatch,
    Failed,
    Ignored,
}

/// <summary>Why a checkout or refund request was refused; the controller turns it into an answer.</summary>
public class PaymentRequestException : Exception
{
    public string Code { get; }
    public PaymentRequestException(string code, string message) : base(message) { Code = code; }
}

/// <summary>
/// Starts payments, applies their result and refunds them (ADR-006). "Paid" is only ever set by <see cref="ApplyAsync"/>, which the
/// browser's confirmation and the provider's webhook both call; it verifies, records a receipt, checks the amount and pays and renews
/// in one transaction, so a repeated or reordered report changes nothing.
/// </summary>
public class PaymentService
{
    private readonly PaymentProviderRegistry _registry;
    private readonly IPaymentRepository _payments;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly SubscriptionLifecycleService _lifecycle;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NotificationService _notifications;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        PaymentProviderRegistry registry,
        IPaymentRepository payments,
        ISubscriptionRepository subscriptions,
        SubscriptionLifecycleService lifecycle,
        IUnitOfWork unitOfWork,
        NotificationService notifications,
        ILogger<PaymentService> logger)
    {
        _registry = registry;
        _payments = payments;
        _subscriptions = subscriptions;
        _lifecycle = lifecycle;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _logger = logger;
    }

    public PaymentProviderRegistry Registry => _registry;

    public async Task<CheckoutSession> StartCheckoutAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var provider = _registry.Current
            ?? throw new PaymentRequestException("no_provider", "Online payment is not set up. Use Renew instead.");

        var subscription = await _subscriptions.GetByIdAsync(subscriptionId)
            ?? throw new PaymentRequestException("not_found", "Subscription not found");
        if (subscription.Status == SubscriptionStatus.Cancelled)
            throw new PaymentRequestException("cancelled", "A cancelled subscription cannot be renewed; choose a plan instead");
        if (subscription.CurrentPrice <= 0)
            throw new PaymentRequestException("nothing_to_pay", "This plan has nothing to pay");

        var currency = CurrencyCodes.Normalize(subscription.Plan?.Currency, _registry.DefaultCurrency);
        var amount = Money.Of(subscription.CurrentPrice, currency);

        var attempt = await _payments.AddAttemptAsync(new PaymentAttempt
        {
            SubscriptionId = subscription.Id,
            ShopId = subscription.ShopId,
            Provider = provider.Name,
            ProviderOrderId = "pending-" + Guid.NewGuid().ToString("N"),
            Amount = amount.Amount,
            Currency = amount.Currency,
        });

        try
        {
            // The reference is our own id; no names, addresses or e-mail go to the provider
            var session = await provider.CreateCheckoutAsync(new CheckoutRequest(amount, attempt.Id.ToString("N")), cancellationToken);
            attempt.ProviderOrderId = session.ProviderOrderId;
            await _payments.UpdateAttemptAsync(attempt);
            PaymentMetrics.CheckoutsStarted.WithLabels(provider.Name).Inc();
            return session;
        }
        catch (PaymentProviderException ex)
        {
            attempt.MarkFailed("provider_error");
            await _payments.UpdateAttemptAsync(attempt);
            _logger.LogWarning(ex, "The {Provider} checkout for subscription {SubscriptionId} could not be started", provider.Name, subscriptionId);
            throw new PaymentRequestException("provider_error", "The payment service could not be reached. Please try again in a moment.");
        }
    }

    /// <summary>The browser's report after the provider's payment form: order, payment and the provider's signature.</summary>
    public async Task<PaymentOutcome> ConfirmAsync(Guid subscriptionId, string orderId, string paymentId, string signature)
    {
        var provider = _registry.Current
            ?? throw new PaymentRequestException("no_provider", "Online payment is not set up.");

        var attempt = await _payments.GetAttemptByOrderIdAsync(provider.Name, orderId);
        if (attempt == null || attempt.SubscriptionId != subscriptionId)
            throw new PaymentRequestException("not_found", "Payment not found");

        if (!provider.VerifyCheckoutSignature(orderId, paymentId, signature))
        {
            PaymentMetrics.PaymentsRejected.WithLabels("bad_signature").Inc();
            throw new PaymentRequestException("bad_signature", "The payment could not be verified");
        }

        // No amount here: the attempt holds the amount we asked for. The webhook carries the provider's own figure and is checked against it.
        return await ApplyAsync(provider.Name, new PaymentEvent("confirm:" + paymentId, PaymentEventType.PaymentSucceeded, orderId, paymentId, null));
    }

    /// <summary>Handles a webhook. Throws <see cref="PaymentRequestException"/> with code bad_signature when it is not genuine.</summary>
    public async Task<PaymentOutcome> HandleWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers)
    {
        var provider = _registry.Current
            ?? throw new PaymentRequestException("no_provider", "Online payment is not set up.");

        var parsed = provider.ParseWebhook(rawBody, headers);
        if (!parsed.SignatureValid)
        {
            PaymentMetrics.PaymentsRejected.WithLabels("bad_signature").Inc();
            throw new PaymentRequestException("bad_signature", "Invalid signature");
        }

        if (parsed.Event == null)
            return PaymentOutcome.Ignored;

        return await ApplyAsync(provider.Name, parsed.Event);
    }

    /// <summary>The one place where a payment result is applied (ADR-006).</summary>
    public async Task<PaymentOutcome> ApplyAsync(string providerName, PaymentEvent paymentEvent)
    {
        var (receipt, alreadyProcessed) = await _payments.RecordReceiptAsync(providerName, paymentEvent.ProviderEventId, paymentEvent.Type.ToString());
        if (alreadyProcessed)
            return PaymentOutcome.AlreadyApplied;

        PaymentOutcome outcome;
        try
        {
            outcome = await ApplyCoreAsync(providerName, paymentEvent);
        }
        catch (PaymentConcurrencyException)
        {
            // another request applied the same payment a moment ago
            outcome = PaymentOutcome.AlreadyApplied;
        }

        receipt.ProcessedAt = DateTime.UtcNow;
        receipt.Outcome = outcome.ToString();
        await _payments.UpdateReceiptAsync(receipt);
        return outcome;
    }

    private async Task<PaymentOutcome> ApplyCoreAsync(string providerName, PaymentEvent ev)
    {
        if (ev.Type == PaymentEventType.RefundDone)
            return PaymentOutcome.Ignored; // refunds are recorded when an administrator asks for them

        if (string.IsNullOrEmpty(ev.ProviderOrderId))
            return PaymentOutcome.UnknownOrder;

        var attempt = await _payments.GetAttemptByOrderIdAsync(providerName, ev.ProviderOrderId);
        if (attempt == null)
        {
            _logger.LogWarning("A {Provider} {Type} event named an order we do not know", providerName, ev.Type);
            return PaymentOutcome.UnknownOrder;
        }

        if (attempt.Status != PaymentAttemptStatus.Created)
            return PaymentOutcome.AlreadyApplied;

        if (ev.Type == PaymentEventType.PaymentFailed)
        {
            attempt.MarkFailed(ev.FailureReason ?? "payment_failed");
            await _payments.UpdateAttemptAsync(attempt);
            var failing = await _subscriptions.GetByIdAsync(attempt.SubscriptionId);
            if (failing != null)
            {
                failing.RecordPaymentAttempt();
                await _subscriptions.UpdateAsync(failing);
            }
            PaymentMetrics.PaymentsFailed.WithLabels(providerName).Inc();
            return PaymentOutcome.Failed;
        }

        // Never apply a payment for another amount or currency than we asked for (ADR-006)
        if (ev.Amount is { } paid && !paid.Matches(Money.Of(attempt.Amount, attempt.Currency)))
        {
            attempt.FailureReason = "amount_mismatch";
            await _payments.UpdateAttemptAsync(attempt);
            PaymentMetrics.PaymentsRejected.WithLabels("amount_mismatch").Inc();
            _logger.LogWarning("Payment {AttemptId} not applied: the provider reported another amount or currency", attempt.Id);
            await _notifications.NotifyAdminsAsync("PaymentMismatch", "A payment does not match its order",
                $"A payment for shop {attempt.ShopId} was reported with another amount or currency than was asked for. It was not applied; check the provider's dashboard.");
            return PaymentOutcome.AmountMismatch;
        }

        var subscription = await _subscriptions.GetByIdAsync(attempt.SubscriptionId);
        if (subscription == null)
            return PaymentOutcome.UnknownOrder;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // database work only inside the transaction; the e-mail and SMS go out after it is committed
            var invoice = await _lifecycle.RenewAfterPaymentAsync(subscription, ev.ProviderPaymentId, attempt.Currency);
            attempt.MarkPaid(ev.ProviderPaymentId, invoice.Id);
            await _payments.UpdateAttemptAsync(attempt);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        PaymentMetrics.PaymentsSucceeded.WithLabels(providerName).Inc();
        await _lifecycle.NotifyRenewedAsync(subscription);
        return PaymentOutcome.Applied;
    }

    /// <summary>Refunds all or part of a paid payment. Amount null means everything that is left.</summary>
    public async Task<Refund> RefundAsync(Guid paymentAttemptId, decimal? amount, string reason, Guid requestedByUserId, CancellationToken cancellationToken)
    {
        var provider = _registry.Current
            ?? throw new PaymentRequestException("no_provider", "Online payment is not set up.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new PaymentRequestException("reason_required", "Give a reason for the refund");

        var attempt = await _payments.GetAttemptAsync(paymentAttemptId)
            ?? throw new PaymentRequestException("not_found", "Payment not found");
        if (!attempt.IsPaid || string.IsNullOrEmpty(attempt.ProviderPaymentId))
            throw new PaymentRequestException("not_paid", "Only a paid payment can be refunded");
        if (!string.Equals(attempt.Provider, provider.Name, StringComparison.OrdinalIgnoreCase))
            throw new PaymentRequestException("other_provider", "This payment was taken through another provider");

        var refundAmount = amount ?? attempt.RefundableAmount;
        if (refundAmount <= 0 || refundAmount > attempt.RefundableAmount)
            throw new PaymentRequestException("too_much", $"At most {attempt.RefundableAmount:0.00} {attempt.Currency} can still be refunded");

        var result = await provider.RefundAsync(attempt.ProviderPaymentId, Money.Of(refundAmount, attempt.Currency), reason.Trim(), cancellationToken);
        if (!result.Success)
            throw new PaymentRequestException("provider_refused", result.Error ?? "The payment service did not accept the refund");

        var refund = new Refund
        {
            PaymentAttemptId = attempt.Id,
            Amount = refundAmount,
            Currency = attempt.Currency,
            Reason = reason.Trim().Length > 256 ? reason.Trim()[..256] : reason.Trim(),
            RequestedByUserId = requestedByUserId,
            ProviderRefundId = result.ProviderRefundId,
        };
        attempt.ApplyRefund(refund);
        await _payments.AddRefundAsync(refund);
        PaymentMetrics.Refunds.WithLabels(provider.Name).Inc();
        return refund;
    }
}

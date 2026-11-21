using Xunit;
using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BannerService.Application.Tests.Providers;

/// <summary>A payment provider that does what the test tells it to.</summary>
public class FakePaymentProvider : IPaymentProvider
{
    public string Name => "Fake";
    public bool IsConfigured { get; set; } = true;
    public bool SignatureOk { get; set; } = true;
    public WebhookParseResult NextWebhook { get; set; } = new(true, null);
    public bool FailCheckout { get; set; }
    public bool RefundSucceeds { get; set; } = true;
    public int Orders { get; private set; }
    public List<(string paymentId, Money amount)> Refunds { get; } = new();
    public CheckoutRequest? LastRequest { get; private set; }

    public Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        if (FailCheckout) throw new PaymentProviderException("down");
        Orders++;
        return Task.FromResult(new CheckoutSession(Name, $"order_{Orders}", request.Amount, "public-key"));
    }

    public bool VerifyCheckoutSignature(string providerOrderId, string providerPaymentId, string signature) => SignatureOk;

    public WebhookParseResult ParseWebhook(string rawBody, IReadOnlyDictionary<string, string> headers) => NextWebhook;

    public Task<ProviderRefundResult> RefundAsync(string providerPaymentId, Money amount, string reason, CancellationToken cancellationToken)
    {
        Refunds.Add((providerPaymentId, amount));
        return Task.FromResult(RefundSucceeds ? new ProviderRefundResult(true, "rfnd_1", null) : new ProviderRefundResult(false, null, "refused"));
    }
}

public class InMemoryPayments : IPaymentRepository
{
    public List<PaymentAttempt> Attempts { get; } = new();
    public List<WebhookReceipt> Receipts { get; } = new();
    public bool ConflictOnNextUpdate { get; set; }

    public Task<PaymentAttempt> AddAttemptAsync(PaymentAttempt attempt) { Attempts.Add(attempt); return Task.FromResult(attempt); }
    public Task<PaymentAttempt?> GetAttemptAsync(Guid attemptId) => Task.FromResult(Attempts.FirstOrDefault(a => a.Id == attemptId));
    public Task<PaymentAttempt?> GetAttemptByOrderIdAsync(string provider, string orderId) =>
        Task.FromResult(Attempts.FirstOrDefault(a => a.Provider == provider && a.ProviderOrderId == orderId));
    public Task<List<PaymentAttempt>> ListForShopAsync(Guid shopId) => Task.FromResult(Attempts.Where(a => a.ShopId == shopId).ToList());

    public Task UpdateAttemptAsync(PaymentAttempt attempt)
    {
        if (ConflictOnNextUpdate)
        {
            ConflictOnNextUpdate = false;
            throw new PaymentConcurrencyException(new Exception("conflict"));
        }
        return Task.CompletedTask;
    }

    public Task AddRefundAsync(Refund refund) => Task.CompletedTask;

    public Task<(WebhookReceipt receipt, bool alreadyProcessed)> RecordReceiptAsync(string provider, string eventId, string eventType)
    {
        var existing = Receipts.FirstOrDefault(r => r.Provider == provider && r.ProviderEventId == eventId);
        if (existing != null) return Task.FromResult((existing, existing.ProcessedAt != null));
        var receipt = new WebhookReceipt { Provider = provider, ProviderEventId = eventId, EventType = eventType };
        Receipts.Add(receipt);
        return Task.FromResult((receipt, false));
    }

    public Task UpdateReceiptAsync(WebhookReceipt receipt) => Task.CompletedTask;
    public Task<int> CleanUpAsync(DateTime nowUtc) => Task.FromResult(0);
}

public class PaymentServiceTests
{
    private readonly FakePaymentProvider _provider = new();
    private readonly InMemoryPayments _payments = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly Mock<IInvoiceRepository> _invoices = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ISmsSender> _sms = new();
    private readonly List<Invoice> _invoicesMade = new();
    private readonly Subscription _subscription;
    private readonly Shop _shop;

    public PaymentServiceTests()
    {
        _shop = new Shop { Id = Guid.NewGuid(), Name = "Olive Mart" };
        _subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ShopId = _shop.Id,
            Status = SubscriptionStatus.Active,
            BillingPeriod = BillingPeriod.Annual,
            CurrentPrice = 499m,
            RenewalDate = DateTime.UtcNow.Date.AddDays(5),
            Plan = new SubscriptionPlan { Name = "Gold", Currency = "INR" },
        };
        _subscriptions.Setup(r => r.GetByIdAsync(_subscription.Id)).ReturnsAsync(_subscription);
        _subscriptions.Setup(r => r.UpdateAsync(It.IsAny<Subscription>())).ReturnsAsync((Subscription s) => s);
        _invoices.Setup(r => r.GenerateInvoiceNumberAsync()).ReturnsAsync("INV-1");
        _invoices.Setup(r => r.CreateAsync(It.IsAny<Invoice>())).ReturnsAsync((Invoice i) => { i.Id = Guid.NewGuid(); _invoicesMade.Add(i); return i; });
        _notificationRepository.Setup(r => r.FindUserIdsByRoleAsync(It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync(new List<string> { "admin-1" });
    }

    private PaymentService Service(bool providerOn = true, IPaymentProvider? provider = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Payments:Provider"] = providerOn ? "Fake" : "None",
        }).Build();
        var registry = new PaymentProviderRegistry(new[] { provider ?? _provider }, config);

        var subscriptionService = new SubscriptionService(Mock.Of<ISubscriptionPlanRepository>(), _subscriptions.Object, _invoices.Object);
        var lifecycle = new SubscriptionLifecycleService(
            _subscriptions.Object, _invoices.Object, Mock.Of<IShopRepository>(), Mock.Of<IUserRepository>(), Mock.Of<IRefreshTokenRepository>(),
            _email.Object, _sms.Object, Mock.Of<IPaymentGateway>(), registry, Mock.Of<ISubscriptionNotificationRepository>(), subscriptionService,
            NullLogger<SubscriptionLifecycleService>.Instance);

        return new PaymentService(registry, _payments, _subscriptions.Object, lifecycle, _unitOfWork.Object,
            new NotificationService(_notificationRepository.Object, NullLogger<NotificationService>.Instance), NullLogger<PaymentService>.Instance);
    }

    private static PaymentEvent Succeeded(string orderId, string eventId = "evt-1", Money? amount = null) =>
        new(eventId, PaymentEventType.PaymentSucceeded, orderId, "pay_1", amount);

    // ---- checkout

    [Fact]
    public async Task Checkout_MakesAnAttemptForThePlansPriceAndCurrency_AndSendsOnlyOurReference()
    {
        var session = await Service().StartCheckoutAsync(_subscription.Id, default);

        Assert.Equal("order_1", session.ProviderOrderId);
        Assert.Equal(Money.Of(499m, "INR"), session.Amount);
        var attempt = Assert.Single(_payments.Attempts);
        Assert.Equal(PaymentAttemptStatus.Created, attempt.Status);
        Assert.Equal("order_1", attempt.ProviderOrderId);
        Assert.Equal(_shop.Id, attempt.ShopId);
        Assert.Equal(attempt.Id.ToString("N"), _provider.LastRequest!.Reference);
    }

    [Fact]
    public async Task Checkout_UsesThePlansCurrency_NotAHardCodedOne()
    {
        _subscription.Plan!.Currency = "USD";
        var session = await Service().StartCheckoutAsync(_subscription.Id, default);
        Assert.Equal("USD", session.Amount.Currency);
        Assert.Equal("USD", _payments.Attempts.Single().Currency);
    }

    [Fact]
    public async Task Checkout_WithNoProvider_IsRefusedAsNoProvider()
    {
        var ex = await Assert.ThrowsAsync<PaymentRequestException>(() => Service(providerOn: false).StartCheckoutAsync(_subscription.Id, default));
        Assert.Equal("no_provider", ex.Code);
        Assert.Empty(_payments.Attempts);
    }

    [Fact]
    public async Task Checkout_ForAProviderThatIsNotFullySetUp_IsTheSameAsNone()
    {
        var ex = await Assert.ThrowsAsync<PaymentRequestException>(
            () => Service(provider: new FakePaymentProvider { IsConfigured = false }).StartCheckoutAsync(_subscription.Id, default));
        Assert.Equal("no_provider", ex.Code);
    }

    [Fact]
    public async Task Checkout_ForAnUnknownOrCancelledSubscription_IsRefused()
    {
        var unknown = await Assert.ThrowsAsync<PaymentRequestException>(() => Service().StartCheckoutAsync(Guid.NewGuid(), default));
        Assert.Equal("not_found", unknown.Code);

        _subscription.Status = SubscriptionStatus.Cancelled;
        var cancelled = await Assert.ThrowsAsync<PaymentRequestException>(() => Service().StartCheckoutAsync(_subscription.Id, default));
        Assert.Equal("cancelled", cancelled.Code);
    }

    [Fact]
    public async Task Checkout_WhenTheProviderIsDown_FailsTheAttemptAndSaysSo()
    {
        _provider.FailCheckout = true;

        var ex = await Assert.ThrowsAsync<PaymentRequestException>(() => Service().StartCheckoutAsync(_subscription.Id, default));

        Assert.Equal("provider_error", ex.Code);
        Assert.Equal(PaymentAttemptStatus.Failed, _payments.Attempts.Single().Status);
    }

    // ---- applying a payment

    private async Task<(PaymentService service, PaymentAttempt attempt)> Started()
    {
        var service = Service();
        await service.StartCheckoutAsync(_subscription.Id, default);
        return (service, _payments.Attempts.Single());
    }

    [Fact]
    public async Task ASuccessfulPayment_PaysTheAttempt_MakesThePaidInvoice_AndRenews()
    {
        var (service, attempt) = await Started();
        var due = _subscription.RenewalDate;

        var outcome = await service.ApplyAsync("Fake", Succeeded(attempt.ProviderOrderId, amount: Money.Of(499m, "INR")));

        Assert.Equal(PaymentOutcome.Applied, outcome);
        Assert.Equal(PaymentAttemptStatus.Paid, attempt.Status);
        Assert.Equal("pay_1", attempt.ProviderPaymentId);
        var invoice = Assert.Single(_invoicesMade);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(499m, invoice.Amount);
        Assert.Equal("INR", invoice.Currency);
        Assert.Equal(invoice.Id, attempt.InvoiceId);
        Assert.Equal(due.AddMonths(12), _subscription.RenewalDate);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        Assert.NotNull(_payments.Receipts.Single().ProcessedAt);
        Assert.Equal("Applied", _payments.Receipts.Single().Outcome);
    }

    [Fact]
    public async Task TheSameEventDeliveredTwice_ChangesNothingTheSecondTime()
    {
        var (service, attempt) = await Started();
        await service.ApplyAsync("Fake", Succeeded(attempt.ProviderOrderId));

        var again = await service.ApplyAsync("Fake", Succeeded(attempt.ProviderOrderId));

        Assert.Equal(PaymentOutcome.AlreadyApplied, again);
        Assert.Single(_invoicesMade);
    }

    [Fact]
    public async Task TheBrowserConfirmAndTheWebhook_ForTheSamePayment_RenewOnlyOnce()
    {
        var (service, attempt) = await Started();

        var confirm = await service.ConfirmAsync(_subscription.Id, attempt.ProviderOrderId, "pay_1", "sig");
        var webhook = await service.ApplyAsync("Fake", Succeeded(attempt.ProviderOrderId, eventId: "evt-from-provider"));

        Assert.Equal(PaymentOutcome.Applied, confirm);
        Assert.Equal(PaymentOutcome.AlreadyApplied, webhook);
        Assert.Single(_invoicesMade);
    }

    [Fact]
    public async Task AReceiptThatWasRecordedButNeverApplied_IsAppliedWhenTheProviderRepeatsTheEvent()
    {
        var (service, attempt) = await Started();
        _payments.Receipts.Add(new WebhookReceipt { Provider = "Fake", ProviderEventId = "evt-1", EventType = "PaymentSucceeded", ProcessedAt = null });

        var outcome = await service.ApplyAsync("Fake", Succeeded(attempt.ProviderOrderId, "evt-1"));

        Assert.Equal(PaymentOutcome.Applied, outcome);
    }

    [Fact]
    public async Task TwoRequestsAtOnce_TheLoserIsTreatedAsAlreadyApplied()
    {
        var (service, attempt) = await Started();
        _payments.ConflictOnNextUpdate = true;

        var outcome = await service.ApplyAsync("Fake", Succeeded(attempt.ProviderOrderId));

        Assert.Equal(PaymentOutcome.AlreadyApplied, outcome);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Theory]
    [InlineData(498.99, "INR")]
    [InlineData(499.0, "USD")]
    public async Task APaymentForAnotherAmountOrCurrency_IsNeverApplied_AndAdministratorsAreTold(double amount, string currency)
    {
        var (service, attempt) = await Started();

        var outcome = await service.ApplyAsync("Fake", Succeeded(attempt.ProviderOrderId, amount: Money.Of((decimal)amount, currency)));

        Assert.Equal(PaymentOutcome.AmountMismatch, outcome);
        Assert.Equal(PaymentAttemptStatus.Created, attempt.Status);
        Assert.Equal("amount_mismatch", attempt.FailureReason);
        Assert.Empty(_invoicesMade);
        _notificationRepository.Verify(r => r.AddRangeAsync(It.Is<IReadOnlyCollection<InAppNotification>>(n => n.Any(x => x.UserId == "admin-1" && x.Kind == "PaymentMismatch"))), Times.Once);
    }

    [Fact]
    public async Task AFailedPayment_FailsTheAttemptAndCountsTheFailure()
    {
        var (service, attempt) = await Started();
        var before = _subscription.PaymentFailureCount;

        var outcome = await service.ApplyAsync("Fake", new PaymentEvent("evt-f", PaymentEventType.PaymentFailed, attempt.ProviderOrderId, "pay_9", null, "card_declined"));

        Assert.Equal(PaymentOutcome.Failed, outcome);
        Assert.Equal(PaymentAttemptStatus.Failed, attempt.Status);
        Assert.Equal("card_declined", attempt.FailureReason);
        Assert.Equal(before + 1, _subscription.PaymentFailureCount);
        Assert.Empty(_invoicesMade);
    }

    [Fact]
    public async Task AnEventForAnUnknownOrder_IsNotApplied()
    {
        var outcome = await Service().ApplyAsync("Fake", Succeeded("order_unknown"));
        Assert.Equal(PaymentOutcome.UnknownOrder, outcome);
        Assert.Empty(_invoicesMade);
    }

    [Fact]
    public async Task APaymentAfterTheAttemptWasFailed_IsNotApplied()
    {
        var (service, attempt) = await Started();
        attempt.MarkFailed("x");

        Assert.Equal(PaymentOutcome.AlreadyApplied, await service.ApplyAsync("Fake", Succeeded(attempt.ProviderOrderId)));
        Assert.Empty(_invoicesMade);
    }

    [Fact]
    public async Task ARefundEvent_IsRecognisedButChangesNothing()
    {
        var (service, attempt) = await Started();
        var outcome = await service.ApplyAsync("Fake", new PaymentEvent("evt-r", PaymentEventType.RefundDone, attempt.ProviderOrderId, "pay_1", null));
        Assert.Equal(PaymentOutcome.Ignored, outcome);
    }

    // ---- confirm and webhook entry points

    [Fact]
    public async Task Confirm_WithABadSignature_IsRefused_AndNothingIsApplied()
    {
        var (service, attempt) = await Started();
        _provider.SignatureOk = false;

        var ex = await Assert.ThrowsAsync<PaymentRequestException>(() => service.ConfirmAsync(_subscription.Id, attempt.ProviderOrderId, "pay_1", "bad"));

        Assert.Equal("bad_signature", ex.Code);
        Assert.Equal(PaymentAttemptStatus.Created, attempt.Status);
        Assert.Empty(_payments.Receipts);
    }

    [Fact]
    public async Task Confirm_ForAnotherSubscriptionsOrder_IsNotFound()
    {
        var (service, attempt) = await Started();
        var ex = await Assert.ThrowsAsync<PaymentRequestException>(() => service.ConfirmAsync(Guid.NewGuid(), attempt.ProviderOrderId, "pay_1", "sig"));
        Assert.Equal("not_found", ex.Code);
    }

    [Fact]
    public async Task Webhook_WithABadSignature_IsRefused()
    {
        _provider.NextWebhook = new WebhookParseResult(false, null);
        var ex = await Assert.ThrowsAsync<PaymentRequestException>(() => Service().HandleWebhookAsync("{}", new Dictionary<string, string>()));
        Assert.Equal("bad_signature", ex.Code);
        Assert.Empty(_payments.Receipts);
    }

    [Fact]
    public async Task Webhook_GenuineButNothingToApply_IsIgnored() =>
        Assert.Equal(PaymentOutcome.Ignored, await Service().HandleWebhookAsync("{}", new Dictionary<string, string>()));

    [Fact]
    public async Task Webhook_WithNoProvider_IsRefusedAsNoProvider()
    {
        var ex = await Assert.ThrowsAsync<PaymentRequestException>(() => Service(providerOn: false).HandleWebhookAsync("{}", new Dictionary<string, string>()));
        Assert.Equal("no_provider", ex.Code);
    }

    // ---- refunds

    private async Task<(PaymentService service, PaymentAttempt attempt)> Paid()
    {
        var (service, attempt) = await Started();
        await service.ApplyAsync("Fake", Succeeded(attempt.ProviderOrderId));
        return (service, attempt);
    }

    [Fact]
    public async Task Refund_OfAPartAndThenTheRest_MovesThePaymentThroughPartlyRefundedToRefunded()
    {
        var (service, attempt) = await Paid();
        var admin = Guid.NewGuid();

        var first = await service.RefundAsync(attempt.Id, 100m, "goodwill", admin, default);
        Assert.Equal(PaymentAttemptStatus.PartlyRefunded, attempt.Status);
        Assert.Equal(399m, attempt.RefundableAmount);
        Assert.Equal(admin, first.RequestedByUserId);
        Assert.Equal("rfnd_1", first.ProviderRefundId);

        await service.RefundAsync(attempt.Id, null, "the rest", admin, default);
        Assert.Equal(PaymentAttemptStatus.Refunded, attempt.Status);
        Assert.Equal(0m, attempt.RefundableAmount);
        Assert.Equal(new[] { 100m, 399m }, _provider.Refunds.Select(r => r.amount.Amount));
    }

    [Fact]
    public async Task Refund_ForMoreThanIsLeft_IsRefused_AndNothingIsSentToTheProvider()
    {
        var (service, attempt) = await Paid();
        var ex = await Assert.ThrowsAsync<PaymentRequestException>(() => service.RefundAsync(attempt.Id, 500m, "too much", Guid.NewGuid(), default));
        Assert.Equal("too_much", ex.Code);
        Assert.Empty(_provider.Refunds);
    }

    [Fact]
    public async Task Refund_OfAPaymentThatWasNotPaid_OrWithoutAReason_IsRefused()
    {
        var (service, attempt) = await Started();
        Assert.Equal("not_paid", (await Assert.ThrowsAsync<PaymentRequestException>(() => service.RefundAsync(attempt.Id, 1m, "x", Guid.NewGuid(), default))).Code);

        var (service2, paid) = await PaidInNewFixture();
        Assert.Equal("reason_required", (await Assert.ThrowsAsync<PaymentRequestException>(() => service2.RefundAsync(paid.Id, 1m, " ", Guid.NewGuid(), default))).Code);
    }

    private async Task<(PaymentService, PaymentAttempt)> PaidInNewFixture()
    {
        var service = Service();
        await service.StartCheckoutAsync(_subscription.Id, default);
        var attempt = _payments.Attempts.Last();
        await service.ApplyAsync("Fake", Succeeded(attempt.ProviderOrderId, "evt-new"));
        return (service, attempt);
    }

    [Fact]
    public async Task Refund_WhenTheProviderRefuses_RecordsNothing()
    {
        var (service, attempt) = await Paid();
        _provider.RefundSucceeds = false;

        var ex = await Assert.ThrowsAsync<PaymentRequestException>(() => service.RefundAsync(attempt.Id, 10m, "x", Guid.NewGuid(), default));

        Assert.Equal("provider_refused", ex.Code);
        Assert.Empty(attempt.Refunds);
        Assert.Equal(PaymentAttemptStatus.Paid, attempt.Status);
    }

    [Fact]
    public async Task Refund_WithNoProvider_IsRefused()
    {
        var ex = await Assert.ThrowsAsync<PaymentRequestException>(() => Service(providerOn: false).RefundAsync(Guid.NewGuid(), 1m, "x", Guid.NewGuid(), default));
        Assert.Equal("no_provider", ex.Code);
    }
}

public class PaymentAttemptTests
{
    [Fact]
    public void PaidAndFailedAreFinal()
    {
        var failed = new PaymentAttempt();
        failed.MarkFailed("x");
        Assert.Throws<InvalidOperationException>(() => failed.MarkPaid("p", Guid.NewGuid()));

        var paid = new PaymentAttempt();
        paid.MarkPaid("p", Guid.NewGuid());
        paid.MarkFailed("late");
        Assert.Equal(PaymentAttemptStatus.Paid, paid.Status);
    }

    [Fact]
    public void ARefund_CannotExceedWhatIsLeft()
    {
        var attempt = new PaymentAttempt { Amount = 100m };
        attempt.MarkPaid("p", Guid.NewGuid());
        attempt.ApplyRefund(new Refund { Amount = 60m });
        Assert.Throws<InvalidOperationException>(() => attempt.ApplyRefund(new Refund { Amount = 50m }));
        attempt.ApplyRefund(new Refund { Amount = 40m });
        Assert.Equal(PaymentAttemptStatus.Refunded, attempt.Status);
    }

    [Fact]
    public void AnUnpaidAttempt_CannotBeRefunded() =>
        Assert.Throws<InvalidOperationException>(() => new PaymentAttempt { Amount = 10m }.ApplyRefund(new Refund { Amount = 1m }));
}

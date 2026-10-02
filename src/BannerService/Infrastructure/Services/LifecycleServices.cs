namespace BannerService.Infrastructure.Services;

using Domain.Entities;
using Domain.Interfaces;

/// <summary>
/// Text messages are written to the log until an SMS provider is connected (Sms:Provider). That keeps the renewal
/// reminders testable without credentials, but nothing reaches a phone.
/// </summary>
public class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(string phoneNumber, string message)
    {
        var masked = phoneNumber.Length > 4 ? new string('*', phoneNumber.Length - 4) + phoneNumber[^4..] : "****";
        _logger.LogWarning("SMS provider not configured: message to {Phone} was not sent ({Length} characters)", masked, message.Length);
        return Task.FromResult(false);
    }
}

/// <summary>
/// Stands in until a payment provider is connected: every charge is reported as paid so renewals run end to end.
/// With Payments:Provider=None nobody is actually charged.
/// </summary>
public class NoPaymentGateway : IPaymentGateway
{
    private readonly ILogger<NoPaymentGateway> _logger;

    public NoPaymentGateway(ILogger<NoPaymentGateway> logger)
    {
        _logger = logger;
    }

    public Task<PaymentResult> ChargeAsync(Subscription subscription, decimal amount)
    {
        _logger.LogWarning("No payment provider configured: renewal of subscription {Id} ({Amount}) was not charged", subscription.Id, amount);
        return Task.FromResult(new PaymentResult { Success = true, Reference = "no-payment-provider" });
    }
}

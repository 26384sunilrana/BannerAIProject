namespace BannerService.Infrastructure.Monitoring;

using Prometheus;

/// <summary>Counters on /metrics for payments and messages. Labels never carry personal data.</summary>
public static class PaymentMetrics
{
    public static readonly Counter CheckoutsStarted = Metrics.CreateCounter("bannerai_checkouts_started_total", "Payment checkouts started.", "provider");
    public static readonly Counter PaymentsSucceeded = Metrics.CreateCounter("bannerai_payments_succeeded_total", "Payments applied.", "provider");
    public static readonly Counter PaymentsFailed = Metrics.CreateCounter("bannerai_payments_failed_total", "Payments reported failed.", "provider");
    public static readonly Counter PaymentsRejected = Metrics.CreateCounter("bannerai_payments_rejected_total", "Payment reports refused (bad signature, amount mismatch).", "reason");
    public static readonly Counter Refunds = Metrics.CreateCounter("bannerai_refunds_total", "Refunds made.", "provider");
    public static readonly Counter SmsSent = Metrics.CreateCounter("bannerai_sms_total", "Text messages by result.", "result");
}

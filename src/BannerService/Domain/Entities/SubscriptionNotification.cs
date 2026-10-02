namespace BannerService.Domain.Entities;

public enum NotificationChannel
{
    Email = 1,
    Sms = 2
}

/// <summary>
/// One message sent to a shop owner about their subscription. The unique (subscription, kind, channel, day)
/// combination is what stops a reminder going out twice in a day, even when several servers run the job.
/// </summary>
public class SubscriptionNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubscriptionId { get; set; }

    /// <summary>For example Reminder7, Reminder3, Reminder1, GraceDaily, Expired, AutoRenewed, PaymentFailed.</summary>
    public string Kind { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }

    /// <summary>The calendar day (UTC) the message belongs to.</summary>
    public DateTime Day { get; set; }

    public bool Delivered { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

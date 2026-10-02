namespace BannerService.Application.Services;

using Domain.Entities;
using Domain.Interfaces;

public class LifecycleReport
{
    public int PlanChangesApplied { get; set; }
    public int AutoRenewed { get; set; }
    public int RenewalsFailed { get; set; }
    public int EnteredGrace { get; set; }
    public int Expired { get; set; }
    public int MessagesSent { get; set; }
}

/// <summary>
/// The yearly rhythm of a subscription, run every hour by a background job (and safe to run more often):
///   • auto renewal on: charge on the renewal date;
///   • auto renewal off: remind by email and text 7, 3 and 1 days before;
///   • renewal date passed unpaid: a week of grace with a daily reminder, the shop shows only its default banner;
///   • grace over: the subscription expires, logins are switched off and sessions end.
/// Every step can be repeated without effect: states only move forward and each message is sent once a day.
/// </summary>
public class SubscriptionLifecycleService
{
    public static readonly int[] ReminderDaysBefore = { 7, 3, 1 };

    private readonly ISubscriptionRepository _subscriptions;
    private readonly IInvoiceRepository _invoices;
    private readonly IShopRepository _shops;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IEmailService _email;
    private readonly ISmsSender _sms;
    private readonly IPaymentGateway _payments;
    private readonly ISubscriptionNotificationRepository _notifications;
    private readonly SubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionLifecycleService> _logger;

    public SubscriptionLifecycleService(
        ISubscriptionRepository subscriptions,
        IInvoiceRepository invoices,
        IShopRepository shops,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IEmailService email,
        ISmsSender sms,
        IPaymentGateway payments,
        ISubscriptionNotificationRepository notifications,
        SubscriptionService subscriptionService,
        ILogger<SubscriptionLifecycleService> logger)
    {
        _subscriptions = subscriptions;
        _invoices = invoices;
        _shops = shops;
        _users = users;
        _refreshTokens = refreshTokens;
        _email = email;
        _sms = sms;
        _payments = payments;
        _notifications = notifications;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<LifecycleReport> RunAsync(DateTime nowUtc)
    {
        var report = new LifecycleReport();

        report.PlanChangesApplied = await _subscriptionService.ApplyDuePlanChangesAsync();

        // Active subscriptions: renew, or remind
        var running = new List<Subscription>();
        foreach (var status in new[] { SubscriptionStatus.Active, SubscriptionStatus.Trial, SubscriptionStatus.RenewalPending, SubscriptionStatus.PaymentFailed })
            running.AddRange(await _subscriptions.GetByStatusAsync(status));

        foreach (var subscription in running)
        {
            await Safely(subscription, () => ProcessRunningAsync(subscription, nowUtc, report));
        }

        // Shops in their grace week: remind daily, or expire
        foreach (var subscription in await _subscriptions.GetByStatusAsync(SubscriptionStatus.GracePeriod))
        {
            await Safely(subscription, () => ProcessGraceAsync(subscription, nowUtc, report));
        }

        return report;
    }

    // One broken subscription must not stop the others from being processed
    private async Task Safely(Subscription subscription, Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscription lifecycle step failed for {SubscriptionId}", subscription.Id);
        }
    }

    /// <summary>
    /// "Renew now": pays for another period at once. Renewing early adds to the current period; renewing
    /// after the date has passed starts a new period today. This is also how an admin reactivates an expired shop.
    /// </summary>
    public async Task<(bool success, string message)> RenewNowAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId);
        if (subscription == null)
            return (false, "Subscription not found");
        if (subscription.Status == SubscriptionStatus.Cancelled)
            return (false, "A cancelled subscription cannot be renewed; choose a plan instead");

        var charge = await _payments.ChargeAsync(subscription, subscription.CurrentPrice);
        if (!charge.Success)
        {
            subscription.RecordPaymentAttempt();
            await _subscriptions.UpdateAsync(subscription);
            return (false, charge.Error ?? "The payment was not accepted");
        }

        var now = DateTime.UtcNow;
        var from = subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.Trial && subscription.RenewalDate > now
            ? subscription.RenewalDate
            : now;

        await RenewAsync(subscription, from, charge.Reference);
        await NotifyAsync(subscription, "Renewed", now, RenewalMessages.Renewed(subscription));
        return (true, "Subscription renewed successfully");
    }

    private async Task ProcessRunningAsync(Subscription subscription, DateTime now, LifecycleReport report)
    {
        if (subscription.RenewalDate > now)
        {
            if (!subscription.AutoRenew)
            {
                var daysLeft = (subscription.RenewalDate.Date - now.Date).Days;
                if (ReminderDaysBefore.Contains(daysLeft))
                    report.MessagesSent += await NotifyAsync(subscription, $"Reminder{daysLeft}", now,
                        RenewalMessages.Reminder(subscription, daysLeft));
            }
            return;
        }

        // The renewal date has arrived
        if (subscription.AutoRenew)
        {
            var charge = await _payments.ChargeAsync(subscription, subscription.CurrentPrice);
            if (charge.Success)
            {
                await RenewAsync(subscription, subscription.RenewalDate, charge.Reference);
                report.AutoRenewed++;
                report.MessagesSent += await NotifyAsync(subscription, "AutoRenewed", now, RenewalMessages.Renewed(subscription));
                return;
            }

            subscription.RecordPaymentAttempt();
            report.RenewalsFailed++;
            report.MessagesSent += await NotifyAsync(subscription, "PaymentFailed", now, RenewalMessages.PaymentFailed(subscription));
        }

        subscription.EnterGracePeriod();
        await _subscriptions.UpdateAsync(subscription);
        report.EnteredGrace++;
        report.MessagesSent += await NotifyAsync(subscription, "GraceDaily", now, RenewalMessages.Grace(subscription, now));
    }

    private async Task ProcessGraceAsync(Subscription subscription, DateTime now, LifecycleReport report)
    {
        subscription.GraceEndsAt ??= subscription.RenewalDate.AddDays(Subscription.GraceDays);

        // With auto renewal on, the charge is tried again once a day during the grace week
        if (subscription.AutoRenew && now < subscription.GraceEndsAt &&
            await _notifications.TryClaimAsync(new SubscriptionNotification
            {
                SubscriptionId = subscription.Id,
                Kind = "ChargeRetry",
                Channel = NotificationChannel.Email,
                Day = now.Date
            }))
        {
            var retry = await _payments.ChargeAsync(subscription, subscription.CurrentPrice);
            if (retry.Success)
            {
                await RenewAsync(subscription, now, retry.Reference);
                report.AutoRenewed++;
                report.MessagesSent += await NotifyAsync(subscription, "AutoRenewed", now, RenewalMessages.Renewed(subscription));
                return;
            }
        }

        if (now >= subscription.GraceEndsAt)
        {
            subscription.Expire();
            await _subscriptions.UpdateAsync(subscription);
            await EndSessionsAsync(subscription);
            report.Expired++;
            report.MessagesSent += await NotifyAsync(subscription, "Expired", now, RenewalMessages.Expired(subscription));
            return;
        }

        report.MessagesSent += await NotifyAsync(subscription, "GraceDaily", now, RenewalMessages.Grace(subscription, now));
    }

    private async Task RenewAsync(Subscription subscription, DateTime from, string? paymentReference)
    {
        subscription.RenewFrom(from);
        await _subscriptions.UpdateAsync(subscription);

        await _invoices.CreateAsync(new Invoice
        {
            SubscriptionId = subscription.Id,
            ShopId = subscription.ShopId,
            Amount = subscription.CurrentPrice,
            Status = InvoiceStatus.Paid,
            InvoiceNumber = await _invoices.GenerateInvoiceNumberAsync(),
            IssuedDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow,
            PaidDate = DateTime.UtcNow,
            PaymentReference = paymentReference,
            Description = $"Automatic renewal - {subscription.BillingPeriod}",
        });
    }

    /// <summary>Ends every session of the shop's logins (admins excepted) so the lock-out takes effect at the next refresh.</summary>
    private async Task EndSessionsAsync(Subscription subscription)
    {
        var shopUsers = await _users.GetByShopIdAsync(subscription.ShopId.ToString());
        foreach (var user in shopUsers.Where(u => !u.UserRoles.Any(r => r.Role?.Name == Role.Admin)))
        {
            foreach (var token in await _refreshTokens.GetActiveByUserIdAsync(user.Id))
            {
                token.Revoke();
                await _refreshTokens.UpdateAsync(token);
            }
        }
    }

    /// <summary>Sends the message by every channel we have an address for, at most once per kind and day.</summary>
    private async Task<int> NotifyAsync(Subscription subscription, string kind, DateTime now, (string subject, string text) message)
    {
        var shop = await _shops.GetByIdAsync(subscription.ShopId);
        if (shop == null)
            return 0;

        var sent = 0;

        var owner = shop.OwnerUserId.HasValue ? await _users.GetByIdAsync(shop.OwnerUserId.Value.ToString()) : null;
        if (!string.IsNullOrWhiteSpace(owner?.Email))
        {
            sent += await SendOnceAsync(subscription, kind, NotificationChannel.Email, now,
                () => _email.SendEmailAsync(owner!.Email, message.subject, message.text));
        }

        if (!string.IsNullOrWhiteSpace(shop.PhoneNumber))
        {
            sent += await SendOnceAsync(subscription, kind, NotificationChannel.Sms, now,
                () => _sms.SendAsync(shop.PhoneNumber!, $"{message.subject}: {message.text}"));
        }

        return sent;
    }

    private async Task<int> SendOnceAsync(Subscription subscription, string kind, NotificationChannel channel, DateTime now, Func<Task<bool>> send)
    {
        var record = new SubscriptionNotification { SubscriptionId = subscription.Id, Kind = kind, Channel = channel, Day = now.Date };
        if (!await _notifications.TryClaimAsync(record))
            return 0;

        try
        {
            var delivered = await send();
            await _notifications.MarkResultAsync(record.Id, delivered, delivered ? null : "Not delivered");
            return delivered ? 1 : 0;
        }
        catch (Exception ex)
        {
            await _notifications.MarkResultAsync(record.Id, false, ex.Message);
            return 0;
        }
    }
}

public static class RenewalMessages
{
    public static (string subject, string text) Reminder(Subscription s, int daysLeft) =>
        ($"Your Banner AI plan renews in {daysLeft} day{(daysLeft == 1 ? "" : "s")}",
         $"Your {s.Plan?.Name ?? "Banner AI"} plan ends on {s.RenewalDate:d MMM yyyy} and will not renew automatically. " +
         "Renew now so your banners keep playing.");

    public static (string subject, string text) Renewed(Subscription s) =>
        ("Your Banner AI plan was renewed",
         $"Your {s.Plan?.Name ?? "Banner AI"} plan was renewed. Next renewal: {s.RenewalDate:d MMM yyyy}.");

    public static (string subject, string text) PaymentFailed(Subscription s) =>
        ("Payment failed for your Banner AI plan",
         "We could not take the payment for your plan renewal. Please renew now to keep your banners playing.");

    public static (string subject, string text) Grace(Subscription s, DateTime now)
    {
        var endsAt = s.GraceEndsAt ?? s.RenewalDate.AddDays(Subscription.GraceDays);
        var daysLeft = Math.Max(0, (endsAt.Date - now.Date).Days);
        return ("Renew your Banner AI plan",
            $"Your plan ended on {s.RenewalDate:d MMM yyyy}. Until you renew your shop shows only its default banner. " +
            $"Logins are switched off in {daysLeft} day{(daysLeft == 1 ? "" : "s")} (on {endsAt:d MMM yyyy}).");
    }

    public static (string subject, string text) Expired(Subscription s) =>
        ("Your Banner AI logins have been switched off",
         "Your plan was not renewed in time. Logins are switched off and your shop shows only its default banner. " +
         "Contact us to reactivate your account.");
}

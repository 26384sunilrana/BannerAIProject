namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;
using Data;

public class SubscriptionNotificationRepository : ISubscriptionNotificationRepository
{
    private readonly ApplicationDbContext _context;

    public SubscriptionNotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryClaimAsync(SubscriptionNotification notification)
    {
        notification.Day = notification.Day.Date;

        // Cheap check first; the unique index decides when two servers race
        var already = await _context.SubscriptionNotifications.AnyAsync(n =>
            n.SubscriptionId == notification.SubscriptionId &&
            n.Kind == notification.Kind &&
            n.Channel == notification.Channel &&
            n.Day == notification.Day);
        if (already)
            return false;

        _context.SubscriptionNotifications.Add(notification);
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(notification).State = EntityState.Detached;
            return false;
        }
    }

    public async Task MarkResultAsync(Guid notificationId, bool delivered, string? error)
    {
        var notification = await _context.SubscriptionNotifications.FirstOrDefaultAsync(n => n.Id == notificationId);
        if (notification == null)
            return;

        notification.Delivered = delivered;
        notification.Error = error?.Length > 500 ? error[..500] : error;
        await _context.SaveChangesAsync();
    }
}

namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;
using Data;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IReadOnlyCollection<InAppNotification> notifications)
    {
        if (notifications.Count == 0) return;
        _context.InAppNotifications.AddRange(notifications);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<InAppNotification> Items, int Total)> ListAsync(string userId, bool unreadOnly, int page, int pageSize)
    {
        var query = _context.InAppNotifications.AsNoTracking().Where(n => n.UserId == userId && (!unreadOnly || n.ReadAt == null));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(n => n.CreatedAt).ThenBy(n => n.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<int> CountUnreadAsync(string userId) =>
        _context.InAppNotifications.CountAsync(n => n.UserId == userId && n.ReadAt == null);

    public async Task<bool> MarkReadAsync(string userId, Guid notificationId)
    {
        var item = await _context.InAppNotifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (item == null) return false;
        if (item.ReadAt == null)
        {
            item.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<int> MarkAllReadAsync(string userId)
    {
        var unread = await _context.InAppNotifications.Where(n => n.UserId == userId && n.ReadAt == null).ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var item in unread) item.ReadAt = now;
        await _context.SaveChangesAsync();
        return unread.Count;
    }

    public async Task<List<string>> FindUserIdsByRoleAsync(string roleName, Guid? shopId)
    {
        var query = _context.UserRoles.Where(ur => ur.Role!.Name == roleName && ur.User!.IsActive);
        if (shopId != null)
        {
            var shop = shopId.Value.ToString();
            query = query.Where(ur => ur.User!.ShopId == shop);
        }
        return await query.Select(ur => ur.UserId).Distinct().ToListAsync();
    }

    public async Task<int> PurgeAsync(DateTime readBefore, DateTime anyBefore)
    {
        var old = await _context.InAppNotifications
            .Where(n => (n.ReadAt != null && n.ReadAt < readBefore) || n.CreatedAt < anyBefore).ToListAsync();
        _context.InAppNotifications.RemoveRange(old);
        await _context.SaveChangesAsync();
        return old.Count;
    }
}

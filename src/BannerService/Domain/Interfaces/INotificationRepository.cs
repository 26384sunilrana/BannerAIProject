namespace BannerService.Domain.Interfaces;

using Entities;

public interface INotificationRepository
{
    Task AddRangeAsync(IReadOnlyCollection<InAppNotification> notifications);
    Task<(List<InAppNotification> Items, int Total)> ListAsync(string userId, bool unreadOnly, int page, int pageSize);
    Task<int> CountUnreadAsync(string userId);
    Task<bool> MarkReadAsync(string userId, Guid notificationId);
    Task<int> MarkAllReadAsync(string userId);

    /// <summary>Active users holding the role; limited to one shop when a shop is given.</summary>
    Task<List<string>> FindUserIdsByRoleAsync(string roleName, Guid? shopId);

    /// <summary>Removes read messages older than the first cut-off and any message older than the second. Returns how many.</summary>
    Task<int> PurgeAsync(DateTime readBefore, DateTime anyBefore);
}

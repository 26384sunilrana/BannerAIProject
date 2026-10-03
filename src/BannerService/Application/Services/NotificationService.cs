namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;

    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class NotificationPageDto
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Unread { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    /// <summary>Messages shown inside the application. Producers call a Notify method; a failure here never fails the action that caused it.</summary>
    public class NotificationService
    {
        public const int MaxTitle = 120;
        public const int MaxMessage = 500;

        private readonly INotificationRepository _repository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(INotificationRepository repository, ILogger<NotificationService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task NotifyUsersAsync(IEnumerable<string> userIds, string kind, string title, string message, string? linkUrl = null)
        {
            try
            {
                var recipients = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
                if (recipients.Count == 0) return;

                // only a page of this application can be linked, never another site
                var link = linkUrl != null && linkUrl.StartsWith('/') && !linkUrl.StartsWith("//") && !linkUrl.Contains('\\') ? linkUrl : null;
                var now = DateTime.UtcNow;
                await _repository.AddRangeAsync(recipients.Select(id => new InAppNotification
                {
                    UserId = id,
                    Kind = Clip(kind, 40),
                    Title = Clip(title, MaxTitle),
                    Message = Clip(message, MaxMessage),
                    LinkUrl = link,
                    CreatedAt = now,
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not store notification {Kind}", kind);
            }
        }

        public async Task NotifyAdminsAsync(string kind, string title, string message, string? linkUrl = null) =>
            await NotifyUsersAsync(await FindAsync(Role.Admin, null), kind, title, message, linkUrl);

        public async Task NotifyShopOwnersAsync(Guid shopId, string kind, string title, string message, string? linkUrl = null) =>
            await NotifyUsersAsync(await FindAsync(Role.ShopOwner, shopId), kind, title, message, linkUrl);

        private async Task<List<string>> FindAsync(string role, Guid? shopId)
        {
            try { return await _repository.FindUserIdsByRoleAsync(role, shopId); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not find recipients for role {Role}", role);
                return new List<string>();
            }
        }

        public async Task<NotificationPageDto> ListAsync(string userId, bool unreadOnly, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var (items, total) = await _repository.ListAsync(userId, unreadOnly, page, pageSize);
            return new NotificationPageDto
            {
                Items = items.Select(n => new NotificationDto
                {
                    Id = n.Id, Kind = n.Kind, Title = n.Title, Message = n.Message, LinkUrl = n.LinkUrl,
                    CreatedAt = DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc), IsRead = n.ReadAt != null,
                }).ToList(),
                Total = total,
                Unread = await _repository.CountUnreadAsync(userId),
                Page = page,
                PageSize = pageSize,
            };
        }

        public Task<int> CountUnreadAsync(string userId) => _repository.CountUnreadAsync(userId);

        public async Task<object?> MarkReadAsync(string userId, Guid id)
        {
            if (!await _repository.MarkReadAsync(userId, id)) throw new KeyNotFoundException("Notification not found.");
            return new { unread = await _repository.CountUnreadAsync(userId) };
        }

        public async Task<object> MarkAllReadAsync(string userId) => new { marked = await _repository.MarkAllReadAsync(userId), unread = 0 };

        /// <summary>Read messages go after 30 days, unread ones after 90.</summary>
        public Task<int> PurgeAsync(DateTime now) => _repository.PurgeAsync(now.AddDays(-30), now.AddDays(-90));

        private static string Clip(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
    }
}

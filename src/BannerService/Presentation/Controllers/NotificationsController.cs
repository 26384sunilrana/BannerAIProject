namespace BannerService.Presentation.Controllers;

using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>The signed-in user's own in-application messages.</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notifications;

    public NotificationsController(NotificationService notifications)
    {
        _notifications = notifications;
    }

    private string UserId => User.GetUserId().ToString();

    [HttpGet]
    public Task<IActionResult> List([FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        ServiceErrors.Run(this, () => _notifications.ListAsync(UserId, unreadOnly, page, pageSize));

    [HttpGet("unread-count")]
    public async Task<IActionResult> Unread() => Ok(new { unread = await _notifications.CountUnreadAsync(UserId) });

    [HttpPost("{id:guid}/read")]
    public Task<IActionResult> MarkRead(Guid id) => ServiceErrors.Run(this, () => _notifications.MarkReadAsync(UserId, id));

    [HttpPost("read-all")]
    public Task<IActionResult> MarkAllRead() => ServiceErrors.Run(this, () => _notifications.MarkAllReadAsync(UserId));
}

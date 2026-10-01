namespace BannerService.Presentation.Controllers;

using System.Security.Claims;
using Application.Services;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>User and audit administration for the product owner.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly UserAdminService _users;
    private readonly IAuditLogRepository _audit;

    public AdminUsersController(UserAdminService users, IAuditLogRepository audit)
    {
        _users = users;
        _audit = audit;
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search, [FromQuery] string? shopId, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        Ok(await _users.ListAsync(search, shopId, includeInactive, page, pageSize));

    [HttpGet("users/{userId}")]
    public Task<IActionResult> GetUser(string userId) => Run(() => _users.GetAsync(userId));

    [HttpPost("users/{userId}/deactivate")]
    public Task<IActionResult> Deactivate(string userId) =>
        Run(() => _users.DeactivateAsync(userId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty));

    [HttpPost("users/{userId}/activate")]
    public Task<IActionResult> Activate(string userId) => Run(() => _users.ActivateAsync(userId));

    [HttpPost("users/{userId}/unlock")]
    public Task<IActionResult> Unlock(string userId) => Run(() => _users.UnlockAsync(userId));

    /// <summary>Who accessed what, newest first. Filter by user, shop, time range, or failures only (minStatusCode=400).</summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? userId, [FromQuery] Guid? shopId,
        [FromQuery] int? minStatusCode, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var (items, total) = await _audit.QueryAsync(from, to, userId, shopId, minStatusCode, page, pageSize);
        return Ok(new { items, total, page, pageSize });
    }

    private async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

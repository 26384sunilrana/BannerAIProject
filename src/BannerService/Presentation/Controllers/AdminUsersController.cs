namespace BannerService.Presentation.Controllers;

using System.Security.Claims;
using Application.Services;
using Domain.Entities;
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
    private readonly ISubscriptionRepository _subscriptions;
    private readonly TwoFactorService _twoFactor;

    public AdminUsersController(UserAdminService users, IAuditLogRepository audit, ISubscriptionRepository subscriptions, TwoFactorService twoFactor)
    {
        _twoFactor = twoFactor;
        _users = users;
        _audit = audit;
        _subscriptions = subscriptions;
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search, [FromQuery] string? shopId, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        Ok(await _users.ListAsync(search, shopId, includeInactive, page, pageSize));

    [HttpGet("users/{userId}")]
    public Task<IActionResult> GetUser(string userId) => Run(() => _users.GetAsync(userId));

    /// <summary>Create a new global administrator account.</summary>
    [HttpPost("users/create-admin")]
    public Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto request) =>
        Run(() => _users.CreateAdminAsync(request));

    /// <summary>List all global administrators (Super Admin and Admins).</summary>
    [HttpGet("admins")]
    public Task<IActionResult> GetAllAdmins() => Run(() => _users.GetAllAdminsAsync());

    /// <summary>Request deletion of a global administrator (requires Super Admin approval).</summary>
    [HttpPost("admins/{adminId}/request-deletion")]
    public Task<IActionResult> RequestAdminDeletion(string adminId, [FromBody] DeleteAdminRequestDto request) =>
        Run(() => _users.RequestAdminDeletionAsync(adminId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty, request.Reason));

    /// <summary>Get pending admin deletion requests (Super Admin only).</summary>
    [HttpGet("super-admin/deletion-requests")]
    [Authorize(Roles = "SuperAdmin")]
    public Task<IActionResult> GetPendingDeletionRequests() =>
        Run(() => _users.GetPendingDeletionRequestsAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty));

    /// <summary>Approve an admin deletion request (Super Admin only).</summary>
    [HttpPost("super-admin/deletion-requests/{requestId}/approve")]
    [Authorize(Roles = "SuperAdmin")]
    public Task<IActionResult> ApproveDeletion(string requestId) =>
        Run(() => _users.ApproveDeletionAsync(requestId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty));

    /// <summary>Reject an admin deletion request (Super Admin only).</summary>
    [HttpPost("super-admin/deletion-requests/{requestId}/reject")]
    [Authorize(Roles = "SuperAdmin")]
    public Task<IActionResult> RejectDeletion(string requestId) =>
        Run(() => _users.RejectDeletionAsync(requestId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty));

    [HttpPost("users/{userId}/deactivate")]
    public Task<IActionResult> Deactivate(string userId) =>
        Run(() => _users.DeactivateAsync(userId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty));

    [HttpPost("users/{userId}/activate")]
    public Task<IActionResult> Activate(string userId) => Run(() => _users.ActivateAsync(userId));

    /// <summary>For someone who lost their phone and their recovery codes: switches their two-step sign-in off so they can set it up again.</summary>
    [HttpPost("users/{userId}/reset-two-factor")]
    public Task<IActionResult> ResetTwoFactor(string userId) => Run(async () =>
    {
        await _users.EnsureNotOwnerAsync(userId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
        await _twoFactor.ResetForUserAsync(userId);
        return new { message = "Two-step sign-in was switched off for this person." };
    });

    [HttpPost("users/{userId}/unlock")]
    public Task<IActionResult> Unlock(string userId) => Run(() => _users.UnlockAsync(userId));

    /// <summary>Every shop's subscription, soonest renewal first. Filter by status or shop name.</summary>
    [HttpGet("subscriptions")]
    public async Task<IActionResult> Subscriptions(
        [FromQuery] int? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _subscriptions.GetPagedAsync(
            status.HasValue ? (SubscriptionStatus)status.Value : null, search, page, pageSize);

        return Ok(new
        {
            items = items.Select(s => new
            {
                id = s.Id,
                shopId = s.ShopId,
                shopName = s.Shop?.Name ?? "Unknown shop",
                planName = s.Plan?.Name ?? "Unknown plan",
                status = (int)s.Status,
                billingPeriod = (int)s.BillingPeriod,
                currentPrice = s.CurrentPrice,
                renewalDate = s.RenewalDate,
                autoRenew = s.AutoRenew,
                graceEndsAt = s.GraceEndsAt
            }),
            total,
            page,
            pageSize
        });
    }

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

    /// <summary>
    /// The same entries as a CSV file, up to 100,000 rows, for keeping or for an investigation. The export is itself an API call and
    /// so is written to the audit log. Cells that a spreadsheet could run as a formula are defused.
    /// </summary>
    [HttpGet("audit-logs/export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? userId, [FromQuery] Guid? shopId, [FromQuery] int? minStatusCode)
    {
        var (items, total) = await _audit.QueryAsync(from, to, userId, shopId, minStatusCode, 1, 100_000);
        var csv = AuditCsv.Build(items);
        Response.Headers["X-Total-Entries"] = total.ToString();
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", $"audit-log-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}

/// <summary>Writes audit entries as CSV.</summary>
public static class AuditCsv
{
    public static string Build(IEnumerable<Domain.Entities.AuditLog> entries)
    {
        var text = new System.Text.StringBuilder();
        text.AppendLine("OccurredAtUtc,UserId,UserEmail,ShopId,Method,Path,StatusCode,IpAddress,DurationMs");
        foreach (var e in entries)
        {
            text.AppendJoin(',', new[]
            {
                Cell(e.OccurredAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")), Cell(e.UserId), Cell(e.UserEmail), Cell(e.ShopId?.ToString()), Cell(e.Method),
                Cell(e.Path), Cell(e.StatusCode.ToString()), Cell(e.IpAddress), Cell(e.DurationMs.ToString()),
            });
            text.AppendLine();
        }
        return text.ToString();
    }

    /// <summary>Quotes a cell, and puts a quote mark in front of one a spreadsheet would read as a formula.</summary>
    public static string Cell(string? value)
    {
        value ??= string.Empty;
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r') value = "'" + value;
        return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
}

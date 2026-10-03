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

    public AdminUsersController(UserAdminService users, IAuditLogRepository audit, ISubscriptionRepository subscriptions)
    {
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

    [HttpPost("users/{userId}/deactivate")]
    public Task<IActionResult> Deactivate(string userId) =>
        Run(() => _users.DeactivateAsync(userId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty));

    [HttpPost("users/{userId}/activate")]
    public Task<IActionResult> Activate(string userId) => Run(() => _users.ActivateAsync(userId));

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

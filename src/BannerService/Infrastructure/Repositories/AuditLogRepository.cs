namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;
using Data;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> PurgeAsync(DateTime olderThanUtc) =>
        await _context.AuditLogs.Where(a => a.OccurredAt < olderThanUtc).ExecuteDeleteAsync();

    public async Task AddAsync(AuditLog entry)
    {
        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<AuditLog> items, int total)> QueryAsync(
        DateTime? from, DateTime? to, string? userId, Guid? shopId, int? minStatusCode, int page, int pageSize)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (from.HasValue) query = query.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.OccurredAt < to.Value);
        if (!string.IsNullOrEmpty(userId)) query = query.Where(a => a.UserId == userId);
        if (shopId.HasValue) query = query.Where(a => a.ShopId == shopId.Value);
        if (minStatusCode.HasValue) query = query.Where(a => a.StatusCode >= minStatusCode.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((Math.Max(page, 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}

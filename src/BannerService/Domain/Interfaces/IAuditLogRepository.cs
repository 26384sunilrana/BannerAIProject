namespace BannerService.Domain.Interfaces;

using Entities;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry);

    Task<(List<AuditLog> items, int total)> QueryAsync(
        DateTime? from, DateTime? to, string? userId, Guid? shopId, int? minStatusCode, int page, int pageSize);

    /// <summary>Removes entries older than the cut-off. Returns how many were removed.</summary>
    Task<int> PurgeAsync(DateTime olderThanUtc);
}

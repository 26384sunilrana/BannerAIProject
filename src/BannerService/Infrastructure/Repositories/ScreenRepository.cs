namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;
using Data;

public class ScreenRepository : IScreenRepository
{
    private readonly ApplicationDbContext _context;

    public ScreenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Screen?> GetByIdAsync(Guid id) => _context.Screens.FirstOrDefaultAsync(s => s.Id == id);

    public Task<Screen?> FindBySecretHashAsync(string hash) => _context.Screens.FirstOrDefaultAsync(s => s.DeviceSecretHash == hash);

    public Task<List<Screen>> ListForShopAsync(Guid shopId) =>
        _context.Screens.AsNoTracking().Where(s => s.ShopId == shopId).OrderBy(s => s.CreatedAt).ToListAsync();

    public Task<int> CountActiveForShopAsync(Guid shopId) =>
        _context.Screens.CountAsync(s => s.ShopId == shopId && s.Status == ScreenStatus.Active);

    public Task<List<Screen>> ListActiveAsync() => _context.Screens.Where(s => s.Status == ScreenStatus.Active).ToListAsync();

    public async Task AddAsync(Screen screen)
    {
        _context.Screens.Add(screen);
        await _context.SaveChangesAsync();
    }

    public Task SaveAsync() => _context.SaveChangesAsync();

    public async Task AddPairingAsync(ScreenPairing pairing)
    {
        _context.ScreenPairings.Add(pairing);
        await _context.SaveChangesAsync();
    }

    public Task<ScreenPairing?> FindPairingByCodeAsync(string code, DateTime nowUtc) =>
        _context.ScreenPairings.FirstOrDefaultAsync(p => p.Code == code && p.ScreenId == null && p.ExpiresAt > nowUtc);

    public Task<ScreenPairing?> FindPairingBySecretHashAsync(string hash) => _context.ScreenPairings.FirstOrDefaultAsync(p => p.DeviceSecretHash == hash);

    public async Task RemovePairingAsync(ScreenPairing pairing)
    {
        _context.ScreenPairings.Remove(pairing);
        await _context.SaveChangesAsync();
    }

    public async Task<int> PurgePairingsAsync(DateTime expiredBefore)
    {
        var old = await _context.ScreenPairings.Where(p => p.ExpiresAt < expiredBefore).ToListAsync();
        _context.ScreenPairings.RemoveRange(old);
        await _context.SaveChangesAsync();
        return old.Count;
    }

    public async Task AddSecondsAsync(Guid screenId, Guid shopId, DateTime dayUtc, PlayKind kind, Guid refId, string label, int seconds)
    {
        var day = dayUtc.Date;
        var row = await _context.ScreenPlayStats.FirstOrDefaultAsync(s => s.ScreenId == screenId && s.Day == day && s.Kind == kind && s.RefId == refId);
        if (row == null)
            _context.ScreenPlayStats.Add(new ScreenPlayStat { ScreenId = screenId, ShopId = shopId, Day = day, Kind = kind, RefId = refId, Label = label, Seconds = seconds });
        else
        {
            row.Seconds += seconds;
            row.Label = label;
        }
        await _context.SaveChangesAsync();
    }

    public Task<List<ScreenPlayStat>> ListStatsAsync(Guid shopId, DateTime fromDayUtc, DateTime toDayUtc) =>
        _context.ScreenPlayStats.AsNoTracking().Where(s => s.ShopId == shopId && s.Day >= fromDayUtc.Date && s.Day <= toDayUtc.Date).ToListAsync();

    public async Task<int> PurgeStatsAsync(DateTime olderThanDayUtc) =>
        await _context.ScreenPlayStats.Where(s => s.Day < olderThanDayUtc.Date).ExecuteDeleteAsync();
}

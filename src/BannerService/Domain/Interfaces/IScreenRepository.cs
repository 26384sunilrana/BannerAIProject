namespace BannerService.Domain.Interfaces;

using Entities;

public interface IScreenRepository
{
    Task<Screen?> GetByIdAsync(Guid id);
    Task<Screen?> FindBySecretHashAsync(string hash);
    Task<List<Screen>> ListForShopAsync(Guid shopId);
    Task<int> CountActiveForShopAsync(Guid shopId);

    /// <summary>Every active screen, for the watchdog and the metrics.</summary>
    Task<List<Screen>> ListActiveAsync();
    Task AddAsync(Screen screen);
    Task SaveAsync();

    Task AddPairingAsync(ScreenPairing pairing);
    Task<ScreenPairing?> FindPairingByCodeAsync(string code, DateTime nowUtc);
    Task<ScreenPairing?> FindPairingBySecretHashAsync(string hash);
    Task RemovePairingAsync(ScreenPairing pairing);

    /// <summary>Removes pairings that expired before the cut-off.</summary>
    Task<int> PurgePairingsAsync(DateTime expiredBefore);

    Task AddSecondsAsync(Guid screenId, Guid shopId, DateTime dayUtc, PlayKind kind, Guid refId, string label, int seconds);
    Task<List<ScreenPlayStat>> ListStatsAsync(Guid shopId, DateTime fromDayUtc, DateTime toDayUtc);

    /// <summary>Removes play statistics older than the cut-off. Returns how many rows.</summary>
    Task<int> PurgeStatsAsync(DateTime olderThanDayUtc);
}

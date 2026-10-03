namespace BannerService.Domain.Interfaces;

using Entities;

public interface IAdRateRepository
{
    Task<List<AdRate>> ListAsync(bool activeOnly);
    Task<AdRate?> GetByIdAsync(Guid id);

    /// <summary>Active rates that could apply to a shop in this place: every rate for everywhere, its country, its state or its city.</summary>
    Task<List<AdRate>> FindCandidatesAsync(string? countryCode, int? stateId, int? cityId);

    /// <summary>An active rate for exactly this place and kind, other than the one given.</summary>
    Task<AdRate?> FindSamePlaceAsync(AdRateLevel level, string? countryCode, int? stateId, int? cityId, ShopAdKind? kind, Guid? exceptId);

    Task AddAsync(AdRate rate);
    Task SaveAsync();
}

namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;
using Data;

public class AdRateRepository : IAdRateRepository
{
    private readonly ApplicationDbContext _context;

    public AdRateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<AdRate>> ListAsync(bool activeOnly) =>
        _context.AdRates.AsNoTracking().Where(r => !activeOnly || r.IsActive)
            .OrderBy(r => r.Level).ThenBy(r => r.CountryCode).ThenBy(r => r.StateId).ThenBy(r => r.CityId).ThenBy(r => r.Kind).ToListAsync();

    public Task<AdRate?> GetByIdAsync(Guid id) => _context.AdRates.FirstOrDefaultAsync(r => r.Id == id);

    public Task<List<AdRate>> FindCandidatesAsync(string? countryCode, int? stateId, int? cityId) =>
        _context.AdRates.AsNoTracking()
            .Where(r => r.IsActive && (r.Level == AdRateLevel.All
                                       || (r.Level == AdRateLevel.Country && r.CountryCode == countryCode)
                                       || (r.Level == AdRateLevel.State && r.StateId == stateId)
                                       || (r.Level == AdRateLevel.City && r.CityId == cityId)))
            .ToListAsync();

    public Task<AdRate?> FindSamePlaceAsync(AdRateLevel level, string? countryCode, int? stateId, int? cityId, ShopAdKind? kind, Guid? exceptId) =>
        _context.AdRates.AsNoTracking().FirstOrDefaultAsync(r => r.IsActive && r.Id != exceptId && r.Level == level && r.CountryCode == countryCode
                                                                  && r.StateId == stateId && r.CityId == cityId && r.Kind == kind);

    public async Task AddAsync(AdRate rate)
    {
        _context.AdRates.Add(rate);
        await _context.SaveChangesAsync();
    }

    public Task SaveAsync() => _context.SaveChangesAsync();
}

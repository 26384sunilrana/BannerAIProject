namespace BannerService.Infrastructure.Repositories
{
    using Data;
    using Domain.Entities;
    using Domain.Interfaces;
    using Microsoft.EntityFrameworkCore;

    public class LocationRepository : ILocationRepository
    {
        private readonly ApplicationDbContext _context;

        public LocationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LocationRow>> ListCountriesAsync(bool includeInactive)
        {
            var rows = await _context.Countries
                .Where(c => includeInactive || c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new { c.ISOCode, c.UniqueId, c.Name, c.IsActive, c.TimeZoneId, States = c.States.Count, Shops = c.Shops.Count })
                .ToListAsync();
            return rows.Select(c => new LocationRow(c.ISOCode, c.UniqueId, c.Name, c.ISOCode, null, c.IsActive, c.States, c.Shops, c.TimeZoneId)).ToList();
        }

        public async Task<List<LocationRow>> ListStatesAsync(string? countryCode, bool includeInactive)
        {
            var rows = await _context.States
                .Where(s => (includeInactive || s.IsActive) && (countryCode == null || s.CountryCode == countryCode))
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.UniqueId, s.Name, s.Code, s.CountryCode, s.IsActive, Cities = s.Cities.Count, Shops = s.Shops.Count })
                .ToListAsync();
            return rows.Select(s => new LocationRow(s.Id.ToString(), s.UniqueId, s.Name, s.Code, s.CountryCode, s.IsActive, s.Cities, s.Shops)).ToList();
        }

        public async Task<List<LocationRow>> ListCitiesAsync(int? stateId, bool includeInactive)
        {
            var rows = await _context.Cities
                .Where(c => (includeInactive || c.IsActive) && (stateId == null || c.StateId == stateId))
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.UniqueId, c.Name, c.StateId, c.IsActive, c.TimeZoneId, Groups = c.Groups.Count, Shops = c.Shops.Count })
                .ToListAsync();
            return rows.Select(c => new LocationRow(c.Id.ToString(), c.UniqueId, c.Name, null, c.StateId.ToString(), c.IsActive, c.Groups, c.Shops, c.TimeZoneId)).ToList();
        }

        public async Task<List<LocationRow>> ListGroupsAsync(int? cityId, bool includeInactive)
        {
            var rows = await _context.LocationGroups
                .Where(g => (includeInactive || g.IsActive) && (cityId == null || g.CityId == cityId))
                .OrderBy(g => g.Name)
                .Select(g => new { g.Id, g.UniqueId, g.Name, g.CityId, g.IsActive, Shops = g.Shops.Count })
                .ToListAsync();
            return rows.Select(g => new LocationRow(g.Id.ToString(), g.UniqueId, g.Name, null, g.CityId.ToString(), g.IsActive, 0, g.Shops)).ToList();
        }

        public Task<Country?> FindCountryAsync(string isoCode) => _context.Countries.FirstOrDefaultAsync(c => c.ISOCode == isoCode);

        public Task<State?> FindStateAsync(int id) => _context.States.Include(s => s.Country).FirstOrDefaultAsync(s => s.Id == id);

        public Task<City?> FindCityAsync(int id) => _context.Cities.Include(c => c.State).FirstOrDefaultAsync(c => c.Id == id);

        public Task<LocationGroup?> FindGroupAsync(int id) => _context.LocationGroups.Include(g => g.City).FirstOrDefaultAsync(g => g.Id == id);

        public Task<bool> CountryNameTakenAsync(string name, string? exceptIsoCode)
        {
            var lower = name.ToLower();
            return _context.Countries.AnyAsync(c => c.Name.ToLower() == lower && c.ISOCode != exceptIsoCode);
        }

        public Task<bool> StateTakenAsync(string countryCode, string name, string code, int? exceptId)
        {
            var lower = name.ToLower();
            var upper = code.ToUpper();
            return _context.States.AnyAsync(s => s.CountryCode == countryCode && s.Id != exceptId
                && (s.Name.ToLower() == lower || s.Code.ToUpper() == upper));
        }

        public Task<bool> CityNameTakenAsync(int stateId, string name, int? exceptId)
        {
            var lower = name.ToLower();
            return _context.Cities.AnyAsync(c => c.StateId == stateId && c.Id != exceptId && c.Name.ToLower() == lower);
        }

        public Task<bool> GroupNameTakenAsync(int cityId, string name, int? exceptId)
        {
            var lower = name.ToLower();
            return _context.LocationGroups.AnyAsync(g => g.CityId == cityId && g.Id != exceptId && g.Name.ToLower() == lower);
        }

        public Task<bool> UniqueIdTakenAsync(LocationLevel level, string uniqueId) => level switch
        {
            LocationLevel.Country => _context.Countries.AnyAsync(c => c.UniqueId == uniqueId),
            LocationLevel.State => _context.States.AnyAsync(s => s.UniqueId == uniqueId),
            LocationLevel.City => _context.Cities.AnyAsync(c => c.UniqueId == uniqueId),
            _ => _context.LocationGroups.AnyAsync(g => g.UniqueId == uniqueId),
        };

        public Task<bool> ShopUniqueIdTakenAsync(string uniqueId) => _context.Shops.AnyAsync(s => s.UniqueId == uniqueId);

        public async Task<(int children, int shops)> CountDependentsAsync(LocationLevel level, string key)
        {
            switch (level)
            {
                case LocationLevel.Country:
                    return (await _context.States.CountAsync(s => s.CountryCode == key),
                            await _context.Shops.CountAsync(s => s.CountryCode == key));
                case LocationLevel.State:
                {
                    var id = int.Parse(key);
                    return (await _context.Cities.CountAsync(c => c.StateId == id) + await _context.Districts.CountAsync(d => d.StateId == id),
                            await _context.Shops.CountAsync(s => s.StateId == id));
                }
                case LocationLevel.City:
                {
                    var id = int.Parse(key);
                    return (await _context.LocationGroups.CountAsync(g => g.CityId == id),
                            await _context.Shops.CountAsync(s => s.CityId == id));
                }
                default:
                {
                    var id = int.Parse(key);
                    return (0, await _context.Shops.CountAsync(s => s.GroupId == id));
                }
            }
        }

        public Task AddCountryAsync(Country country) { _context.Countries.Add(country); return Task.CompletedTask; }
        public Task AddStateAsync(State state) { _context.States.Add(state); return Task.CompletedTask; }
        public Task AddCityAsync(City city) { _context.Cities.Add(city); return Task.CompletedTask; }
        public Task AddGroupAsync(LocationGroup group) { _context.LocationGroups.Add(group); return Task.CompletedTask; }
        public Task RemoveCountryAsync(Country country) { _context.Countries.Remove(country); return Task.CompletedTask; }
        public Task RemoveStateAsync(State state) { _context.States.Remove(state); return Task.CompletedTask; }
        public Task RemoveCityAsync(City city) { _context.Cities.Remove(city); return Task.CompletedTask; }
        public Task RemoveGroupAsync(LocationGroup group) { _context.LocationGroups.Remove(group); return Task.CompletedTask; }

        public async Task SaveAsync() => await _context.SaveChangesAsync();

        public Task<List<Shop>> ShopsInGroupAsync(int groupId) =>
            _context.Shops.Where(s => s.GroupId == groupId).OrderBy(s => s.Name).ToListAsync();

        public Task<List<Shop>> ShopsInCityAsync(int cityId) =>
            _context.Shops.Where(s => s.CityId == cityId).OrderBy(s => s.Name).ToListAsync();

        public Task<List<Shop>> ActiveShopsInPlaceAsync(string? countryCode, int? stateId, int? cityId)
        {
            var query = _context.Shops.Where(s => s.Status == ShopStatus.Active);
            if (cityId != null) query = query.Where(s => s.CityId == cityId);
            else if (stateId != null) query = query.Where(s => s.StateId == stateId);
            else if (!string.IsNullOrWhiteSpace(countryCode)) query = query.Where(s => s.CountryCode == countryCode);
            return query.OrderBy(s => s.Name).Take(501).ToListAsync();
        }

        public async Task<int> BackfillUniqueIdsAsync(Func<string, string> next)
        {
            var changed = 0;
            var used = new HashSet<string>();

            string Fresh(string prefix)
            {
                string id;
                do { id = next(prefix); } while (!used.Add(id));
                return id;
            }

            foreach (var c in await _context.Countries.Where(c => c.UniqueId == null).ToListAsync()) { c.UniqueId = Fresh("CNT"); changed++; }
            foreach (var s in await _context.States.Where(s => s.UniqueId == null).ToListAsync()) { s.UniqueId = Fresh("STA"); changed++; }

            // countries that should have a time zone (the ones the product is sold in) get it when they have none
            var zones = new Dictionary<string, string> { ["IN"] = "Asia/Kolkata" };
            foreach (var c in await _context.Countries.Where(c => c.TimeZoneId == null).ToListAsync())
            {
                if (zones.TryGetValue(c.ISOCode, out var zone)) { c.TimeZoneId = zone; changed++; }
            }

            // a shop that already pays for a subscription gets its identifier too
            var subscribed = await _context.Shops
                .Where(s => s.UniqueId == null && _context.Subscriptions.Any(sub => sub.ShopId == s.Id))
                .ToListAsync();
            foreach (var shop in subscribed)
            {
                shop.UniqueId = Fresh("SHP");
                changed++;
            }

            if (changed > 0)
                await _context.SaveChangesAsync();
            return changed;
        }
    }
}

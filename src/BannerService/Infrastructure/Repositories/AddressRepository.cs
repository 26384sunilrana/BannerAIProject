namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Microsoft.EntityFrameworkCore;
    using Data;

    public class AddressRepository : IAddressRepository
    {
        private readonly ApplicationDbContext _context;

        public AddressRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Country operations
        public async Task<Country?> GetCountryAsync(string isoCode)
        {
            return await _context.Countries
                .FirstOrDefaultAsync(c => c.ISOCode == isoCode);
        }

        public async Task<List<Country>> GetAllCountriesAsync(bool activeOnly = true)
        {
            var query = _context.Countries.AsQueryable();
            if (activeOnly)
                query = query.Where(c => c.IsActive);

            return await query.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Country> AddCountryAsync(Country country)
        {
            _context.Countries.Add(country);
            await _context.SaveChangesAsync();
            return country;
        }

        // State operations
        public async Task<State?> GetStateAsync(int stateId)
        {
            return await _context.States
                .Include(s => s.Country)
                .FirstOrDefaultAsync(s => s.Id == stateId);
        }

        public async Task<List<State>> GetStatesByCountryAsync(string countryCode, bool activeOnly = true)
        {
            var query = _context.States
                .Where(s => s.CountryCode == countryCode)
                .AsQueryable();

            if (activeOnly)
                query = query.Where(s => s.IsActive);

            return await query
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<State>> GetAllStatesAsync(bool activeOnly = true)
        {
            var query = _context.States.AsQueryable();
            if (activeOnly)
                query = query.Where(s => s.IsActive);

            return await query
                .OrderBy(s => s.CountryCode)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<State> AddStateAsync(State state)
        {
            _context.States.Add(state);
            await _context.SaveChangesAsync();
            return state;
        }

        // District operations
        public async Task<District?> GetDistrictAsync(int districtId)
        {
            return await _context.Districts
                .Include(d => d.State)
                .FirstOrDefaultAsync(d => d.Id == districtId);
        }

        public async Task<List<District>> GetDistrictsByStateAsync(int stateId, bool activeOnly = true)
        {
            var query = _context.Districts
                .Where(d => d.StateId == stateId)
                .AsQueryable();

            if (activeOnly)
                query = query.Where(d => d.IsActive);

            return await query
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<List<District>> GetAllDistrictsAsync(bool activeOnly = true)
        {
            var query = _context.Districts.AsQueryable();
            if (activeOnly)
                query = query.Where(d => d.IsActive);

            return await query
                .OrderBy(d => d.State!.Name)
                .ThenBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<District> AddDistrictAsync(District district)
        {
            _context.Districts.Add(district);
            await _context.SaveChangesAsync();
            return district;
        }

        // Bulk operations
        public async Task AddCountriesAsync(List<Country> countries)
        {
            await _context.Countries.AddRangeAsync(countries);
            await _context.SaveChangesAsync();
        }

        public async Task AddStatesAsync(List<State> states)
        {
            await _context.States.AddRangeAsync(states);
            await _context.SaveChangesAsync();
        }

        public async Task AddDistrictsAsync(List<District> districts)
        {
            await _context.Districts.AddRangeAsync(districts);
            await _context.SaveChangesAsync();
        }

        // Search operations
        public async Task<List<State>> SearchStatesAsync(string searchTerm)
        {
            return await _context.States
                .Where(s => s.IsActive && (s.Name.Contains(searchTerm) || s.Code.Contains(searchTerm)))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<District>> SearchDistrictsAsync(string searchTerm)
        {
            return await _context.Districts
                .Where(d => d.IsActive && (d.Name.Contains(searchTerm) || d.Code.Contains(searchTerm)))
                .OrderBy(d => d.Name)
                .ToListAsync();
        }
    }
}

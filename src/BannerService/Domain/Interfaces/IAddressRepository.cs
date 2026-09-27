namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface IAddressRepository
    {
        // Country operations
        Task<Country?> GetCountryAsync(string isoCode);
        Task<List<Country>> GetAllCountriesAsync(bool activeOnly = true);
        Task<Country> AddCountryAsync(Country country);

        // State operations
        Task<State?> GetStateAsync(int stateId);
        Task<List<State>> GetStatesByCountryAsync(string countryCode, bool activeOnly = true);
        Task<List<State>> GetAllStatesAsync(bool activeOnly = true);
        Task<State> AddStateAsync(State state);

        // District operations
        Task<District?> GetDistrictAsync(int districtId);
        Task<List<District>> GetDistrictsByStateAsync(int stateId, bool activeOnly = true);
        Task<List<District>> GetAllDistrictsAsync(bool activeOnly = true);
        Task<District> AddDistrictAsync(District district);

        // Bulk operations for seeding
        Task AddCountriesAsync(List<Country> countries);
        Task AddStatesAsync(List<State> states);
        Task AddDistrictsAsync(List<District> districts);

        // Search
        Task<List<State>> SearchStatesAsync(string searchTerm);
        Task<List<District>> SearchDistrictsAsync(string searchTerm);
    }
}

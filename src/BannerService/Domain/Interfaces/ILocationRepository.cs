namespace BannerService.Domain.Interfaces
{
    using Entities;

    /// <summary>One line of a location list: any level of the hierarchy, with how much hangs under it.</summary>
    public record LocationRow(string Id, string? UniqueId, string Name, string? Code, string? ParentId, bool IsActive, int ChildCount, int ShopCount, string? TimeZoneId = null);

    public enum LocationLevel { Country, State, City, Group }

    public interface ILocationRepository
    {
        Task<List<LocationRow>> ListCountriesAsync(bool includeInactive);
        Task<List<LocationRow>> ListStatesAsync(string? countryCode, bool includeInactive);
        Task<List<LocationRow>> ListCitiesAsync(int? stateId, bool includeInactive);
        Task<List<LocationRow>> ListGroupsAsync(int? cityId, bool includeInactive);

        Task<Country?> FindCountryAsync(string isoCode);
        Task<State?> FindStateAsync(int id);
        Task<City?> FindCityAsync(int id);
        Task<LocationGroup?> FindGroupAsync(int id);

        Task<bool> CountryNameTakenAsync(string name, string? exceptIsoCode);
        Task<bool> StateTakenAsync(string countryCode, string name, string code, int? exceptId);
        Task<bool> CityNameTakenAsync(int stateId, string name, int? exceptId);
        Task<bool> GroupNameTakenAsync(int cityId, string name, int? exceptId);
        Task<bool> UniqueIdTakenAsync(LocationLevel level, string uniqueId);
        Task<bool> ShopUniqueIdTakenAsync(string uniqueId);

        /// <summary>Things directly under the item (states under a country ...), and shops placed in it or below it.</summary>
        Task<(int children, int shops)> CountDependentsAsync(LocationLevel level, string key);

        Task AddCountryAsync(Country country);
        Task AddStateAsync(State state);
        Task AddCityAsync(City city);
        Task AddGroupAsync(LocationGroup group);
        Task RemoveCountryAsync(Country country);
        Task RemoveStateAsync(State state);
        Task RemoveCityAsync(City city);
        Task RemoveGroupAsync(LocationGroup group);
        Task SaveAsync();

        Task<List<Shop>> ShopsInGroupAsync(int groupId);
        Task<List<Shop>> ShopsInCityAsync(int cityId);

        /// <summary>Active shops in a place: the deepest of city, state and country that is given; every active shop when none is.</summary>
        Task<List<Shop>> ActiveShopsInPlaceAsync(string? countryCode, int? stateId, int? cityId);

        /// <summary>Fills in the identifiers of countries and states created before identifiers existed. Returns how many rows changed.</summary>
        Task<int> BackfillUniqueIdsAsync(Func<string, string> next);
    }
}

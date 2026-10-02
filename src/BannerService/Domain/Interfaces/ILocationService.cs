namespace BannerService.Domain.Interfaces
{
    using Entities;

    public enum LocationOutcome { Ok, Invalid, NotFound, Conflict }

    public record LocationResult<T>(LocationOutcome Outcome, string Message, T? Value = default)
    {
        public bool Succeeded => Outcome == LocationOutcome.Ok;
        public static LocationResult<T> Ok(T value, string message = "Saved") => new(LocationOutcome.Ok, message, value);
        public static LocationResult<T> Invalid(string message) => new(LocationOutcome.Invalid, message);
        public static LocationResult<T> NotFound(string message) => new(LocationOutcome.NotFound, message);
        public static LocationResult<T> Conflict(string message) => new(LocationOutcome.Conflict, message);
    }

    /// <summary>
    /// The location hierarchy: Country > State > City > Group > Shop. Every level has a platform-wide unique identifier,
    /// can be created, renamed, switched off and deleted (deleting is refused while anything is still under it).
    /// </summary>
    public interface ILocationService
    {
        Task<List<LocationRow>> ListCountriesAsync(bool includeInactive);
        Task<List<LocationRow>> ListStatesAsync(string? countryCode, bool includeInactive);
        Task<List<LocationRow>> ListCitiesAsync(int? stateId, bool includeInactive);
        Task<List<LocationRow>> ListGroupsAsync(int? cityId, bool includeInactive);

        Task<LocationResult<Country>> CreateCountryAsync(string isoCode, string name, string? regionName, string? phoneCode);
        Task<LocationResult<Country>> UpdateCountryAsync(string isoCode, string name, string? regionName, string? phoneCode, bool isActive);
        Task<LocationResult<string>> DeleteCountryAsync(string isoCode);

        Task<LocationResult<State>> CreateStateAsync(string countryCode, string code, string name, string? regionType);
        Task<LocationResult<State>> UpdateStateAsync(int id, string code, string name, string? regionType, bool isActive);
        Task<LocationResult<string>> DeleteStateAsync(int id);

        Task<LocationResult<City>> CreateCityAsync(int stateId, string name);
        Task<LocationResult<City>> UpdateCityAsync(int id, string name, bool isActive);
        Task<LocationResult<string>> DeleteCityAsync(int id);

        Task<LocationResult<LocationGroup>> CreateGroupAsync(int cityId, string name);
        Task<LocationResult<LocationGroup>> UpdateGroupAsync(int id, string name, bool isActive);
        Task<LocationResult<string>> DeleteGroupAsync(int id);

        Task<LocationResult<List<Shop>>> ShopsInGroupAsync(int groupId);

        /// <summary>Places a shop in a city and, optionally, a group of that city.</summary>
        Task<LocationResult<Shop>> SetShopLocationAsync(Guid shopId, int cityId, int? groupId);

        /// <summary>Gives the shop its SHP- identifier if it does not have one yet. Safe to call again.</summary>
        Task<string?> EnsureShopUniqueIdAsync(Guid shopId);

        /// <summary>Gives identifiers to countries and states that predate them.</summary>
        Task<int> BackfillUniqueIdsAsync();
    }
}

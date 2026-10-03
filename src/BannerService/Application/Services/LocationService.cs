namespace BannerService.Application.Services
{
    using System.Text.RegularExpressions;
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.Services;

    public class LocationService : ILocationService
    {
        private static readonly Regex CountryCodePattern = new("^[A-Z]{2}$", RegexOptions.Compiled);
        private static readonly Regex StateCodePattern = new("^[A-Za-z0-9]{1,10}$", RegexOptions.Compiled);
        private static readonly Regex PhoneCodePattern = new(@"^\+\d{1,4}$", RegexOptions.Compiled);

        private readonly ILocationRepository _locations;
        private readonly IShopRepository _shops;
        private readonly ILogger<LocationService> _logger;

        public LocationService(ILocationRepository locations, IShopRepository shops, ILogger<LocationService> logger)
        {
            _locations = locations;
            _shops = shops;
            _logger = logger;
        }

        public Task<List<LocationRow>> ListCountriesAsync(bool includeInactive) => _locations.ListCountriesAsync(includeInactive);
        public Task<List<LocationRow>> ListStatesAsync(string? countryCode, bool includeInactive) => _locations.ListStatesAsync(countryCode, includeInactive);
        public Task<List<LocationRow>> ListCitiesAsync(int? stateId, bool includeInactive) => _locations.ListCitiesAsync(stateId, includeInactive);
        public Task<List<LocationRow>> ListGroupsAsync(int? cityId, bool includeInactive) => _locations.ListGroupsAsync(cityId, includeInactive);

        // ----- countries

        public async Task<LocationResult<Country>> CreateCountryAsync(string isoCode, string name, string? regionName, string? phoneCode)
        {
            isoCode = (isoCode ?? string.Empty).Trim().ToUpperInvariant();
            if (!CountryCodePattern.IsMatch(isoCode))
                return LocationResult<Country>.Invalid("The country code is two letters, for example IN.");
            if (CheckName(name, "country") is { } nameError)
                return LocationResult<Country>.Invalid(nameError);
            if (CheckPhoneCode(phoneCode) is { } phoneError)
                return LocationResult<Country>.Invalid(phoneError);

            name = name.Trim();
            if (await _locations.FindCountryAsync(isoCode) != null)
                return LocationResult<Country>.Conflict($"The country code {isoCode} is already in use.");
            if (await _locations.CountryNameTakenAsync(name, null))
                return LocationResult<Country>.Conflict($"A country called {name} already exists.");

            var country = new Country
            {
                ISOCode = isoCode,
                Name = name,
                RegionName = Clean(regionName),
                PhoneCode = Clean(phoneCode),
                UniqueId = await NewIdAsync(LocationLevel.Country, UniqueIdGenerator.Country),
            };
            await _locations.AddCountryAsync(country);
            await _locations.SaveAsync();
            _logger.LogInformation("Country created: {Code} {UniqueId}", country.ISOCode, country.UniqueId);
            return LocationResult<Country>.Ok(country, "Country created");
        }

        public async Task<LocationResult<Country>> UpdateCountryAsync(string isoCode, string name, string? regionName, string? phoneCode, bool isActive)
        {
            var country = await _locations.FindCountryAsync(isoCode);
            if (country == null)
                return LocationResult<Country>.NotFound("Country not found");
            if (CheckName(name, "country") is { } nameError)
                return LocationResult<Country>.Invalid(nameError);
            if (CheckPhoneCode(phoneCode) is { } phoneError)
                return LocationResult<Country>.Invalid(phoneError);

            name = name.Trim();
            if (await _locations.CountryNameTakenAsync(name, country.ISOCode))
                return LocationResult<Country>.Conflict($"A country called {name} already exists.");

            country.Name = name;
            // a missing value keeps what is stored; an empty one clears it
            if (regionName != null) country.RegionName = Clean(regionName);
            if (phoneCode != null) country.PhoneCode = Clean(phoneCode);
            country.IsActive = isActive;
            country.UpdatedAt = DateTime.UtcNow;
            await _locations.SaveAsync();
            return LocationResult<Country>.Ok(country);
        }

        public async Task<LocationResult<string>> DeleteCountryAsync(string isoCode)
        {
            var country = await _locations.FindCountryAsync(isoCode);
            if (country == null)
                return LocationResult<string>.NotFound("Country not found");

            var (children, shops) = await _locations.CountDependentsAsync(LocationLevel.Country, country.ISOCode);
            if (children > 0 || shops > 0)
                return LocationResult<string>.Conflict(InUse(country.Name, children, "state", shops));

            await _locations.RemoveCountryAsync(country);
            await _locations.SaveAsync();
            _logger.LogInformation("Country deleted: {Code}", isoCode);
            return LocationResult<string>.Ok(country.ISOCode, "Country deleted");
        }

        // ----- states

        public async Task<LocationResult<State>> CreateStateAsync(string countryCode, string code, string name, string? regionType)
        {
            var country = await _locations.FindCountryAsync((countryCode ?? string.Empty).Trim().ToUpperInvariant());
            if (country == null)
                return LocationResult<State>.NotFound("Country not found");
            if (!country.IsActive)
                return LocationResult<State>.Invalid($"{country.Name} is switched off. Switch it on before adding states.");
            if (CheckStateFields(code, name) is { } error)
                return LocationResult<State>.Invalid(error);

            code = code.Trim().ToUpperInvariant();
            name = name.Trim();
            if (await _locations.StateTakenAsync(country.ISOCode, name, code, null))
                return LocationResult<State>.Conflict($"{country.Name} already has a state with that name or code.");

            var state = new State
            {
                CountryCode = country.ISOCode,
                Code = code,
                Name = name,
                RegionType = Clean(regionType),
                UniqueId = await NewIdAsync(LocationLevel.State, UniqueIdGenerator.State),
            };
            await _locations.AddStateAsync(state);
            await _locations.SaveAsync();
            _logger.LogInformation("State created: {Id} {UniqueId}", state.Id, state.UniqueId);
            return LocationResult<State>.Ok(state, "State created");
        }

        public async Task<LocationResult<State>> UpdateStateAsync(int id, string code, string name, string? regionType, bool isActive)
        {
            var state = await _locations.FindStateAsync(id);
            if (state == null)
                return LocationResult<State>.NotFound("State not found");
            if (CheckStateFields(code, name) is { } error)
                return LocationResult<State>.Invalid(error);
            if (isActive && !state.IsActive && state.Country is { IsActive: false })
                return LocationResult<State>.Invalid($"{state.Country.Name} is switched off.");

            code = code.Trim().ToUpperInvariant();
            name = name.Trim();
            if (await _locations.StateTakenAsync(state.CountryCode, name, code, state.Id))
                return LocationResult<State>.Conflict("Another state in this country has that name or code.");

            state.Code = code;
            state.Name = name;
            if (regionType != null) state.RegionType = Clean(regionType);
            state.IsActive = isActive;
            state.UpdatedAt = DateTime.UtcNow;
            await _locations.SaveAsync();
            return LocationResult<State>.Ok(state);
        }

        public async Task<LocationResult<string>> DeleteStateAsync(int id)
        {
            var state = await _locations.FindStateAsync(id);
            if (state == null)
                return LocationResult<string>.NotFound("State not found");

            var (children, shops) = await _locations.CountDependentsAsync(LocationLevel.State, id.ToString());
            if (children > 0 || shops > 0)
                return LocationResult<string>.Conflict(InUse(state.Name, children, "city or district", shops));

            await _locations.RemoveStateAsync(state);
            await _locations.SaveAsync();
            _logger.LogInformation("State deleted: {Id}", id);
            return LocationResult<string>.Ok(id.ToString(), "State deleted");
        }

        // ----- cities

        public async Task<LocationResult<City>> CreateCityAsync(int stateId, string name)
        {
            var state = await _locations.FindStateAsync(stateId);
            if (state == null)
                return LocationResult<City>.NotFound("State not found");
            if (!state.IsActive)
                return LocationResult<City>.Invalid($"{state.Name} is switched off. Switch it on before adding cities.");
            if (CheckName(name, "city") is { } error)
                return LocationResult<City>.Invalid(error);

            name = name.Trim();
            if (await _locations.CityNameTakenAsync(stateId, name, null))
                return LocationResult<City>.Conflict($"{state.Name} already has a city called {name}.");

            var city = new City
            {
                StateId = stateId,
                Name = name,
                UniqueId = await NewIdAsync(LocationLevel.City, UniqueIdGenerator.City),
            };
            await _locations.AddCityAsync(city);
            await _locations.SaveAsync();
            _logger.LogInformation("City created: {Id} {UniqueId}", city.Id, city.UniqueId);
            return LocationResult<City>.Ok(city, "City created");
        }

        public async Task<LocationResult<City>> UpdateCityAsync(int id, string name, bool isActive)
        {
            var city = await _locations.FindCityAsync(id);
            if (city == null)
                return LocationResult<City>.NotFound("City not found");
            if (CheckName(name, "city") is { } error)
                return LocationResult<City>.Invalid(error);
            if (isActive && !city.IsActive && city.State is { IsActive: false })
                return LocationResult<City>.Invalid($"{city.State.Name} is switched off.");

            name = name.Trim();
            if (await _locations.CityNameTakenAsync(city.StateId, name, city.Id))
                return LocationResult<City>.Conflict($"Another city in this state is called {name}.");

            city.Name = name;
            city.IsActive = isActive;
            city.UpdatedAt = DateTime.UtcNow;

            // shops keep a copy of the city name for display; keep it in step
            foreach (var shop in await _locations.ShopsInCityAsync(city.Id))
                shop.City = city.Name;

            await _locations.SaveAsync();
            return LocationResult<City>.Ok(city);
        }

        public async Task<LocationResult<string>> DeleteCityAsync(int id)
        {
            var city = await _locations.FindCityAsync(id);
            if (city == null)
                return LocationResult<string>.NotFound("City not found");

            var (children, shops) = await _locations.CountDependentsAsync(LocationLevel.City, id.ToString());
            if (children > 0 || shops > 0)
                return LocationResult<string>.Conflict(InUse(city.Name, children, "group", shops));

            await _locations.RemoveCityAsync(city);
            await _locations.SaveAsync();
            _logger.LogInformation("City deleted: {Id}", id);
            return LocationResult<string>.Ok(id.ToString(), "City deleted");
        }

        // ----- groups

        public async Task<LocationResult<LocationGroup>> CreateGroupAsync(int cityId, string name)
        {
            var city = await _locations.FindCityAsync(cityId);
            if (city == null)
                return LocationResult<LocationGroup>.NotFound("City not found");
            if (!city.IsActive)
                return LocationResult<LocationGroup>.Invalid($"{city.Name} is switched off. Switch it on before adding groups.");
            if (CheckName(name, "group") is { } error)
                return LocationResult<LocationGroup>.Invalid(error);

            name = name.Trim();
            if (await _locations.GroupNameTakenAsync(cityId, name, null))
                return LocationResult<LocationGroup>.Conflict($"{city.Name} already has a group called {name}.");

            var group = new LocationGroup
            {
                CityId = cityId,
                Name = name,
                UniqueId = await NewIdAsync(LocationLevel.Group, UniqueIdGenerator.Group),
            };
            await _locations.AddGroupAsync(group);
            await _locations.SaveAsync();
            _logger.LogInformation("Group created: {Id} {UniqueId}", group.Id, group.UniqueId);
            return LocationResult<LocationGroup>.Ok(group, "Group created");
        }

        public async Task<LocationResult<LocationGroup>> UpdateGroupAsync(int id, string name, bool isActive)
        {
            var group = await _locations.FindGroupAsync(id);
            if (group == null)
                return LocationResult<LocationGroup>.NotFound("Group not found");
            if (CheckName(name, "group") is { } error)
                return LocationResult<LocationGroup>.Invalid(error);
            if (isActive && !group.IsActive && group.City is { IsActive: false })
                return LocationResult<LocationGroup>.Invalid($"{group.City.Name} is switched off.");

            name = name.Trim();
            if (await _locations.GroupNameTakenAsync(group.CityId, name, group.Id))
                return LocationResult<LocationGroup>.Conflict($"Another group in this city is called {name}.");

            group.Name = name;
            group.IsActive = isActive;
            group.UpdatedAt = DateTime.UtcNow;
            await _locations.SaveAsync();
            return LocationResult<LocationGroup>.Ok(group);
        }

        public async Task<LocationResult<string>> DeleteGroupAsync(int id)
        {
            var group = await _locations.FindGroupAsync(id);
            if (group == null)
                return LocationResult<string>.NotFound("Group not found");

            var (_, shops) = await _locations.CountDependentsAsync(LocationLevel.Group, id.ToString());
            if (shops > 0)
                return LocationResult<string>.Conflict(InUse(group.Name, 0, string.Empty, shops));

            await _locations.RemoveGroupAsync(group);
            await _locations.SaveAsync();
            _logger.LogInformation("Group deleted: {Id}", id);
            return LocationResult<string>.Ok(id.ToString(), "Group deleted");
        }

        public async Task<LocationResult<List<Shop>>> ShopsInGroupAsync(int groupId)
        {
            if (await _locations.FindGroupAsync(groupId) == null)
                return LocationResult<List<Shop>>.NotFound("Group not found");
            return LocationResult<List<Shop>>.Ok(await _locations.ShopsInGroupAsync(groupId));
        }

        // ----- time zones

        public async Task<LocationResult<Country>> SetCountryTimeZoneAsync(string isoCode, string? timeZoneId)
        {
            var country = await _locations.FindCountryAsync(isoCode);
            if (country == null)
                return LocationResult<Country>.NotFound("Country not found");
            if (CheckTimeZone(timeZoneId) is { } error)
                return LocationResult<Country>.Invalid(error);

            country.TimeZoneId = Clean(timeZoneId);
            country.UpdatedAt = DateTime.UtcNow;
            await _locations.SaveAsync();
            return LocationResult<Country>.Ok(country);
        }

        public async Task<LocationResult<City>> SetCityTimeZoneAsync(int id, string? timeZoneId)
        {
            var city = await _locations.FindCityAsync(id);
            if (city == null)
                return LocationResult<City>.NotFound("City not found");
            if (CheckTimeZone(timeZoneId) is { } error)
                return LocationResult<City>.Invalid(error);

            city.TimeZoneId = Clean(timeZoneId);
            city.UpdatedAt = DateTime.UtcNow;
            await _locations.SaveAsync();
            return LocationResult<City>.Ok(city);
        }

        private static string? CheckTimeZone(string? timeZoneId) =>
            string.IsNullOrWhiteSpace(timeZoneId) || TimeZones.TryGet(timeZoneId, out _)
                ? null
                : "That time zone is not known on this server. Use a name like Asia/Kolkata or Europe/London.";

        // ----- shops

        public async Task<LocationResult<Shop>> SetShopLocationAsync(Guid shopId, int cityId, int? groupId)
        {
            var shop = await _shops.GetByIdAsync(shopId);
            if (shop == null)
                return LocationResult<Shop>.NotFound("Shop not found");

            var city = await _locations.FindCityAsync(cityId);
            if (city == null)
                return LocationResult<Shop>.NotFound("City not found");
            if (!city.IsActive)
                return LocationResult<Shop>.Invalid($"{city.Name} is switched off.");

            LocationGroup? group = null;
            if (groupId.HasValue)
            {
                group = await _locations.FindGroupAsync(groupId.Value);
                if (group == null)
                    return LocationResult<Shop>.NotFound("Group not found");
                if (group.CityId != city.Id)
                    return LocationResult<Shop>.Invalid($"{group.Name} is not a group of {city.Name}.");
                if (!group.IsActive)
                    return LocationResult<Shop>.Invalid($"{group.Name} is switched off.");
            }

            shop.SetLocation(city, group);
            await _shops.UpdateAsync(shop);
            return LocationResult<Shop>.Ok(shop, "Location saved");
        }

        public async Task<string?> EnsureShopUniqueIdAsync(Guid shopId)
        {
            var shop = await _shops.GetByIdAsync(shopId);
            if (shop == null)
                return null;
            if (!string.IsNullOrEmpty(shop.UniqueId))
                return shop.UniqueId;

            for (var attempt = 0; attempt < 5; attempt++)
            {
                var id = UniqueIdGenerator.Generate(UniqueIdGenerator.Shop);
                if (await _locations.ShopUniqueIdTakenAsync(id))
                    continue;

                shop.UniqueId = id;
                await _shops.UpdateAsync(shop);
                _logger.LogInformation("Shop {ShopId} got identifier {UniqueId}", shopId, id);
                return id;
            }

            throw new InvalidOperationException("Could not find a free shop identifier");
        }

        public Task<int> BackfillUniqueIdsAsync() => _locations.BackfillUniqueIdsAsync(UniqueIdGenerator.Generate);

        // ----- helpers

        private async Task<string> NewIdAsync(LocationLevel level, string prefix)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var id = UniqueIdGenerator.Generate(prefix);
                if (!await _locations.UniqueIdTakenAsync(level, id))
                    return id;
            }

            throw new InvalidOperationException("Could not find a free identifier");
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? CheckName(string? name, string what)
        {
            var trimmed = (name ?? string.Empty).Trim();
            if (trimmed.Length < 2 || trimmed.Length > 100)
                return $"The {what} name needs 2 to 100 characters.";
            if (trimmed.Any(char.IsControl))
                return $"The {what} name has characters that are not allowed.";
            return null;
        }

        private static string? CheckStateFields(string? code, string? name)
        {
            if (!StateCodePattern.IsMatch((code ?? string.Empty).Trim()))
                return "The state code is 1 to 10 letters or digits, for example MH.";
            return CheckName(name, "state");
        }

        private static string? CheckPhoneCode(string? phoneCode) =>
            string.IsNullOrWhiteSpace(phoneCode) || PhoneCodePattern.IsMatch(phoneCode.Trim())
                ? null
                : "The phone code looks like +91.";

        private static string InUse(string name, int children, string childWord, int shops)
        {
            var parts = new List<string>();
            if (children > 0) parts.Add($"{children} {childWord}{(children == 1 ? string.Empty : "s")}");
            if (shops > 0) parts.Add($"{shops} shop{(shops == 1 ? string.Empty : "s")}");
            return $"{name} cannot be deleted because it still has {string.Join(" and ", parts)}. Switch it off instead, or move them first.";
        }
    }
}

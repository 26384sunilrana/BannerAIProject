namespace BannerService.Presentation.Controllers
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Security;

    public class CreateCountryRequest
    {
        public string IsoCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? RegionName { get; set; }
        public string? PhoneCode { get; set; }
    }

    public class UpdateCountryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? RegionName { get; set; }
        public string? PhoneCode { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CreateStateRequest
    {
        public string CountryCode { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? RegionType { get; set; }
    }

    public class UpdateStateRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? RegionType { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CreateCityRequest
    {
        public int StateId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CreateGroupRequest
    {
        public int CityId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateNamedLocationRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class SetShopLocationRequest
    {
        public int CityId { get; set; }
        public int? GroupId { get; set; }
    }

    /// <summary>
    /// Country > State > City > Group > Shop. Anyone signed in can read the active places (to fill in an address);
    /// only an administrator changes them.
    /// </summary>
    [ApiController]
    [Route("api/locations")]
    [Authorize]
    public class LocationsController : ControllerBase
    {
        private readonly ILocationService _locations;

        public LocationsController(ILocationService locations)
        {
            _locations = locations;
        }

        private bool CanSeeInactive(bool requested) => requested && User.IsAdmin();

        // ----- reading

        [HttpGet("countries")]
        public async Task<IActionResult> Countries([FromQuery] bool includeInactive = false) =>
            Ok(await _locations.ListCountriesAsync(CanSeeInactive(includeInactive)));

        [HttpGet("states")]
        public async Task<IActionResult> States([FromQuery] string? countryCode = null, [FromQuery] bool includeInactive = false) =>
            Ok(await _locations.ListStatesAsync(countryCode?.ToUpperInvariant(), CanSeeInactive(includeInactive)));

        [HttpGet("cities")]
        public async Task<IActionResult> Cities([FromQuery] int? stateId = null, [FromQuery] bool includeInactive = false) =>
            Ok(await _locations.ListCitiesAsync(stateId, CanSeeInactive(includeInactive)));

        [HttpGet("groups")]
        public async Task<IActionResult> Groups([FromQuery] int? cityId = null, [FromQuery] bool includeInactive = false) =>
            Ok(await _locations.ListGroupsAsync(cityId, CanSeeInactive(includeInactive)));

        [HttpGet("groups/{groupId:int}/shops")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ShopsInGroup(int groupId)
        {
            var result = await _locations.ShopsInGroupAsync(groupId);
            if (!result.Succeeded)
                return Failure(result.Outcome, result.Message);

            return Ok(result.Value!.Select(s => new { s.Id, s.Name, s.UniqueId, s.Status, s.City }));
        }

        // ----- countries

        [HttpPost("countries")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCountry([FromBody] CreateCountryRequest request)
        {
            var result = await _locations.CreateCountryAsync(request.IsoCode, request.Name, request.RegionName, request.PhoneCode);
            return result.Succeeded ? Created($"api/locations/countries", Row(result.Value!)) : Failure(result.Outcome, result.Message);
        }

        [HttpPut("countries/{isoCode}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCountry(string isoCode, [FromBody] UpdateCountryRequest request)
        {
            var result = await _locations.UpdateCountryAsync(isoCode.ToUpperInvariant(), request.Name, request.RegionName, request.PhoneCode, request.IsActive);
            return result.Succeeded ? Ok(Row(result.Value!)) : Failure(result.Outcome, result.Message);
        }

        [HttpDelete("countries/{isoCode}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCountry(string isoCode)
        {
            var result = await _locations.DeleteCountryAsync(isoCode.ToUpperInvariant());
            return result.Succeeded ? Ok(new { message = result.Message }) : Failure(result.Outcome, result.Message);
        }

        // ----- states

        [HttpPost("states")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateState([FromBody] CreateStateRequest request)
        {
            var result = await _locations.CreateStateAsync(request.CountryCode, request.Code, request.Name, request.RegionType);
            return result.Succeeded ? Created($"api/locations/states", Row(result.Value!)) : Failure(result.Outcome, result.Message);
        }

        [HttpPut("states/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateState(int id, [FromBody] UpdateStateRequest request)
        {
            var result = await _locations.UpdateStateAsync(id, request.Code, request.Name, request.RegionType, request.IsActive);
            return result.Succeeded ? Ok(Row(result.Value!)) : Failure(result.Outcome, result.Message);
        }

        [HttpDelete("states/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteState(int id)
        {
            var result = await _locations.DeleteStateAsync(id);
            return result.Succeeded ? Ok(new { message = result.Message }) : Failure(result.Outcome, result.Message);
        }

        // ----- cities

        [HttpPost("cities")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCity([FromBody] CreateCityRequest request)
        {
            var result = await _locations.CreateCityAsync(request.StateId, request.Name);
            return result.Succeeded ? Created($"api/locations/cities", Row(result.Value!)) : Failure(result.Outcome, result.Message);
        }

        [HttpPut("cities/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] UpdateNamedLocationRequest request)
        {
            var result = await _locations.UpdateCityAsync(id, request.Name, request.IsActive);
            return result.Succeeded ? Ok(Row(result.Value!)) : Failure(result.Outcome, result.Message);
        }

        [HttpDelete("cities/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var result = await _locations.DeleteCityAsync(id);
            return result.Succeeded ? Ok(new { message = result.Message }) : Failure(result.Outcome, result.Message);
        }

        // ----- groups

        [HttpPost("groups")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            var result = await _locations.CreateGroupAsync(request.CityId, request.Name);
            return result.Succeeded ? Created($"api/locations/groups", Row(result.Value!)) : Failure(result.Outcome, result.Message);
        }

        [HttpPut("groups/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateGroup(int id, [FromBody] UpdateNamedLocationRequest request)
        {
            var result = await _locations.UpdateGroupAsync(id, request.Name, request.IsActive);
            return result.Succeeded ? Ok(Row(result.Value!)) : Failure(result.Outcome, result.Message);
        }

        [HttpDelete("groups/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var result = await _locations.DeleteGroupAsync(id);
            return result.Succeeded ? Ok(new { message = result.Message }) : Failure(result.Outcome, result.Message);
        }

        // ----- a shop's own place in the hierarchy

        /// <summary>The shop owner (or an administrator) places the shop in a city and, optionally, a group of that city.</summary>
        [HttpPut("shops/{shopId:guid}")]
        [Authorize(Roles = "Admin,ShopOwner")]
        public async Task<IActionResult> SetShopLocation(Guid shopId, [FromBody] SetShopLocationRequest request)
        {
            var result = await _locations.SetShopLocationAsync(shopId, request.CityId, request.GroupId);
            if (!result.Succeeded)
                return Failure(result.Outcome, result.Message);

            var shop = result.Value!;
            return Ok(new { shop.Id, shop.UniqueId, shop.CountryCode, shop.StateId, shop.CityId, shop.GroupId, shop.City });
        }

        private IActionResult Failure(LocationOutcome outcome, string message) => outcome switch
        {
            LocationOutcome.NotFound => NotFound(new { message }),
            LocationOutcome.Conflict => Conflict(new { message }),
            _ => BadRequest(new { message }),
        };

        private static LocationRow Row(Country c) => new(c.ISOCode, c.UniqueId, c.Name, c.ISOCode, null, c.IsActive, 0, 0);
        private static LocationRow Row(State s) => new(s.Id.ToString(), s.UniqueId, s.Name, s.Code, s.CountryCode, s.IsActive, 0, 0);
        private static LocationRow Row(City c) => new(c.Id.ToString(), c.UniqueId, c.Name, null, c.StateId.ToString(), c.IsActive, 0, 0);
        private static LocationRow Row(LocationGroup g) => new(g.Id.ToString(), g.UniqueId, g.Name, null, g.CityId.ToString(), g.IsActive, 0, 0);
    }
}

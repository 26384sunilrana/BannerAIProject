namespace BannerService.Presentation.Controllers
{
    using Application.DTOs;
    using Domain.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly IAddressRepository _addressRepository;
        private readonly ILogger<AddressController> _logger;

        public AddressController(IAddressRepository addressRepository, ILogger<AddressController> logger)
        {
            _addressRepository = addressRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all active countries
        /// </summary>
        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries()
        {
            try
            {
                var countries = await _addressRepository.GetAllCountriesAsync(activeOnly: true);
                var countryDtos = countries.Select(c => new CountryDto
                {
                    ISOCode = c.ISOCode,
                    Name = c.Name,
                    RegionName = c.RegionName,
                    PhoneCode = c.PhoneCode,
                    IsActive = c.IsActive
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = countryDtos,
                    count = countryDtos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching countries");
                return StatusCode(500, new { message = "Error fetching countries" });
            }
        }

        /// <summary>
        /// Get states by country code
        /// </summary>
        [HttpGet("countries/{countryCode}/states")]
        public async Task<IActionResult> GetStatesByCountry(string countryCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(countryCode))
                    return BadRequest(new { message = "Country code is required" });

                var states = await _addressRepository.GetStatesByCountryAsync(countryCode, activeOnly: true);

                if (states.Count == 0)
                    return NotFound(new { message = "No states found for the given country" });

                var stateDtos = states.Select(s => new StateDto
                {
                    Id = s.Id,
                    CountryCode = s.CountryCode,
                    Code = s.Code,
                    Name = s.Name,
                    RegionType = s.RegionType,
                    IsActive = s.IsActive
                }).ToList();

                return Ok(new
                {
                    success = true,
                    countryCode,
                    data = stateDtos,
                    count = stateDtos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching states for country {CountryCode}", countryCode);
                return StatusCode(500, new { message = "Error fetching states" });
            }
        }

        /// <summary>
        /// Get districts by state id
        /// </summary>
        [HttpGet("states/{stateId}/districts")]
        public async Task<IActionResult> GetDistrictsByState(int stateId)
        {
            try
            {
                if (stateId <= 0)
                    return BadRequest(new { message = "Valid state ID is required" });

                var districts = await _addressRepository.GetDistrictsByStateAsync(stateId, activeOnly: true);

                if (districts.Count == 0)
                    return NotFound(new { message = "No districts found for the given state" });

                var districtDtos = districts.Select(d => new DistrictDto
                {
                    Id = d.Id,
                    StateId = d.StateId,
                    Code = d.Code,
                    Name = d.Name,
                    RegionType = d.RegionType,
                    IsActive = d.IsActive
                }).ToList();

                return Ok(new
                {
                    success = true,
                    stateId,
                    data = districtDtos,
                    count = districtDtos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching districts for state {StateId}", stateId);
                return StatusCode(500, new { message = "Error fetching districts" });
            }
        }

        /// <summary>
        /// Search states by name or code
        /// </summary>
        [HttpGet("states/search")]
        public async Task<IActionResult> SearchStates([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest(new { message = "Search term is required" });

                var states = await _addressRepository.SearchStatesAsync(searchTerm);
                var stateDtos = states.Select(s => new StateDto
                {
                    Id = s.Id,
                    CountryCode = s.CountryCode,
                    Code = s.Code,
                    Name = s.Name,
                    RegionType = s.RegionType,
                    IsActive = s.IsActive
                }).ToList();

                return Ok(new
                {
                    success = true,
                    searchTerm,
                    data = stateDtos,
                    count = stateDtos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching states with term {SearchTerm}", searchTerm);
                return StatusCode(500, new { message = "Error searching states" });
            }
        }

        /// <summary>
        /// Search districts by name or code
        /// </summary>
        [HttpGet("districts/search")]
        public async Task<IActionResult> SearchDistricts([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest(new { message = "Search term is required" });

                var districts = await _addressRepository.SearchDistrictsAsync(searchTerm);
                var districtDtos = districts.Select(d => new DistrictDto
                {
                    Id = d.Id,
                    StateId = d.StateId,
                    Code = d.Code,
                    Name = d.Name,
                    RegionType = d.RegionType,
                    IsActive = d.IsActive
                }).ToList();

                return Ok(new
                {
                    success = true,
                    searchTerm,
                    data = districtDtos,
                    count = districtDtos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching districts with term {SearchTerm}", searchTerm);
                return StatusCode(500, new { message = "Error searching districts" });
            }
        }
    }
}

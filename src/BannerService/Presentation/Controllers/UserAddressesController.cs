using BannerService.Application.DTOs;
using BannerService.Application.Services;
using BannerService.Application.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BannerService.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserAddressesController : ControllerBase
    {
        private readonly IUserAddressService _userAddressService;
        private readonly ILogger<UserAddressesController> _logger;

        public UserAddressesController(
            IUserAddressService userAddressService,
            ILogger<UserAddressesController> logger)
        {
            _userAddressService = userAddressService;
            _logger = logger;
        }

        private string GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User ID not found in token");
            return userId;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAddresses()
        {
            try
            {
                var userId = GetUserId();
                var addresses = await _userAddressService.GetAllByUserAsync(userId);
                return Ok(new
                {
                    success = true,
                    data = addresses,
                    count = addresses.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user addresses");
                return StatusCode(500, new { message = "Error retrieving addresses" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAddressById(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var address = await _userAddressService.GetByIdAsync(userId, id);
                return Ok(new { success = true, data = address });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Address not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving address {AddressId}", id);
                return StatusCode(500, new { message = "Error retrieving address" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateUserAddressDto request)
        {
            try
            {
                var (isValid, errors) = UserAddressValidator.ValidateCreate(
                    request.AddressLine, request.City, request.PostalCode, request.Label,
                    request.Latitude, request.Longitude);

                if (!isValid)
                    return BadRequest(new { success = false, message = "Validation failed", errors });

                var userId = GetUserId();
                var address = await _userAddressService.CreateAsync(userId, request);

                return CreatedAtAction(nameof(GetAddressById), new { id = address.Id },
                    new { success = true, data = address, message = "Address created successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating address");
                return StatusCode(500, new { message = "Error creating address" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateUserAddressDto request)
        {
            try
            {
                var (isValid, errors) = UserAddressValidator.ValidateUpdate(
                    request.AddressLine, request.City, request.PostalCode, request.Label,
                    request.Latitude, request.Longitude);

                if (!isValid)
                    return BadRequest(new { success = false, message = "Validation failed", errors });

                var userId = GetUserId();
                var address = await _userAddressService.UpdateAsync(userId, id, request);

                return Ok(new { success = true, data = address, message = "Address updated successfully" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Address not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address {AddressId}", id);
                return StatusCode(500, new { message = "Error updating address" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _userAddressService.DeleteAsync(userId, id);

                if (!result)
                    return NotFound(new { message = "Address not found" });

                return Ok(new { success = true, message = "Address deleted successfully" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Address not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address {AddressId}", id);
                return StatusCode(500, new { message = "Error deleting address" });
            }
        }

        [HttpPost("{id}/set-primary")]
        public async Task<IActionResult> SetPrimary(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var address = await _userAddressService.SetPrimaryAsync(userId, id);

                return Ok(new { success = true, data = address, message = "Address set as primary successfully" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Address not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary address {AddressId}", id);
                return StatusCode(500, new { message = "Error setting primary address" });
            }
        }
    }
}

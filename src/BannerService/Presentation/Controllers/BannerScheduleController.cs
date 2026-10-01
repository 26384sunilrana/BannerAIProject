namespace BannerService.Presentation.Controllers;

using System.Security.Claims;
using Application.Dto;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/banners")]
[Authorize]
public class BannerScheduleController : ControllerBase
{
    private readonly BannerScheduleAppService _service;

    public BannerScheduleController(BannerScheduleAppService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    private Guid GetShopId()
    {
        var claim = User.FindFirst("shop_id")?.Value;
        if (!Guid.TryParse(claim, out var shopId))
            throw new UnauthorizedAccessException("Invalid or missing shop_id");
        return shopId;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user_id");
        return userId;
    }

    /// <summary>Sets when a banner is shown. Overlapping windows (including same-day hours) are rejected.</summary>
    [HttpPut("{bannerId}/schedule")]
    [ProducesResponseType(typeof(BannerScheduleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetSchedule(Guid bannerId, [FromBody] SetBannerScheduleRequestDto request)
    {
        try
        {
            var userName = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "user";
            var result = await _service.SetScheduleAsync(bannerId, GetShopId(), GetUserId(), userName, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>The banner live right now, or useDefaultBanner=true when the local default should be shown.</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ActiveBannerResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        return Ok(await _service.GetActiveBannerAsync(GetShopId(), DateTime.UtcNow));
    }
}

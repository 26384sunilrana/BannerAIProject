namespace BannerService.Presentation.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BannerService.Application.Dto;
using BannerService.Application.Services;
using System.Security.Claims;

[ApiController]
[Route("api/banners/{bannerId}/versions")]
[Authorize]
public class VersionControlController : ControllerBase
{
    private readonly IVersionControlService _versionControlService;
    private readonly ILogger<VersionControlController> _logger;

    public VersionControlController(
        IVersionControlService versionControlService,
        ILogger<VersionControlController> logger)
    {
        _versionControlService = versionControlService;
        _logger = logger;
    }

    private Guid GetShopId()
    {
        var shopIdClaim = User.FindFirst("shop_id")?.Value;
        if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out var shopId))
            throw new UnauthorizedAccessException("Invalid or missing shop_id");
        return shopId;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user ID");
        return userId;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BannerVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListVersions(Guid bannerId)
    {
        try
        {
            var shopId = GetShopId();
            var versions = await _versionControlService.ListVersionsAsync(bannerId, shopId);
            return Ok(versions);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized();
        }
    }

    [HttpGet("{versionNumber}")]
    [ProducesResponseType(typeof(BannerVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(Guid bannerId, int versionNumber)
    {
        try
        {
            if (versionNumber <= 0)
                return BadRequest("Version number must be greater than 0");

            var shopId = GetShopId();
            var version = await _versionControlService.GetVersionDetailAsync(bannerId, versionNumber, shopId);
            if (version == null)
                return NotFound();
            return Ok(version);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized();
        }
    }

    [HttpPost("{versionNumber}/restore")]
    [ProducesResponseType(typeof(BannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RestoreVersion(Guid bannerId, int versionNumber)
    {
        try
        {
            if (versionNumber <= 0)
                return BadRequest("Version number must be greater than 0");

            var shopId = GetShopId();
            var userId = GetUserId();
            var banner = await _versionControlService.RestoreVersionAsync(bannerId, versionNumber, shopId, userId);
            return Ok(BannerDto.FromBanner(banner));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Banner or version not found");
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request");
            return BadRequest(new { message = ex.Message });
        }
    }
}

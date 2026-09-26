namespace BannerService.Presentation.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Dto;
using Application.Services;
using System.Security.Claims;

[ApiController]
[Route("api/banners")]
[Authorize]
public class BannersController : ControllerBase
{
    private readonly IBannerService _bannerService;
    private readonly ILogger<BannersController> _logger;

    public BannersController(IBannerService bannerService, ILogger<BannersController> logger)
    {
        _bannerService = bannerService ?? throw new ArgumentNullException(nameof(bannerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user_id");
        return userId;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BannerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBanner([FromBody] CreateBannerRequestDto request)
    {
        var shopId = GetShopId();
        var userId = GetUserId();

        var banner = await _bannerService.CreateBannerAsync(shopId, userId, request);
        return CreatedAtAction(nameof(GetBanner), new { id = banner.Id }, banner);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BannerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBanner(Guid id)
    {
        var shopId = GetShopId();
        var banner = await _bannerService.GetBannerAsync(id, shopId);
        return Ok(banner);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BannerResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBanners([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var shopId = GetShopId();
        var banners = await _bannerService.ListBannersAsync(shopId, page, pageSize);
        return Ok(banners);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(BannerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateBanner(Guid id, [FromBody] CreateBannerRequestDto request)
    {
        var shopId = GetShopId();
        var banner = await _bannerService.UpdateBannerAsync(id, shopId, request);
        return Ok(banner);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteBanner(Guid id)
    {
        var shopId = GetShopId();
        var deleted = await _bannerService.DeleteBannerAsync(id, shopId);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpGet("{bannerId}/preview")]
    [ProducesResponseType(typeof(PreviewResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(Guid bannerId)
    {
        var shopId = GetShopId();
        var preview = await _bannerService.GetPreviewAsync(bannerId, shopId);
        return Ok(preview);
    }
}

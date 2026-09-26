namespace BannerService.Presentation.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BannerService.Application.Dto;
using BannerService.Application.Services;
using System.Security.Claims;

[ApiController]
[Route("api/banners/{bannerId}/carousels")]
[Authorize]
public class CarouselController : ControllerBase
{
    private readonly ICarouselService _carouselService;
    private readonly ILogger<CarouselController> _logger;

    public CarouselController(
        ICarouselService carouselService,
        ILogger<CarouselController> logger)
    {
        _carouselService = carouselService;
        _logger = logger;
    }

    private Guid GetShopId()
    {
        var shopIdClaim = User.FindFirst("shop_id")?.Value;
        if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out var shopId))
            throw new UnauthorizedAccessException("Invalid or missing shop_id");
        return shopId;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CarouselDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCarousel(
        Guid bannerId,
        [FromBody] CreateCarouselRequestDto request)
    {
        try
        {
            if (bannerId == Guid.Empty)
                return BadRequest("Invalid banner ID");
            if (request.ComponentIds == null || request.ComponentIds.Count < 2)
                return BadRequest("Carousel requires at least 2 components");
            if (request.IntervalMs < 1000 || request.IntervalMs > 30000)
                return BadRequest("IntervalMs must be 1000-30000");
            if (request.TransitionDuration < 200 || request.TransitionDuration > 2000)
                return BadRequest("TransitionDuration must be 200-2000");
            if (request.TransitionType < 1 || request.TransitionType > 4)
                return BadRequest("TransitionType must be 1-4");

            var shopId = GetShopId();
            var carousel = await _carouselService.CreateCarouselAsync(
                bannerId, shopId,
                request.IntervalMs, request.TransitionDuration,
                request.TransitionType, request.ComponentIds);

            return CreatedAtAction(nameof(GetCarousel), new { bannerId, carouselId = carousel.Id }, carousel);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found");
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CarouselDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListCarousels(Guid bannerId)
    {
        try
        {
            var shopId = GetShopId();
            var carousels = await _carouselService.GetBannerCarouselsAsync(bannerId, shopId);
            return Ok(carousels);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized();
        }
    }

    [HttpGet("{carouselId}")]
    [ProducesResponseType(typeof(CarouselDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCarousel(Guid bannerId, Guid carouselId)
    {
        try
        {
            var shopId = GetShopId();
            var carousel = await _carouselService.GetCarouselAsync(carouselId, shopId);
            if (carousel == null)
                return NotFound();
            return Ok(carousel);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized();
        }
    }

    [HttpPut("{carouselId}")]
    [ProducesResponseType(typeof(CarouselDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCarousel(
        Guid bannerId, Guid carouselId,
        [FromBody] CreateCarouselRequestDto request)
    {
        try
        {
            if (request.ComponentIds == null || request.ComponentIds.Count < 2)
                return BadRequest("Carousel requires at least 2 components");

            var shopId = GetShopId();
            var carousel = await _carouselService.UpdateCarouselAsync(
                carouselId, shopId,
                request.IntervalMs, request.TransitionDuration,
                request.TransitionType, request.ComponentIds);

            return Ok(carousel);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found");
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{carouselId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCarousel(Guid bannerId, Guid carouselId)
    {
        try
        {
            var shopId = GetShopId();
            await _carouselService.DeleteCarouselAsync(carouselId, shopId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized();
        }
    }
}

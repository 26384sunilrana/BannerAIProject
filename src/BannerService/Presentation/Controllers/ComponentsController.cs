namespace BannerService.Presentation.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Dto;
using Application.Services;
using System.Security.Claims;

[ApiController]
[Route("api/banners/{bannerId}/components")]
[Authorize]
public class ComponentsController : ControllerBase
{
    private readonly IBannerService _bannerService;
    private readonly ILogger<ComponentsController> _logger;

    public ComponentsController(IBannerService bannerService, ILogger<ComponentsController> logger)
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

    [HttpPost]
    [ProduceResponseType(typeof(ComponentResponseDto), StatusCodes.Status201Created)]
    [ProduceResponseType(StatusCodes.Status400BadRequest)]
    [ProduceResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComponent(Guid bannerId, [FromBody] AddComponentRequestDto request)
    {
        var shopId = GetShopId();
        var component = await _bannerService.AddComponentAsync(bannerId, shopId, request);
        return CreatedAtAction(nameof(UpdateComponent), new { bannerId, componentId = component.Id }, component);
    }

    [HttpPut("{componentId}")]
    [ProduceResponseType(typeof(ComponentResponseDto), StatusCodes.Status200OK)]
    [ProduceResponseType(StatusCodes.Status400BadRequest)]
    [ProduceResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateComponent(Guid bannerId, Guid componentId, [FromBody] AddComponentRequestDto request)
    {
        var shopId = GetShopId();
        var component = await _bannerService.UpdateComponentAsync(bannerId, componentId, shopId, request);
        return Ok(component);
    }

    [HttpDelete("{componentId}")]
    [ProduceResponseType(StatusCodes.Status204NoContent)]
    [ProduceResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveComponent(Guid bannerId, Guid componentId)
    {
        var shopId = GetShopId();
        var removed = await _bannerService.RemoveComponentAsync(bannerId, componentId, shopId);
        if (!removed)
            return NotFound();

        return NoContent();
    }
}

namespace BannerService.Presentation.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BannerService.Application.Dto;
using BannerService.Application.Services;
using System.Security.Claims;

[ApiController]
[Route("api/banners/{bannerId}/components/{componentId}/effects")]
[Authorize]
public class EffectsController : ControllerBase
{
    private readonly IEffectService _effectService;
    private readonly ILogger<EffectsController> _logger;

    public EffectsController(
        IEffectService effectService,
        ILogger<EffectsController> logger)
    {
        _effectService = effectService;
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
    [ProducesResponseType(typeof(EffectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyEffect(Guid bannerId, Guid componentId,
        [FromBody] ApplyEffectRequestDto request)
    {
        try
        {
            if (bannerId == Guid.Empty || componentId == Guid.Empty)
                return BadRequest("Invalid banner or component ID");
            if (request.EffectType < 1 || request.EffectType > 5)
                return BadRequest("Invalid effect type (must be 1-5)");
            if (request.Parameters == null || request.Parameters.Count == 0)
                return BadRequest("Effect parameters required");

            var shopId = GetShopId();
            var result = await _effectService.ApplyEffectAsync(bannerId, componentId, shopId,
                request.EffectType, request.Parameters);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Banner or component not found");
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid effect parameters");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<EffectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListEffects(Guid bannerId, Guid componentId)
    {
        try
        {
            var shopId = GetShopId();
            var effects = await _effectService.GetComponentEffectsAsync(bannerId, componentId, shopId);
            return Ok(effects);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Banner or component not found");
            return NotFound();
        }
    }

    [HttpPut("{effectId}")]
    [ProducesResponseType(typeof(EffectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEffect(Guid bannerId, Guid componentId, Guid effectId,
        [FromBody] UpdateEffectRequestDto request)
    {
        try
        {
            if (request.Parameters == null || request.Parameters.Count == 0)
                return BadRequest("Effect parameters required");

            var shopId = GetShopId();
            var result = await _effectService.UpdateEffectAsync(bannerId, componentId, effectId,
                request.Parameters, shopId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Effect not found");
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{effectId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEffect(Guid bannerId, Guid componentId, Guid effectId)
    {
        try
        {
            var shopId = GetShopId();
            await _effectService.RemoveEffectAsync(bannerId, componentId, effectId, shopId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Effect not found");
            return NotFound();
        }
    }
}

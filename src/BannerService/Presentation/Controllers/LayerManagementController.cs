namespace BannerService.Presentation.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Dto;
using Application.Services;
using System.Security.Claims;
using Domain.Services;
using Domain.Interfaces;

[ApiController]
[Route("api/banners/{bannerId}/components/{componentId}")]
[Authorize]
public class LayerManagementController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly LayerManagementService _layerManagementService;
    private readonly ILogger<LayerManagementController> _logger;

    public LayerManagementController(
        IUnitOfWork unitOfWork,
        LayerManagementService layerManagementService,
        ILogger<LayerManagementController> logger)
    {
        _unitOfWork = unitOfWork;
        _layerManagementService = layerManagementService;
        _logger = logger;
    }

    private Guid GetShopId()
    {
        var shopIdClaim = User.FindFirst("shop_id")?.Value;
        if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out var shopId))
            throw new UnauthorizedAccessException("Invalid or missing shop_id");
        return shopId;
    }

    [HttpPost("reorder")]
    [ProducesResponseType(typeof(LayerOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderComponent(
        Guid bannerId,
        Guid componentId,
        [FromBody] ReorderComponentRequestDto request)
    {
        var shopId = GetShopId();
        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            return NotFound();

        var layerOrder = _layerManagementService.ReorderComponent(
            banner,
            componentId,
            request.NewZIndex);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new LayerOrderDto
        {
            ComponentId = layerOrder.ComponentId,
            OldZIndex = layerOrder.OldZIndex,
            NewZIndex = layerOrder.NewZIndex
        });
    }

    [HttpPost("move-forward")]
    [ProducesResponseType(typeof(LayerOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MoveForward(Guid bannerId, Guid componentId)
    {
        var shopId = GetShopId();
        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            return NotFound();

        var layerOrder = _layerManagementService.MoveForward(banner, componentId);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new LayerOrderDto
        {
            ComponentId = layerOrder.ComponentId,
            OldZIndex = layerOrder.OldZIndex,
            NewZIndex = layerOrder.NewZIndex
        });
    }

    [HttpPost("move-backward")]
    [ProducesResponseType(typeof(LayerOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MoveBackward(Guid bannerId, Guid componentId)
    {
        var shopId = GetShopId();
        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            return NotFound();

        var layerOrder = _layerManagementService.MoveBackward(banner, componentId);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new LayerOrderDto
        {
            ComponentId = layerOrder.ComponentId,
            OldZIndex = layerOrder.OldZIndex,
            NewZIndex = layerOrder.NewZIndex
        });
    }

    [HttpPost("send-to-front")]
    [ProducesResponseType(typeof(LayerOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendToFront(Guid bannerId, Guid componentId)
    {
        var shopId = GetShopId();
        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            return NotFound();

        var layerOrder = _layerManagementService.SendToFront(banner, componentId);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new LayerOrderDto
        {
            ComponentId = layerOrder.ComponentId,
            OldZIndex = layerOrder.OldZIndex,
            NewZIndex = layerOrder.NewZIndex
        });
    }

    [HttpPost("send-to-back")]
    [ProducesResponseType(typeof(LayerOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendToBack(Guid bannerId, Guid componentId)
    {
        var shopId = GetShopId();
        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            return NotFound();

        var layerOrder = _layerManagementService.SendToBack(banner, componentId);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new LayerOrderDto
        {
            ComponentId = layerOrder.ComponentId,
            OldZIndex = layerOrder.OldZIndex,
            NewZIndex = layerOrder.NewZIndex
        });
    }
}

namespace BannerService.Presentation.Controllers;

using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class CreateShopAdRequest : ShopAdInput
{
    /// <summary>Only the administrator chooses the shop; owners and executives book on their own.</summary>
    public Guid? ShopId { get; set; }
}

public class DecideShopAdRequest
{
    public string? Note { get; set; }
    public string? Reason { get; set; }
}

/// <summary>Booking ads on shops screens: the administrator, shop owners and their sales executives.</summary>
[ApiController]
[Authorize(Roles = "Admin,ShopOwner,SalesExecutive")]
public class ShopAdsController : ControllerBase
{
    private readonly ShopAdService _ads;

    public ShopAdsController(ShopAdService ads)
    {
        _ads = ads;
    }

    private AdActor Actor()
    {
        var source = User.IsInRole("Admin") ? ShopAdSource.Admin : User.IsInRole("ShopOwner") ? ShopAdSource.ShopOwner : ShopAdSource.SalesExecutive;
        Guid? shopId = source == ShopAdSource.Admin ? null : User.GetShopId();
        return new AdActor(User.GetUserId(), User.GetUserName(), source, shopId);
    }

    [HttpGet("api/shop-ads")]
    public Task<IActionResult> List([FromQuery] Guid? shopId, [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        ServiceErrors.Run(this, () => _ads.ListAsync(Actor(), shopId, from, to));

    [HttpPost("api/shop-ads")]
    public Task<IActionResult> Create([FromBody] CreateShopAdRequest request) =>
        ServiceErrors.Run(this, () => _ads.CreateAsync(Actor(), request.ShopId, request));

    /// <summary>The administrator books one ad on every active shop in a place; shops it cannot go on are listed with the reason.</summary>
    [HttpPost("api/shop-ads/campaign")]
    [Authorize(Roles = "Admin")]
    public Task<IActionResult> Campaign([FromBody] CampaignInput request) =>
        ServiceErrors.Run(this, () => _ads.CampaignAsync(Actor(), request));

    [HttpPut("api/shop-ads/{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] ShopAdInput request) =>
        ServiceErrors.Run(this, () => _ads.UpdateAsync(Actor(), id, request));

    [HttpPost("api/shop-ads/{id:guid}/submit")]
    public Task<IActionResult> Submit(Guid id) => ServiceErrors.Run(this, () => _ads.SubmitAsync(Actor(), id));

    [HttpPost("api/shop-ads/{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, [FromBody] DecideShopAdRequest? request) =>
        ServiceErrors.Run(this, () => _ads.ApproveAsync(Actor(), id, request?.Note));

    [HttpPost("api/shop-ads/{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, [FromBody] DecideShopAdRequest? request) =>
        ServiceErrors.Run(this, () => _ads.RejectAsync(Actor(), id, request?.Reason));

    [HttpPost("api/shop-ads/{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id) => ServiceErrors.Run(this, () => _ads.CancelAsync(Actor(), id));

    /// <summary>The shop owner stops an administrator ad for a better local offer; the administrators are told.</summary>
    [HttpPost("api/shop-ads/{id:guid}/override")]
    public Task<IActionResult> Override(Guid id, [FromBody] DecideShopAdRequest? request) =>
        ServiceErrors.Run(this, () => _ads.OverrideAsync(Actor(), id, request?.Reason));

    [HttpGet("api/shop-ads/{id:guid}/history")]
    public Task<IActionResult> History(Guid id) => ServiceErrors.Run(this, () => _ads.HistoryAsync(Actor(), id));

}

/// <summary>What is on a shop's screen right now. Any signed-in member of the shop may read it, and so may the shop's own paired screen.</summary>
[ApiController]
[Authorize]
public class ShopAdsLiveController : ControllerBase
{
    private readonly ShopAdService _ads;

    public ShopAdsLiveController(ShopAdService ads)
    {
        _ads = ads;
    }

    [HttpGet("api/shops/{shopId:guid}/ads/live")]
    public Task<IActionResult> Live(Guid shopId) => ServiceErrors.Run(this, () => _ads.LiveAsync(shopId));
}

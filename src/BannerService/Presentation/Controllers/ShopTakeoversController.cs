namespace BannerService.Presentation.Controllers;

using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class AskTakeoverRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
}

public class AnswerTakeoverRequest
{
    /// <summary>Keep or Replace.</summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public TakeoverAssociates? Associates { get; set; }
    public string? Password { get; set; }
    public string? Note { get; set; }
}

/// <summary>A shop changing hands: the new owner asks, the old owner (or an administrator) answers.</summary>
[ApiController]
[Route("api/shop-takeovers")]
[Authorize(Roles = "Admin,ShopOwner")]
public class ShopTakeoversController : ControllerBase
{
    private readonly ShopTakeoverService _takeovers;

    public ShopTakeoversController(ShopTakeoverService takeovers)
    {
        _takeovers = takeovers;
    }

    private bool IsAdmin => User.IsInRole("Admin");
    private string UserId => User.GetUserId().ToString();
    private Guid OwnShop => User.GetShopId();

    [HttpGet]
    public Task<IActionResult> List() => ServiceErrors.Run(this, () => _takeovers.ListAsync(UserId, IsAdmin ? null : OwnShop, IsAdmin));

    [HttpPost]
    [Authorize(Roles = "ShopOwner")]
    public Task<IActionResult> Ask([FromBody] AskTakeoverRequest request) =>
        ServiceErrors.Run(this, () => _takeovers.RequestAsync(OwnShop, UserId, request.Name, request.Address, request.PostalCode));

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "ShopOwner")]
    public Task<IActionResult> Confirm(Guid id, [FromBody] AnswerTakeoverRequest request) =>
        ServiceErrors.Run(this, () => _takeovers.ConfirmAsync(UserId, OwnShop, id, request.Associates, request.Password));

    [HttpPost("{id:guid}/confirm-as-admin")]
    [Authorize(Roles = "Admin")]
    public Task<IActionResult> ConfirmAsAdmin(Guid id, [FromBody] AnswerTakeoverRequest request) =>
        ServiceErrors.Run(this, () => _takeovers.ConfirmAsAdminAsync(UserId, User.GetUserName(), id, request.Associates, request.Note));

    [HttpPost("{id:guid}/decline")]
    public Task<IActionResult> Decline(Guid id, [FromBody] AnswerTakeoverRequest? request) =>
        ServiceErrors.Run(this, () => _takeovers.DeclineAsync(UserId, IsAdmin ? Guid.Empty : OwnShop, id, request?.Note, IsAdmin, User.GetUserName()));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "ShopOwner")]
    public Task<IActionResult> Cancel(Guid id) => ServiceErrors.Run(this, () => _takeovers.CancelAsync(UserId, OwnShop, id));
}

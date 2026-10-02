namespace BannerService.Presentation.Controllers;

using System.Security.Claims;
using Application.DTOs;
using Application.Services;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class UpdateAccountRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class TransferOwnershipRequest
{
    public string NewOwnerUserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AssignOwnerRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class MoveUserRequest
{
    public Guid ShopId { get; set; }
}

/// <summary>Maps what the services throw to the status codes the web app expects.</summary>
internal static class ServiceErrors
{
    public static async Task<IActionResult> Run<T>(ControllerBase controller, Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return result == null ? controller.NoContent() : controller.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return controller.NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return controller.StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return controller.BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return controller.Conflict(new { message = ex.Message });
        }
    }
}

/// <summary>A signed-in person's own details and password.</summary>
[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly AccountService _account;
    private readonly IRefreshCookie _cookie;

    public AccountController(AccountService account, IRefreshCookie cookie)
    {
        _account = account;
        _cookie = cookie;
    }

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    public Task<IActionResult> Get() => ServiceErrors.Run(this, () => _account.GetAsync(UserId));

    [HttpPut]
    public Task<IActionResult> Update([FromBody] UpdateAccountRequest request) =>
        ServiceErrors.Run(this, () => _account.UpdateAsync(UserId, request.FirstName, request.LastName, request.PhoneNumber));

    /// <summary>Changes the password, ends every other session and answers with a new one for this device.</summary>
    [HttpPost("change-password")]
    public Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request) =>
        ServiceErrors.Run(this, async () =>
        {
            var tokens = await _account.ChangePasswordAsync(
                UserId, request.CurrentPassword, request.NewPassword, request.ConfirmPassword,
                HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());

            if (_cookie.Enabled)
            {
                _cookie.Write(Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
                tokens.RefreshToken = string.Empty;
            }
            return new { message = "Your password was changed. Other devices were signed out.", tokens };
        });

    /// <summary>Ends every session on every device, this one included.</summary>
    [HttpPost("sign-out-everywhere")]
    public Task<IActionResult> SignOutEverywhere() =>
        ServiceErrors.Run(this, async () =>
        {
            var ended = await _account.SignOutEverywhereAsync(UserId);
            if (_cookie.Enabled) _cookie.Clear(Response);
            return new { message = "You were signed out everywhere.", sessionsEnded = ended };
        });
}

/// <summary>The owner hands the shop to one of the shop's sales executives.</summary>
[ApiController]
[Route("api/shops/{shopId:guid}/team")]
[Authorize(Roles = "ShopOwner")]
public class OwnershipController : ControllerBase
{
    private readonly ShopMembershipService _membership;

    public OwnershipController(ShopMembershipService membership)
    {
        _membership = membership;
    }

    [HttpPost("transfer-ownership")]
    public Task<IActionResult> Transfer(Guid shopId, [FromBody] TransferOwnershipRequest request) =>
        ServiceErrors.Run(this, () => _membership.TransferOwnershipAsync(
            shopId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty, request.NewOwnerUserId, request.Password));
}

/// <summary>Administrator tools for who owns a shop and which shop a sales executive belongs to.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminMembershipController : ControllerBase
{
    private readonly ShopMembershipService _membership;

    public AdminMembershipController(ShopMembershipService membership)
    {
        _membership = membership;
    }

    [HttpPost("shops/{shopId:guid}/owner")]
    public Task<IActionResult> AssignOwner(Guid shopId, [FromBody] AssignOwnerRequest request) =>
        ServiceErrors.Run(this, () => _membership.AssignOwnerAsAdminAsync(shopId, request.UserId));

    [HttpPost("users/{userId}/move")]
    public Task<IActionResult> Move(string userId, [FromBody] MoveUserRequest request) =>
        ServiceErrors.Run(this, () => _membership.MoveExecutiveAsync(userId, request.ShopId));
}

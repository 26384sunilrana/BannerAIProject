namespace BannerService.Presentation.Controllers;

using System.Security.Claims;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class PairingPollRequest
{
    public string? DeviceSecret { get; set; }
}

public class PairScreenRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
}

public class RenameScreenRequest
{
    public string? Name { get; set; }
}

/// <summary>What a screen (a television in a shop) does to pair itself and to report in. None of it needs a person's login.</summary>
[ApiController]
[Route("api/screens")]
public class ScreenDeviceController : ControllerBase
{
    public const string TokenHeader = "X-Screen-Token";

    private readonly ScreenService _screens;

    public ScreenDeviceController(ScreenService screens)
    {
        _screens = screens;
    }

    /// <summary>A screen that is not (or no longer) paired is told 401, so it knows to start pairing again.</summary>
    private async Task<IActionResult> Device<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
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

    /// <summary>A screen that is not paired asks for a code to show.</summary>
    [HttpPost("pairing/start")]
    [AllowAnonymous]
    public Task<IActionResult> Start() => ServiceErrors.Run(this, () => _screens.StartPairingAsync());

    /// <summary>The screen asks whether an owner has typed its code in yet.</summary>
    [HttpPost("pairing/poll")]
    [AllowAnonymous]
    public Task<IActionResult> Poll([FromBody] PairingPollRequest request) => ServiceErrors.Run(this, () => _screens.PollPairingAsync(request.DeviceSecret));

    /// <summary>The screen shows its secret (in the X-Screen-Token header) and gets an access token that lasts an hour.</summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public Task<IActionResult> Token() =>
        Device(() => _screens.ExchangeTokenAsync(Request.Headers[TokenHeader].ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()));

    /// <summary>What is on the screen now. Sent about once a minute by the screen itself.</summary>
    [HttpPost("heartbeat")]
    [Authorize(Roles = "Screen")]
    public Task<IActionResult> Heartbeat([FromBody] HeartbeatInput input) =>
        Device(async () =>
        {
            var screenId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shopId = Guid.Parse(User.FindFirstValue("shop_id")!);
            await _screens.HeartbeatAsync(screenId, shopId, input, HttpContext.Connection.RemoteIpAddress?.ToString());
            return new { serverTime = DateTime.UtcNow };
        });
}

/// <summary>The owner adds, renames and removes the shop's screen and reads what it played.</summary>
[ApiController]
[Route("api/shops/{shopId:guid}/screens")]
[Authorize(Roles = "ShopOwner,SalesExecutive,Admin")]
public class ShopScreensController : ControllerBase
{
    private readonly ScreenService _screens;

    public ShopScreensController(ScreenService screens)
    {
        _screens = screens;
    }

    [HttpGet]
    public Task<IActionResult> List(Guid shopId) => ServiceErrors.Run(this, () => _screens.ListAsync(shopId));

    /// <summary>Type the six characters the screen shows to make it this shop's screen.</summary>
    [HttpPost("pair")]
    [Authorize(Roles = "ShopOwner")]
    public Task<IActionResult> Pair(Guid shopId, [FromBody] PairScreenRequest request) =>
        ServiceErrors.Run(this, () => _screens.ClaimAsync(shopId, User.GetUserId(), request.Code, request.Name));

    [HttpPut("{screenId:guid}")]
    [Authorize(Roles = "ShopOwner")]
    public Task<IActionResult> Rename(Guid shopId, Guid screenId, [FromBody] RenameScreenRequest request) =>
        ServiceErrors.Run(this, () => _screens.RenameAsync(shopId, screenId, request.Name));

    [HttpDelete("{screenId:guid}")]
    [Authorize(Roles = "ShopOwner")]
    public Task<IActionResult> Remove(Guid shopId, Guid screenId) =>
        ServiceErrors.Run(this, async () =>
        {
            await _screens.RevokeAsync(shopId, screenId);
            return new { message = "The screen was removed." };
        });

    /// <summary>Hours each banner, ad and the default board were on the screen, and the hours per day, for the last days.</summary>
    [HttpGet("report")]
    public Task<IActionResult> Report(Guid shopId, [FromQuery] int days = 7) => ServiceErrors.Run(this, () => _screens.ReportAsync(shopId, days));
}

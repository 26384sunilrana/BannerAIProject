namespace BannerService.Presentation.Controllers;

using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class SetTimeZoneRequest
{
    /// <summary>An IANA name such as Asia/Kolkata; null or empty to follow the city and country again.</summary>
    public string? TimeZoneId { get; set; }
}

public class SetDefaultBoardRequest
{
    public string? Message { get; set; }
    public string? Background { get; set; }
    public string? TextColor { get; set; }
    public Guid? LogoMediaFileId { get; set; }
}

/// <summary>A shop's time zone and the default board its screen shows when no banner is live.</summary>
[ApiController]
[Route("api/shops/{shopId:guid}")]
[Authorize]
public class ShopSettingsController : ControllerBase
{
    private readonly ShopSettingsService _settings;

    public ShopSettingsController(ShopSettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet("time-zone")]
    public Task<IActionResult> GetTimeZone(Guid shopId) => ServiceErrors.Run(this, () => _settings.GetTimeZoneAsync(shopId));

    [HttpPut("time-zone")]
    [Authorize(Roles = "Admin,ShopOwner")]
    public Task<IActionResult> SetTimeZone(Guid shopId, [FromBody] SetTimeZoneRequest request) =>
        ServiceErrors.Run(this, () => _settings.SetTimeZoneAsync(shopId, request.TimeZoneId));

    /// <summary>Any login of the shop can read it: the shop screen needs it.</summary>
    [HttpGet("default-board")]
    public Task<IActionResult> GetDefaultBoard(Guid shopId) => ServiceErrors.Run(this, () => _settings.GetDefaultBoardAsync(shopId));

    [HttpPut("default-board")]
    [Authorize(Roles = "Admin,ShopOwner")]
    public Task<IActionResult> SetDefaultBoard(Guid shopId, [FromBody] SetDefaultBoardRequest request) =>
        ServiceErrors.Run(this, () => _settings.SetDefaultBoardAsync(shopId, request.Message, request.Background, request.TextColor, request.LogoMediaFileId));
}

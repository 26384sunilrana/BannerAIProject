namespace BannerService.Application.Services;

using System.Text.RegularExpressions;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;
using Infrastructure.Security;

public class DefaultBoardDto
{
    public string? Message { get; set; }
    /// <summary>Background colour as #rrggbb; null means the standard dark blue.</summary>
    public string? Background { get; set; }
    public string? TextColor { get; set; }
    public Guid? LogoMediaFileId { get; set; }
    /// <summary>A link the shop screen can load for the logo (it expires after four hours).</summary>
    public string? LogoUrl { get; set; }
    public string ShopName { get; set; } = string.Empty;
}

public class ShopTimeZoneDto
{
    /// <summary>The time zone in use, whichever level it came from.</summary>
    public string TimeZoneId { get; set; } = ShopTimeZone.Fallback;
    /// <summary>Where it came from: shop, city, country or default.</summary>
    public string Source { get; set; } = "default";
    /// <summary>The shop's own setting; null when it follows its city or country.</summary>
    public string? OwnTimeZoneId { get; set; }
}

/// <summary>Settings a shop owner keeps for the shop itself: its time zone and the default board its screen shows when no banner is live.</summary>
public class ShopSettingsService
{
    public const int MaxMessageLength = 200;
    private const int LinkMinutes = 240;
    private static readonly Regex Colour = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    private readonly IShopRepository _shops;
    private readonly IMediaFileRepository _media;
    private readonly IMediaUrlSigner _signer;

    public ShopSettingsService(IShopRepository shops, IMediaFileRepository media, IMediaUrlSigner signer)
    {
        _shops = shops;
        _media = media;
        _signer = signer;
    }

    // ----- time zone

    public async Task<ShopTimeZoneDto> GetTimeZoneAsync(Guid shopId)
    {
        var shop = await LoadAsync(shopId);
        return ToDto(shop);
    }

    /// <summary>Sets the shop's own time zone, or clears it (null) so the shop follows its city and country again.</summary>
    public async Task<ShopTimeZoneDto> SetTimeZoneAsync(Guid shopId, string? timeZoneId)
    {
        var shop = await LoadAsync(shopId);

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            shop.TimeZoneId = null;
        }
        else
        {
            if (!TimeZones.TryGet(timeZoneId, out _))
                throw new ArgumentException("That time zone is not known on this server. Use a name like Asia/Kolkata or Europe/London.");
            shop.TimeZoneId = timeZoneId.Trim();
        }

        shop.UpdatedAt = DateTime.UtcNow;
        await _shops.UpdateAsync(shop);
        return ToDto(shop);
    }

    private static ShopTimeZoneDto ToDto(Shop shop)
    {
        var resolved = ShopTimeZone.Resolve(shop);
        return new ShopTimeZoneDto { TimeZoneId = resolved.Id, Source = resolved.Source, OwnTimeZoneId = shop.TimeZoneId };
    }

    // ----- default board

    public async Task<DefaultBoardDto> GetDefaultBoardAsync(Guid shopId)
    {
        var shop = await LoadAsync(shopId);
        return await ToBoardAsync(shop);
    }

    public async Task<DefaultBoardDto> SetDefaultBoardAsync(
        Guid shopId, string? message, string? background, string? textColor, Guid? logoMediaFileId)
    {
        var shop = await LoadAsync(shopId);

        message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        if (message is { Length: > MaxMessageLength })
            throw new ArgumentException($"The message can have up to {MaxMessageLength} characters.");
        if (message != null && message.Any(c => char.IsControl(c) && c != '\n'))
            throw new ArgumentException("The message has characters that are not allowed.");

        background = string.IsNullOrWhiteSpace(background) ? null : background.Trim().ToLowerInvariant();
        textColor = string.IsNullOrWhiteSpace(textColor) ? null : textColor.Trim().ToLowerInvariant();
        if (background != null && !Colour.IsMatch(background))
            throw new ArgumentException("The background colour looks like #1e3a8a.");
        if (textColor != null && !Colour.IsMatch(textColor))
            throw new ArgumentException("The text colour looks like #ffffff.");

        if (logoMediaFileId.HasValue)
        {
            var file = await _media.GetByIdAsync(logoMediaFileId.Value, shopId);
            if (file == null || file.Status != (int)MediaFileStatus.Active)
                throw new ArgumentException("The logo must be one of your uploaded pictures.");
            if (file.FileType != (int)MediaFileType.Image)
                throw new ArgumentException("The logo must be a picture, not a video.");
        }

        shop.DefaultBoardMessage = message;
        shop.DefaultBoardBackground = background;
        shop.DefaultBoardTextColor = textColor;
        shop.DefaultBoardLogoMediaId = logoMediaFileId;
        shop.UpdatedAt = DateTime.UtcNow;
        await _shops.UpdateAsync(shop);

        return await ToBoardAsync(shop);
    }

    private async Task<DefaultBoardDto> ToBoardAsync(Shop shop)
    {
        string? logoUrl = null;
        if (shop.DefaultBoardLogoMediaId is { } logoId)
        {
            // a logo that was deleted since simply no longer shows
            var file = await _media.GetByIdAsync(logoId, shop.Id);
            if (file is { Status: (int)MediaFileStatus.Active })
            {
                var expires = DateTime.UtcNow.AddMinutes(LinkMinutes);
                logoUrl = $"/api/media/{logoId}/download?expires={new DateTimeOffset(expires).ToUnixTimeSeconds()}&sig={_signer.Sign(logoId, expires)}";
            }
        }

        return new DefaultBoardDto
        {
            Message = shop.DefaultBoardMessage,
            Background = shop.DefaultBoardBackground,
            TextColor = shop.DefaultBoardTextColor,
            LogoMediaFileId = shop.DefaultBoardLogoMediaId,
            LogoUrl = logoUrl,
            ShopName = shop.Name,
        };
    }

    private async Task<Shop> LoadAsync(Guid shopId) =>
        await _shops.GetByIdAsync(shopId) ?? throw new KeyNotFoundException("Shop not found");
}

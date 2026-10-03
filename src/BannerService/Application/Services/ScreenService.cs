namespace BannerService.Application.Services;

using System.Security.Cryptography;
using System.Text;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;

public class PairingStartDto
{
    /// <summary>Shown on the screen, for the owner to type in.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The device keeps this and never shows it. It is the screen's own key.</summary>
    public string DeviceSecret { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class PairingPollDto
{
    /// <summary>waiting, paired or expired.</summary>
    public string Status { get; set; } = string.Empty;
    public string? ShopName { get; set; }
}

public class ScreenTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
}

public class ScreenDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Online { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? AppVersion { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class HeartbeatAd
{
    public Guid Id { get; set; }
}

public class HeartbeatInput
{
    public Guid? BannerId { get; set; }
    public List<HeartbeatAd> Ads { get; set; } = new();
    public bool DefaultBoard { get; set; }
    public string? AppVersion { get; set; }
}

public class PlayRowDto
{
    public string Kind { get; set; } = string.Empty;
    public Guid? RefId { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Hours { get; set; }
}

public class PlayDayDto
{
    public DateTime Day { get; set; }
    public decimal Hours { get; set; }
}

public class PlayReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<PlayRowDto> Rows { get; set; } = new();
    public List<PlayDayDto> Days { get; set; } = new();
}

/// <summary>
/// The screens in a shop: pairing a new one with a code the owner types in, giving it a token that may only read what a screen needs, counting what it
/// plays (proof of play) and noticing when it goes quiet. One screen per shop for now.
/// </summary>
public class ScreenService
{
    public const int MaxScreensPerShop = 1;
    public static readonly TimeSpan PairingLifetime = TimeSpan.FromMinutes(15);

    /// <summary>A screen that has not been heard from for this long is shown as offline.</summary>
    public static readonly TimeSpan OnlineWindow = TimeSpan.FromMinutes(3);

    /// <summary>The most one heartbeat can count: a screen that was off for an hour does not get an hour of play.</summary>
    public static readonly TimeSpan MaxCredit = TimeSpan.FromSeconds(150);

    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly IScreenRepository _screens;
    private readonly IShopRepository _shops;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShopAdRepository _ads;
    private readonly IJwtTokenService _tokens;

    public ScreenService(IScreenRepository screens, IShopRepository shops, IUnitOfWork unitOfWork, IShopAdRepository ads, IJwtTokenService tokens)
    {
        _screens = screens;
        _shops = shops;
        _unitOfWork = unitOfWork;
        _ads = ads;
        _tokens = tokens;
    }

    // ----- pairing

    public async Task<PairingStartDto> StartPairingAsync(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var code = NewCode();
            if (await _screens.FindPairingByCodeAsync(code, now) != null) continue;

            var pairing = new ScreenPairing { Code = code, DeviceSecretHash = Hash(secret), CreatedAt = now, ExpiresAt = now + PairingLifetime };
            await _screens.AddPairingAsync(pairing);
            return new PairingStartDto { Code = code, DeviceSecret = secret, ExpiresAt = pairing.ExpiresAt };
        }
        throw new InvalidOperationException("Could not make a pairing code. Try again.");
    }

    public async Task<PairingPollDto> PollPairingAsync(string? deviceSecret, DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(deviceSecret)) return new PairingPollDto { Status = "expired" };
        var now = nowUtc ?? DateTime.UtcNow;

        var pairing = await _screens.FindPairingBySecretHashAsync(Hash(deviceSecret));
        if (pairing == null)
        {
            // already trading its secret for tokens: it was paired and the pairing note has been cleaned up
            var known = await _screens.FindBySecretHashAsync(Hash(deviceSecret));
            if (known is { Status: ScreenStatus.Active })
                return new PairingPollDto { Status = "paired", ShopName = (await _shops.GetByIdAsync(known.ShopId))?.Name };
            return new PairingPollDto { Status = "expired" };
        }

        if (pairing.ScreenId != null)
        {
            var screen = await _screens.GetByIdAsync(pairing.ScreenId.Value);
            return new PairingPollDto { Status = "paired", ShopName = screen == null ? null : (await _shops.GetByIdAsync(screen.ShopId))?.Name };
        }
        return new PairingPollDto { Status = pairing.ExpiresAt <= now ? "expired" : "waiting" };
    }

    /// <summary>The owner types the code shown on the screen: the screen becomes the shop's.</summary>
    public async Task<ScreenDto> ClaimAsync(Guid shopId, Guid userId, string? code, string? name, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var shop = await _shops.GetByIdAsync(shopId) ?? throw new KeyNotFoundException("Shop not found.");
        if (shop.Status != ShopStatus.Active) throw new InvalidOperationException("The shop is not active.");

        var clean = new string((code ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (clean.Length != 6) throw new ArgumentException("Type the six characters shown on the screen.");
        name = string.IsNullOrWhiteSpace(name) ? "Shop screen" : name.Trim();
        if (name.Length > 60) throw new ArgumentException("The name can be up to 60 characters.");

        var pairing = await _screens.FindPairingByCodeAsync(clean, now)
            ?? throw new KeyNotFoundException("That code is not right, or it has expired. Check the screen: it shows a new code every 15 minutes.");

        if (await _screens.CountActiveForShopAsync(shopId) >= MaxScreensPerShop)
            throw new InvalidOperationException($"Your shop already has {MaxScreensPerShop} screen. Remove it first to add another.");

        var screen = new Screen { ShopId = shopId, Name = name, DeviceSecretHash = pairing.DeviceSecretHash, PairedByUserId = userId, CreatedAt = now };
        await _screens.AddAsync(screen);
        pairing.ScreenId = screen.Id;
        await _screens.SaveAsync();
        return ToDto(screen, now);
    }

    // ----- the owner's view

    public async Task<List<ScreenDto>> ListAsync(Guid shopId, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        return (await _screens.ListForShopAsync(shopId)).Where(s => s.Status == ScreenStatus.Active).Select(s => ToDto(s, now)).ToList();
    }

    public async Task<ScreenDto> RenameAsync(Guid shopId, Guid screenId, string? name)
    {
        var screen = await OwnedAsync(shopId, screenId);
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 60) throw new ArgumentException("Give the screen a name of up to 60 characters.");
        screen.Name = name.Trim();
        await _screens.SaveAsync();
        return ToDto(screen, DateTime.UtcNow);
    }

    /// <summary>Takes the screen away: its key stops working at once (the token it already holds ends within the hour).</summary>
    public async Task RevokeAsync(Guid shopId, Guid screenId)
    {
        var screen = await OwnedAsync(shopId, screenId);
        screen.Status = ScreenStatus.Revoked;
        screen.RevokedAt = DateTime.UtcNow;
        await _screens.SaveAsync();
    }

    public async Task<PlayReportDto> ReportAsync(Guid shopId, int days, DateTime? nowUtc = null)
    {
        days = Math.Clamp(days, 1, 90);
        var to = (nowUtc ?? DateTime.UtcNow).Date;
        var from = to.AddDays(-(days - 1));
        var stats = await _screens.ListStatsAsync(shopId, from, to);

        return new PlayReportDto
        {
            From = from,
            To = to,
            Rows = stats.GroupBy(s => (s.Kind, s.RefId))
                .Select(g => new PlayRowDto
                {
                    Kind = g.Key.Kind.ToString(),
                    RefId = g.Key.Kind == PlayKind.DefaultBoard ? null : g.Key.RefId,
                    Label = g.OrderByDescending(x => x.Day).First().Label,
                    Hours = Hours(g.Sum(x => x.Seconds)),
                })
                .OrderBy(r => r.Kind == nameof(PlayKind.DefaultBoard) ? 1 : 0).ThenByDescending(r => r.Hours).ToList(),
            Days = Enumerable.Range(0, days).Select(i => from.AddDays(i))
                .Select(day => new PlayDayDto { Day = day, Hours = Hours(stats.Where(s => s.Day == day && s.Kind != PlayKind.Ad).Sum(s => s.Seconds)) }).ToList(),
        };
    }

    // ----- the device

    /// <summary>The screen shows its secret and gets an access token for an hour. A removed screen is refused.</summary>
    public async Task<ScreenTokenDto> ExchangeTokenAsync(string? deviceSecret, string? ip, string? userAgent, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var screen = string.IsNullOrWhiteSpace(deviceSecret) ? null : await _screens.FindBySecretHashAsync(Hash(deviceSecret));
        if (screen == null || screen.Status != ScreenStatus.Active)
            throw new UnauthorizedAccessException("This screen is not paired (any more). Pair it again.");

        var shop = await _shops.GetByIdAsync(screen.ShopId) ?? throw new UnauthorizedAccessException("The shop of this screen no longer exists.");
        screen.LastSeenAt = now;
        screen.LastSeenIp = ip;
        screen.UserAgent = userAgent is { Length: > 300 } ? userAgent[..300] : userAgent;
        await _screens.SaveAsync();

        return new ScreenTokenDto
        {
            AccessToken = _tokens.GenerateScreenToken(screen.Id, screen.ShopId, screen.Name),
            ExpiresIn = 3600,
            ShopId = shop.Id,
            ShopName = shop.Name,
        };
    }

    /// <summary>What is on the screen right now. The time since the last heartbeat is counted as played for each thing that is on it.</summary>
    public async Task HeartbeatAsync(Guid screenId, Guid shopId, HeartbeatInput input, string? ip, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var screen = await _screens.GetByIdAsync(screenId);
        if (screen == null || screen.ShopId != shopId || screen.Status != ScreenStatus.Active)
            throw new UnauthorizedAccessException("This screen is not paired any more.");

        var credit = screen.LastHeartbeatAt == null ? TimeSpan.Zero : Min(now - screen.LastHeartbeatAt.Value, MaxCredit);
        if (credit < TimeSpan.Zero) credit = TimeSpan.Zero;
        var seconds = (int)Math.Round(credit.TotalSeconds);

        screen.LastSeenAt = now;
        screen.LastHeartbeatAt = now;
        screen.LastSeenIp = ip;
        screen.AppVersion = input.AppVersion is { Length: > 40 } ? input.AppVersion[..40] : input.AppVersion;
        await _screens.SaveAsync();
        if (seconds <= 0) return;

        // names come from the database, not from the device
        if (input.BannerId is { } bannerId)
        {
            var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
            if (banner != null) await _screens.AddSecondsAsync(screenId, shopId, now, PlayKind.Banner, bannerId, Clip(banner.Name), seconds);
        }
        foreach (var item in input.Ads.Select(a => a.Id).Distinct().Take(10))
        {
            var ad = await _ads.GetByIdAsync(item);
            if (ad != null && ad.ShopId == shopId) await _screens.AddSecondsAsync(screenId, shopId, now, PlayKind.Ad, ad.Id, Clip(ad.Headline), seconds);
        }
        if (input.DefaultBoard) await _screens.AddSecondsAsync(screenId, shopId, now, PlayKind.DefaultBoard, Guid.Empty, "Default board", seconds);
    }

    // ----- helpers

    private async Task<Screen> OwnedAsync(Guid shopId, Guid screenId)
    {
        var screen = await _screens.GetByIdAsync(screenId);
        if (screen == null || screen.ShopId != shopId || screen.Status != ScreenStatus.Active) throw new KeyNotFoundException("Screen not found.");
        return screen;
    }

    public static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private static string NewCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(6);
        return new string(bytes.Select(b => CodeAlphabet[b % CodeAlphabet.Length]).ToArray());
    }

    private static ScreenDto ToDto(Screen s, DateTime now) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Online = s.LastSeenAt != null && now - s.LastSeenAt.Value <= OnlineWindow,
        CreatedAt = DateTime.SpecifyKind(s.CreatedAt, DateTimeKind.Utc),
        LastSeenAt = s.LastSeenAt == null ? null : DateTime.SpecifyKind(s.LastSeenAt.Value, DateTimeKind.Utc),
        AppVersion = s.AppVersion,
        Status = s.Status.ToString(),
    };

    private static decimal Hours(int seconds) => Math.Round(seconds / 3600m, 2, MidpointRounding.AwayFromZero);
    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a <= b ? a : b;
    private static string Clip(string text) => text.Length <= 80 ? text : text[..80];
}

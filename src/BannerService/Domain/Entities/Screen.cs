namespace BannerService.Domain.Entities;

public enum ScreenStatus
{
    Active = 1,
    Revoked = 2,
}

public enum PlayKind
{
    Banner = 1,
    Ad = 2,
    DefaultBoard = 3,
}

/// <summary>
/// A television or tablet in a shop that plays its banners. It is not a person's login: it holds a long secret of its own (made when it starts pairing,
/// never shown again) that it trades for a short-lived access token, and that token may only read what the screen needs.
/// </summary>
public class Screen
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>SHA-256 of the device secret. The secret itself is only on the device.</summary>
    public string DeviceSecretHash { get; set; } = string.Empty;

    public ScreenStatus Status { get; set; } = ScreenStatus.Active;
    public Guid PairedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The last time the device did anything (asked for a token, sent a heartbeat).</summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>The last heartbeat: the time between two heartbeats is what is counted as played.</summary>
    public DateTime? LastHeartbeatAt { get; set; }
    public string? LastSeenIp { get; set; }
    public string? UserAgent { get; set; }
    public string? AppVersion { get; set; }
    public DateTime? RevokedAt { get; set; }

    /// <summary>When the owner was last told that the screen is offline, so one outage does not make a message every minute.</summary>
    public DateTime? OfflineNoticeAt { get; set; }
}

/// <summary>A screen that is showing a code and waiting for an owner to type it in.</summary>
public class ScreenPairing
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Six characters shown on the screen. Short-lived.</summary>
    public string Code { get; set; } = string.Empty;
    public string DeviceSecretHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when an owner has entered the code: the screen that was made for it.</summary>
    public Guid? ScreenId { get; set; }
}

/// <summary>Seconds a banner, an ad or the default board was on a screen on one day (UTC): proof of play, counted from the heartbeats.</summary>
public class ScreenPlayStat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScreenId { get; set; }
    public Guid ShopId { get; set; }
    public DateTime Day { get; set; }
    public PlayKind Kind { get; set; }

    /// <summary>The banner or ad. Empty for the default board.</summary>
    public Guid RefId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Seconds { get; set; }
}

namespace BannerService.Domain.Entities;

using ValueObjects;

/// <summary>How an ad sits on a shop's screen.</summary>
public enum ShopAdKind
{
    /// <summary>A strip along one edge that takes a share of the screen; the shop's banner keeps the rest.</summary>
    Side = 1,

    /// <summary>The ad takes the larger part of the screen for a period and the banner moves to the smaller side.</summary>
    Mega = 2,

    /// <summary>A box that appears over the banner for a few seconds, now and then. Its size does not take space from the banner.</summary>
    Popup = 3,

    /// <summary>A small tile in one corner.</summary>
    Minor = 4,
}

public enum ShopAdPlacement
{
    Left = 1,
    Right = 2,
    Top = 3,
    Bottom = 4,
    TopLeft = 5,
    TopRight = 6,
    BottomLeft = 7,
    BottomRight = 8,
    Center = 9,
}

public enum ShopAdStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5,
}

/// <summary>Who booked the ad. It decides who approves it and who may change it.</summary>
public enum ShopAdSource
{
    Admin = 1,
    ShopOwner = 2,
    SalesExecutive = 3,
}

/// <summary>
/// An advertisement on one shop's screen for a period. Advertisers are not users: the admin, the shop owner or one of the shop's sales
/// executives books the ad for them. Whether it is live is worked out from the time, so the space goes back to the banner when it ends.
/// </summary>
public class ShopAd
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public ShopAdSource Source { get; set; }

    /// <summary>The business the ad is for, as the shop knows it. A business name, never a person's details.</summary>
    public string AdvertiserName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string? Body { get; set; }

    /// <summary>A picture from the shop's own files. Admin ads are text only for now (the admin has no files of the shop).</summary>
    public Guid? MediaFileId { get; set; }

    /// <summary>#rrggbb</summary>
    public string Background { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#111111";

    public ShopAdKind Kind { get; set; }
    public ShopAdPlacement Placement { get; set; }

    /// <summary>Share of the screen for Side and Mega ads. Zero for the other kinds.</summary>
    public int SpacePercent { get; set; }

    /// <summary>Popup only: how long it stays, and how often it comes back.</summary>
    public int PopupSeconds { get; set; }
    public int PopupEveryMinutes { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int? DailyStartMinutes { get; set; }
    public int? DailyEndMinutes { get; set; }
    public int ActiveDays { get; set; } = DailySchedule.AllDays;

    public ShopAdStatus Status { get; set; } = ShopAdStatus.Draft;
    public Guid? DecidedByUserId { get; set; }
    public string? DecidedByName { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public PublishWindow GetWindow() => new(StartAt, EndAt);

    public DailySchedule? GetDailySchedule() =>
        DailyStartMinutes.HasValue && DailyEndMinutes.HasValue ? new DailySchedule(DailyStartMinutes.Value, DailyEndMinutes.Value, ActiveDays) : null;

    public void SetDaily(DailySchedule? daily)
    {
        daily?.Validate();
        DailyStartMinutes = daily?.StartMinutes;
        DailyEndMinutes = daily?.EndMinutes;
        ActiveDays = daily?.Days ?? DailySchedule.AllDays;
    }

    /// <summary>Holds its place on the screen: waiting for approval or approved.</summary>
    public bool HoldsSlot => Status is ShopAdStatus.PendingApproval or ShopAdStatus.Approved;

    public bool CanBeEdited => Status is ShopAdStatus.Draft or ShopAdStatus.Rejected;
}

/// <summary>One line of an ad's history: who did what and when. Kept for as long as the ad is.</summary>
public class ShopAdEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopAdId { get; set; }
    public Guid ShopId { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    /// <summary>Created, Edited, Submitted, Approved, Rejected, Cancelled.</summary>
    public string Action { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

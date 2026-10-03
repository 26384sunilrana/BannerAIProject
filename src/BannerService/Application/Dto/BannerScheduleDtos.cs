namespace BannerService.Application.Dto;

public class SetBannerScheduleRequestDto
{
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    /// <summary>Hours of the day (shop time, minutes from midnight) the banner is shown within the window. Leave both out to show it all the time.</summary>
    public int? DailyStartMinutes { get; set; }
    public int? DailyEndMinutes { get; set; }

    /// <summary>Weekdays the daily hours apply to: one bit per day, Sunday = 1, Monday = 2 ... Saturday = 64. Defaults to every day.</summary>
    public int? ActiveDays { get; set; }
}

public class BannerScheduleResponseDto
{
    public Guid BannerId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int? DailyStartMinutes { get; set; }
    public int? DailyEndMinutes { get; set; }
    public int ActiveDays { get; set; }
    /// <summary>The shop's time zone the daily hours are in.</summary>
    public string TimeZoneId { get; set; } = "UTC";
    public bool RequiresReapproval { get; set; }
}

/// <summary>One stretch of time a banner is shown, for the calendar.</summary>
public class CalendarEntryDto
{
    public Guid BannerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    /// <summary>True once the banner is approved and published, so it will really be shown.</summary>
    public bool Published { get; set; }
}

public class ScheduleCalendarDto
{
    public string TimeZoneId { get; set; } = "UTC";
    public List<CalendarEntryDto> Entries { get; set; } = new();
}

public class ActiveBannerResponseDto
{
    /// <summary>Null when nothing is live; the client then shows the default banner from the local machine.</summary>
    public BannerResponseDto? Banner { get; set; }
    public bool UseDefaultBanner { get; set; }

    /// <summary>Why the default banner is used: NothingScheduled or SubscriptionEnded.</summary>
    public string? Reason { get; set; }
}

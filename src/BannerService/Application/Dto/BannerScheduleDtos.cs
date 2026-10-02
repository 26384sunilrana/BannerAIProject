namespace BannerService.Application.Dto;

public class SetBannerScheduleRequestDto
{
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}

public class BannerScheduleResponseDto
{
    public Guid BannerId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool RequiresReapproval { get; set; }
}

public class ActiveBannerResponseDto
{
    /// <summary>Null when nothing is live; the client then shows the default banner from the local machine.</summary>
    public BannerResponseDto? Banner { get; set; }
    public bool UseDefaultBanner { get; set; }

    /// <summary>Why the default banner is used: NothingScheduled or SubscriptionEnded.</summary>
    public string? Reason { get; set; }
}

namespace BannerService.Application.Services;

using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;
using Domain.ValueObjects;
using Dto;

public class BannerScheduleAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishWorkflowRepository _workflowRepository;
    private readonly BannerScheduleService _scheduleService;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IShopRepository _shops;

    public BannerScheduleAppService(
        IUnitOfWork unitOfWork,
        IPublishWorkflowRepository workflowRepository,
        BannerScheduleService scheduleService,
        ISubscriptionRepository subscriptions,
        IShopRepository shops)
    {
        _shops = shops;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _workflowRepository = workflowRepository;
        _scheduleService = scheduleService;
    }

    public async Task<BannerScheduleResponseDto> SetScheduleAsync(
        Guid bannerId, Guid shopId, Guid userId, string userName, SetBannerScheduleRequestDto request)
    {
        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId)
            ?? throw new KeyNotFoundException("Banner not found");

        var window = new PublishWindow(request.StartAt.ToUniversalTime(), request.EndAt.ToUniversalTime());
        var daily = ReadDaily(request);
        var zone = await ZoneAsync(shopId);
        var shopBanners = await _unitOfWork.BannerRepository.GetScheduledByShopAsync(shopId);
        _scheduleService.ValidateNoOverlap(banner, window, shopBanners, daily, zone.Zone);

        var requiresReapproval = banner.SetSchedule(window, daily);
        await _unitOfWork.BannerRepository.UpdateAsync(banner);

        if (requiresReapproval)
        {
            // A changed schedule goes through the same approval as a new banner
            var workflow = await _workflowRepository.GetByBannerIdAsync(bannerId);
            if (workflow != null)
            {
                workflow.ResubmitForApproval(userId, userName, "Publish schedule changed");
                await _workflowRepository.UpdateAsync(workflow);
            }
        }

        return new BannerScheduleResponseDto
        {
            BannerId = banner.Id,
            StartAt = window.Start,
            EndAt = window.End,
            DailyStartMinutes = banner.DailyStartMinutes,
            DailyEndMinutes = banner.DailyEndMinutes,
            ActiveDays = banner.ActiveDays,
            TimeZoneId = zone.Id,
            RequiresReapproval = requiresReapproval
        };
    }

    private static DailySchedule? ReadDaily(SetBannerScheduleRequestDto request)
    {
        if (request.DailyStartMinutes == null && request.DailyEndMinutes == null)
            return null;
        if (request.DailyStartMinutes == null || request.DailyEndMinutes == null)
            throw new ArgumentException("Give both the daily start and the daily end, or neither");

        var daily = new DailySchedule(request.DailyStartMinutes.Value, request.DailyEndMinutes.Value, request.ActiveDays ?? DailySchedule.AllDays);
        daily.Validate();
        return daily;
    }

    private async Task<ShopTimeZoneInfo> ZoneAsync(Guid shopId)
    {
        var shop = await _shops.GetByIdAsync(shopId);
        return shop == null ? new ShopTimeZoneInfo(ShopTimeZone.Fallback, "default", TimeZoneInfo.Utc) : ShopTimeZone.Resolve(shop);
    }

    /// <summary>When each scheduled banner is shown between two moments, in time order. At most 62 days at a time.</summary>
    public async Task<ScheduleCalendarDto> GetCalendarAsync(Guid shopId, DateTime fromUtc, DateTime toUtc)
    {
        fromUtc = fromUtc.ToUniversalTime();
        toUtc = toUtc.ToUniversalTime();
        if (toUtc <= fromUtc)
            throw new ArgumentException("The end of the range must be after its start");
        if ((toUtc - fromUtc).TotalDays > 62)
            throw new ArgumentException("Ask for 62 days or fewer at a time");

        var zone = await ZoneAsync(shopId);
        var entries = new List<CalendarEntryDto>();
        foreach (var banner in await _unitOfWork.BannerRepository.GetScheduledByShopAsync(shopId))
        {
            var window = banner.GetPublishWindow();
            if (window == null)
                continue;

            var workflow = await _workflowRepository.GetByBannerIdAsync(banner.Id);
            foreach (var occurrence in ScheduleOccurrences.For(window, banner.GetDailySchedule(), zone.Zone, fromUtc, toUtc))
            {
                entries.Add(new CalendarEntryDto
                {
                    BannerId = banner.Id,
                    Name = banner.Name,
                    StartUtc = occurrence.Start,
                    EndUtc = occurrence.End,
                    Published = workflow is { IsPublished: true },
                });
            }
        }

        return new ScheduleCalendarDto { TimeZoneId = zone.Id, Entries = entries.OrderBy(e => e.StartUtc).ToList() };
    }

    public async Task<ActiveBannerResponseDto> GetActiveBannerAsync(Guid shopId, DateTime now)
    {
        // After the renewal date passes unpaid, only the default banner on the shop's own machine is shown
        var subscription = await _subscriptions.GetByShopIdAsync(shopId);
        if (subscription?.ShowsDefaultBannerOnly == true)
            return new ActiveBannerResponseDto { UseDefaultBanner = true, Reason = "SubscriptionEnded" };

        var scheduled = await _unitOfWork.BannerRepository.GetScheduledByShopAsync(shopId);
        var zone = await ZoneAsync(shopId);

        var candidates = new List<(Banner, PublishWorkflow?)>();
        foreach (var banner in scheduled.Where(b => b.GetPublishWindow()?.Contains(now) == true))
            candidates.Add((banner, await _workflowRepository.GetByBannerIdAsync(banner.Id)));

        var active = _scheduleService.ResolveActive(candidates, now, zone.Zone);

        return active == null
            ? new ActiveBannerResponseDto { UseDefaultBanner = true, Reason = "NothingScheduled" }
            : new ActiveBannerResponseDto { Banner = BannerResponseDto.FromBanner(active) };
    }
}

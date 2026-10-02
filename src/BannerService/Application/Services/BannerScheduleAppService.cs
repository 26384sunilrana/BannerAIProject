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

    public BannerScheduleAppService(
        IUnitOfWork unitOfWork,
        IPublishWorkflowRepository workflowRepository,
        BannerScheduleService scheduleService,
        ISubscriptionRepository subscriptions)
    {
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
        var shopBanners = await _unitOfWork.BannerRepository.GetScheduledByShopAsync(shopId);
        _scheduleService.ValidateNoOverlap(banner, window, shopBanners);

        var requiresReapproval = banner.SetSchedule(window);
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
            RequiresReapproval = requiresReapproval
        };
    }

    public async Task<ActiveBannerResponseDto> GetActiveBannerAsync(Guid shopId, DateTime now)
    {
        // After the renewal date passes unpaid, only the default banner on the shop's own machine is shown
        var subscription = await _subscriptions.GetByShopIdAsync(shopId);
        if (subscription?.ShowsDefaultBannerOnly == true)
            return new ActiveBannerResponseDto { UseDefaultBanner = true, Reason = "SubscriptionEnded" };

        var scheduled = await _unitOfWork.BannerRepository.GetScheduledByShopAsync(shopId);

        var candidates = new List<(Banner, PublishWorkflow?)>();
        foreach (var banner in scheduled.Where(b => b.GetPublishWindow()?.Contains(now) == true))
            candidates.Add((banner, await _workflowRepository.GetByBannerIdAsync(banner.Id)));

        var active = _scheduleService.ResolveActive(candidates, now);

        return active == null
            ? new ActiveBannerResponseDto { UseDefaultBanner = true, Reason = "NothingScheduled" }
            : new ActiveBannerResponseDto { Banner = BannerResponseDto.FromBanner(active) };
    }
}

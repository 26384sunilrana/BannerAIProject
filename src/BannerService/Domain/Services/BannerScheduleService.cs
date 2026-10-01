namespace BannerService.Domain.Services;

using Entities;
using ValueObjects;

public class BannerScheduleService
{
    /// <summary>
    /// A shop shows one banner at a time, so scheduled windows must not overlap
    /// (for same-day banners this means the hours must not overlap).
    /// </summary>
    public void ValidateNoOverlap(Banner banner, PublishWindow window, IEnumerable<Banner> shopBanners)
    {
        foreach (var other in shopBanners)
        {
            if (other.Id == banner.Id || other.ShopId != banner.ShopId)
                continue;

            var otherWindow = other.GetPublishWindow();
            if (otherWindow == null || !window.Overlaps(otherWindow))
                continue;

            throw new InvalidOperationException(
                $"Schedule overlaps banner '{other.Name}' ({otherWindow.Start:u} to {otherWindow.End:u})");
        }
    }

    /// <summary>
    /// Returns the banner to show at the given moment, or null when the shop
    /// should fall back to the default banner stored on its local machine.
    /// </summary>
    public Banner? ResolveActive(IEnumerable<(Banner banner, PublishWorkflow? workflow)> candidates, DateTime now)
    {
        return candidates
            .Where(c => c.workflow is { IsPublished: true })
            .Where(c => c.banner.GetPublishWindow()?.Contains(now) == true)
            .Select(c => c.banner)
            .FirstOrDefault();
    }
}

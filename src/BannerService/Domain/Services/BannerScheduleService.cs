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

            // No times in the message: the server does not know the viewer's time zone, and the owner can see
            // the other banner's hours in their own list
            throw new InvalidOperationException(
                $"Schedule overlaps banner '{other.Name}'. Choose hours that do not overlap another banner.");
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

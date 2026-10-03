namespace BannerService.Domain.Services;

using Entities;
using ValueObjects;

public class BannerScheduleService
{
    /// <summary>
    /// A shop shows one banner at a time, so two banners cannot be shown at the same moment. Banners with daily hours
    /// only clash when their hours meet on a shared day (in the shop's time zone); for banners without daily hours this
    /// is the old rule: their date windows must not overlap.
    /// </summary>
    public void ValidateNoOverlap(
        Banner banner, PublishWindow window, IEnumerable<Banner> shopBanners, DailySchedule? daily = null, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Utc;

        foreach (var other in shopBanners)
        {
            if (other.Id == banner.Id || other.ShopId != banner.ShopId)
                continue;

            var otherWindow = other.GetPublishWindow();
            if (otherWindow == null || !ScheduleOccurrences.Conflict(window, daily, otherWindow, other.GetDailySchedule(), zone))
                continue;

            // No times in the message: the server does not know the viewer's time zone, and the owner can see
            // the other banner's hours in their own list
            throw new InvalidOperationException(
                $"Schedule overlaps banner '{other.Name}'. Choose hours that do not overlap another banner.");
        }
    }

    /// <summary>
    /// Returns the banner to show at the given moment, or null when the shop
    /// should fall back to its default banner.
    /// </summary>
    public Banner? ResolveActive(IEnumerable<(Banner banner, PublishWorkflow? workflow)> candidates, DateTime now, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Utc;

        return candidates
            .Where(c => c.workflow is { IsPublished: true })
            .Where(c => c.banner.GetPublishWindow() is { } window && ScheduleOccurrences.IsLive(window, c.banner.GetDailySchedule(), zone, now))
            .Select(c => c.banner)
            .FirstOrDefault();
    }
}

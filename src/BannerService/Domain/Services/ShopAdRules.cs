namespace BannerService.Domain.Services;

using System.Text.RegularExpressions;
using Entities;

/// <summary>What makes an ad acceptable, and when two ads cannot share a screen at the same time.</summary>
public static class ShopAdRules
{
    public const int MaxHeadline = 80;
    public const int MaxBody = 300;
    public const int MaxAdvertiser = 80;
    public const int MaxDays = 366;

    public const int MinSidePercent = 10;
    public const int MaxSidePercent = 40;
    public const int MinMegaPercent = 50;
    public const int MaxMegaPercent = 80;

    /// <summary>All side ads showing together may not take more than this share of the screen.</summary>
    public const int MaxTotalSidePercent = 50;

    private static readonly Regex Colour = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    private static readonly ShopAdPlacement[] Edges = { ShopAdPlacement.Left, ShopAdPlacement.Right, ShopAdPlacement.Top, ShopAdPlacement.Bottom };
    private static readonly ShopAdPlacement[] Corners = { ShopAdPlacement.TopLeft, ShopAdPlacement.TopRight, ShopAdPlacement.BottomLeft, ShopAdPlacement.BottomRight };

    /// <summary>Returns the first thing wrong with the ad, or null. Checks the shape of the ad, not whether the slot is free.</summary>
    public static string? Validate(ShopAd ad)
    {
        if (string.IsNullOrWhiteSpace(ad.AdvertiserName) || ad.AdvertiserName.Trim().Length > MaxAdvertiser)
            return $"Give the business name of the advertiser (up to {MaxAdvertiser} characters).";
        if (string.IsNullOrWhiteSpace(ad.Headline) || ad.Headline.Trim().Length > MaxHeadline)
            return $"Write a headline of up to {MaxHeadline} characters.";
        if (ad.Body is { Length: > MaxBody })
            return $"The text can be up to {MaxBody} characters.";
        if (!Colour.IsMatch(ad.Background) || !Colour.IsMatch(ad.TextColor))
            return "Colours must look like #1e3a8a.";

        switch (ad.Kind)
        {
            case ShopAdKind.Side:
                if (!Edges.Contains(ad.Placement)) return "A side ad goes on the left, right, top or bottom.";
                if (ad.SpacePercent < MinSidePercent || ad.SpacePercent > MaxSidePercent)
                    return $"A side ad takes {MinSidePercent} to {MaxSidePercent} percent of the screen.";
                break;
            case ShopAdKind.Mega:
                if (!Edges.Contains(ad.Placement)) return "A mega ad goes on the left, right, top or bottom.";
                if (ad.SpacePercent < MinMegaPercent || ad.SpacePercent > MaxMegaPercent)
                    return $"A mega ad takes {MinMegaPercent} to {MaxMegaPercent} percent of the screen.";
                break;
            case ShopAdKind.Minor:
                if (!Corners.Contains(ad.Placement)) return "A minor ad sits in one corner.";
                break;
            case ShopAdKind.Popup:
                if (ad.PopupSeconds < 3 || ad.PopupSeconds > 30) return "A popup stays for 3 to 30 seconds.";
                if (ad.PopupEveryMinutes < 1 || ad.PopupEveryMinutes > 60) return "A popup comes back every 1 to 60 minutes.";
                if (ad.PopupSeconds >= ad.PopupEveryMinutes * 60) return "A popup must go away before it comes back.";
                break;
            default:
                return "Choose the kind of ad.";
        }

        if (ad.EndAt <= ad.StartAt) return "The end must be after the start.";
        if (ad.EndAt - ad.StartAt > TimeSpan.FromDays(MaxDays)) return $"An ad can run for at most {MaxDays} days at a time.";
        return null;
    }

    /// <summary>Puts the ad in the shape its kind expects (fields that do not apply are cleared).</summary>
    public static void Normalise(ShopAd ad)
    {
        ad.AdvertiserName = ad.AdvertiserName.Trim();
        ad.Headline = ad.Headline.Trim();
        ad.Body = string.IsNullOrWhiteSpace(ad.Body) ? null : ad.Body.Trim();
        ad.Background = ad.Background.ToLowerInvariant();
        ad.TextColor = ad.TextColor.ToLowerInvariant();

        if (ad.Kind == ShopAdKind.Popup)
        {
            ad.Placement = ShopAdPlacement.Center;
            ad.SpacePercent = 0;
        }
        else
        {
            ad.PopupSeconds = 0;
            ad.PopupEveryMinutes = 0;
        }
        if (ad.Kind == ShopAdKind.Minor) ad.SpacePercent = 0;
    }

    /// <summary>
    /// Says why the ad cannot take its place next to the ads that already hold one, or returns null. Only ads that would be on the
    /// screen at the same moment count. Several side ads are checked together (conservatively: every one that overlaps this ad).
    /// </summary>
    public static string? FindConflict(ShopAd ad, IEnumerable<ShopAd> others, TimeZoneInfo zone)
    {
        var overlapping = others
            .Where(o => o.Id != ad.Id && o.HoldsSlot)
            .Where(o => ScheduleOccurrences.Conflict(ad.GetWindow(), ad.GetDailySchedule(), o.GetWindow(), o.GetDailySchedule(), zone))
            .ToList();

        string Name(ShopAd o) => $"\"{o.Headline}\" ({o.AdvertiserName})";

        switch (ad.Kind)
        {
            case ShopAdKind.Mega:
                var big = overlapping.FirstOrDefault(o => o.Kind is ShopAdKind.Mega or ShopAdKind.Side);
                return big == null ? null : $"The screen is already taken at that time by {Name(big)}. A mega ad needs the screen to itself.";

            case ShopAdKind.Side:
                var mega = overlapping.FirstOrDefault(o => o.Kind == ShopAdKind.Mega);
                if (mega != null) return $"A mega ad, {Name(mega)}, has the screen at that time.";
                var sameEdge = overlapping.FirstOrDefault(o => o.Kind == ShopAdKind.Side && o.Placement == ad.Placement);
                if (sameEdge != null) return $"{Name(sameEdge)} is already on that side at that time.";
                var taken = overlapping.Where(o => o.Kind == ShopAdKind.Side).Sum(o => o.SpacePercent);
                return taken + ad.SpacePercent > MaxTotalSidePercent
                    ? $"Side ads may take {MaxTotalSidePercent} percent of the screen together; {taken} percent is already booked at that time."
                    : null;

            case ShopAdKind.Popup:
                var popup = overlapping.FirstOrDefault(o => o.Kind == ShopAdKind.Popup);
                return popup == null ? null : $"{Name(popup)} already has a popup at that time.";

            case ShopAdKind.Minor:
                var corner = overlapping.FirstOrDefault(o => o.Kind == ShopAdKind.Minor && o.Placement == ad.Placement);
                return corner == null ? null : $"{Name(corner)} is already in that corner at that time.";

            default:
                return null;
        }
    }
}

using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using BannerService.Domain.ValueObjects;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class ShopAdRulesTests
{
    private static readonly DateTime Monday = new(2030, 1, 7, 0, 0, 0, DateTimeKind.Utc);

    private static ShopAd Ad(ShopAdKind kind = ShopAdKind.Side, ShopAdPlacement placement = ShopAdPlacement.Left, int percent = 20, int days = 7, int startDay = 0) => new()
    {
        AdvertiserName = "Olive Cafe", Headline = "Two for one", Background = "#ffffff", TextColor = "#111111",
        Kind = kind, Placement = placement, SpacePercent = percent,
        PopupSeconds = kind == ShopAdKind.Popup ? 10 : 0, PopupEveryMinutes = kind == ShopAdKind.Popup ? 5 : 0,
        StartAt = Monday.AddDays(startDay), EndAt = Monday.AddDays(startDay + days), Status = ShopAdStatus.Approved,
    };

    private static string? Conflict(ShopAd ad, params ShopAd[] others) => ShopAdRules.FindConflict(ad, others, TimeZoneInfo.Utc);

    // ----- shape

    [Fact]
    public void AGoodAd_HasNothingWrong() => Assert.Null(ShopAdRules.Validate(Ad()));

    [Theory]
    [InlineData(ShopAdKind.Side, ShopAdPlacement.Left, 5, false)]
    [InlineData(ShopAdKind.Side, ShopAdPlacement.Left, 10, true)]
    [InlineData(ShopAdKind.Side, ShopAdPlacement.Left, 40, true)]
    [InlineData(ShopAdKind.Side, ShopAdPlacement.Left, 41, false)]
    [InlineData(ShopAdKind.Side, ShopAdPlacement.TopLeft, 20, false)]
    [InlineData(ShopAdKind.Mega, ShopAdPlacement.Right, 49, false)]
    [InlineData(ShopAdKind.Mega, ShopAdPlacement.Right, 50, true)]
    [InlineData(ShopAdKind.Mega, ShopAdPlacement.Right, 80, true)]
    [InlineData(ShopAdKind.Mega, ShopAdPlacement.Right, 81, false)]
    [InlineData(ShopAdKind.Minor, ShopAdPlacement.BottomRight, 0, true)]
    [InlineData(ShopAdKind.Minor, ShopAdPlacement.Left, 0, false)]
    public void TheSpaceAndPlaceDependOnTheKind(ShopAdKind kind, ShopAdPlacement placement, int percent, bool ok) =>
        Assert.Equal(ok, ShopAdRules.Validate(Ad(kind, placement, percent)) == null);

    [Theory]
    [InlineData(2, 5, false)]
    [InlineData(3, 5, true)]
    [InlineData(30, 5, true)]
    [InlineData(31, 5, false)]
    [InlineData(10, 0, false)]
    [InlineData(10, 61, false)]
    [InlineData(30, 1, true)]
    [InlineData(30, 1, true)]
    public void APopupNeedsSensibleTimes(int seconds, int everyMinutes, bool ok)
    {
        var popup = Ad(ShopAdKind.Popup);
        popup.PopupSeconds = seconds;
        popup.PopupEveryMinutes = everyMinutes;

        Assert.Equal(ok, ShopAdRules.Validate(popup) == null);
    }

    [Theory]
    [InlineData("", "Headline")]
    [InlineData("Cafe", "")]
    [InlineData("  ", "Headline")]
    public void TheAdvertiserAndHeadlineAreRequired(string advertiser, string headline)
    {
        var ad = Ad();
        ad.AdvertiserName = advertiser;
        ad.Headline = headline;

        Assert.NotNull(ShopAdRules.Validate(ad));
    }

    [Fact]
    public void LongTextAndBadColoursAreRefused()
    {
        var ad = Ad();
        ad.Headline = new string('x', 81);
        Assert.NotNull(ShopAdRules.Validate(ad));

        ad = Ad();
        ad.Body = new string('x', 301);
        Assert.NotNull(ShopAdRules.Validate(ad));

        ad = Ad();
        ad.Background = "red";
        Assert.NotNull(ShopAdRules.Validate(ad));
    }

    [Fact]
    public void TheDatesMustMakeSense()
    {
        var ad = Ad();
        ad.EndAt = ad.StartAt;
        Assert.NotNull(ShopAdRules.Validate(ad));

        Assert.Null(ShopAdRules.Validate(Ad(days: 366)));
        Assert.NotNull(ShopAdRules.Validate(Ad(days: 367)));
    }

    [Fact]
    public void NormaliseTidiesTheTextAndClearsWhatDoesNotApply()
    {
        var ad = Ad(ShopAdKind.Popup, ShopAdPlacement.Left, 30);
        ad.Headline = "  Sale  ";
        ad.Body = "   ";
        ad.Background = "#ABCDEF";

        ShopAdRules.Normalise(ad);

        Assert.Equal("Sale", ad.Headline);
        Assert.Null(ad.Body);
        Assert.Equal("#abcdef", ad.Background);
        Assert.Equal(ShopAdPlacement.Center, ad.Placement);
        Assert.Equal(0, ad.SpacePercent);

        var side = Ad();
        side.PopupSeconds = 9;
        ShopAdRules.Normalise(side);
        Assert.Equal(0, side.PopupSeconds);
    }

    // ----- sharing the screen

    [Fact]
    public void AdsAtDifferentTimesNeverClash() =>
        Assert.Null(Conflict(Ad(ShopAdKind.Mega, ShopAdPlacement.Left, 70, startDay: 7), Ad(ShopAdKind.Mega, ShopAdPlacement.Left, 70)));

    [Fact]
    public void AMegaAdNeedsTheScreenToItself()
    {
        var side = Ad(ShopAdKind.Side, ShopAdPlacement.Left, 20);
        var mega = Ad(ShopAdKind.Mega, ShopAdPlacement.Right, 60);

        Assert.Contains("needs the screen to itself", Conflict(mega, side));
        Assert.Contains("mega ad", Conflict(side, mega));
        Assert.Contains("needs the screen", Conflict(mega, Ad(ShopAdKind.Mega, ShopAdPlacement.Left, 60)));
    }

    [Fact]
    public void SideAdsShareUpToHalfTheScreen_OnDifferentEdges()
    {
        var left = Ad(ShopAdKind.Side, ShopAdPlacement.Left, 30);

        Assert.Null(Conflict(Ad(ShopAdKind.Side, ShopAdPlacement.Right, 20), left));
        Assert.Contains("50 percent", Conflict(Ad(ShopAdKind.Side, ShopAdPlacement.Right, 25), left));
        Assert.Contains("already on that side", Conflict(Ad(ShopAdKind.Side, ShopAdPlacement.Left, 10), left));
    }

    [Fact]
    public void OnlyOnePopupAtATime_AndOneMinorAdPerCorner()
    {
        Assert.Contains("popup", Conflict(Ad(ShopAdKind.Popup), Ad(ShopAdKind.Popup)));

        var corner = Ad(ShopAdKind.Minor, ShopAdPlacement.TopLeft);
        Assert.Contains("corner", Conflict(Ad(ShopAdKind.Minor, ShopAdPlacement.TopLeft), corner));
        Assert.Null(Conflict(Ad(ShopAdKind.Minor, ShopAdPlacement.TopRight), corner));

        // a popup does not compete with a minor ad or a side ad
        Assert.Null(Conflict(Ad(ShopAdKind.Popup), corner, Ad(ShopAdKind.Side)));
    }

    [Fact]
    public void OnlyAdsHoldingASlotCount_AndAnAdNeverClashesWithItself()
    {
        var cancelled = Ad(ShopAdKind.Mega, ShopAdPlacement.Left, 70);
        cancelled.Status = ShopAdStatus.Cancelled;
        var rejected = Ad(ShopAdKind.Mega, ShopAdPlacement.Left, 70);
        rejected.Status = ShopAdStatus.Rejected;
        var pending = Ad(ShopAdKind.Mega, ShopAdPlacement.Left, 70);
        pending.Status = ShopAdStatus.PendingApproval;
        var candidate = Ad(ShopAdKind.Mega, ShopAdPlacement.Left, 70);

        Assert.Null(Conflict(candidate, cancelled, rejected, candidate));
        Assert.NotNull(Conflict(candidate, pending));
    }

    [Fact]
    public void DailyHoursLetTwoMegaAdsShareTheSameDates()
    {
        var morning = Ad(ShopAdKind.Mega, ShopAdPlacement.Left, 70);
        morning.SetDaily(new DailySchedule(9 * 60, 12 * 60));
        var afternoon = Ad(ShopAdKind.Mega, ShopAdPlacement.Left, 70);
        afternoon.SetDaily(new DailySchedule(12 * 60, 15 * 60));
        var clash = Ad(ShopAdKind.Mega, ShopAdPlacement.Left, 70);
        clash.SetDaily(new DailySchedule(11 * 60, 13 * 60));

        Assert.Null(Conflict(afternoon, morning));
        Assert.NotNull(Conflict(clash, morning, afternoon));
    }
}

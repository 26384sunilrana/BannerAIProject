using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using BannerService.Domain.ValueObjects;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class ScheduleOccurrencesTests
{
    private static readonly TimeZoneInfo Kolkata = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");   // UTC+5:30, no daylight saving
    private static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0) => new(y, m, d, h, min, 0, DateTimeKind.Utc);
    private static int Minutes(int h, int m = 0) => h * 60 + m;

    // 2026-10-05 is a Monday
    private static PublishWindow Week() => new(Utc(2026, 10, 4, 18, 30), Utc(2026, 10, 11, 18, 30)); // Mon 00:00 to Mon 00:00 in Kolkata

    // ----- no daily hours: the whole window

    [Fact]
    public void WithoutDailyHours_ItIsTheWholeWindow_NarrowedToTheRange()
    {
        var window = new PublishWindow(Utc(2026, 10, 1), Utc(2026, 10, 10));

        var all = ScheduleOccurrences.For(window, null, Kolkata, Utc(2026, 9, 1), Utc(2026, 12, 1));
        var part = ScheduleOccurrences.For(window, null, Kolkata, Utc(2026, 10, 3), Utc(2026, 10, 5));

        Assert.Equal(new[] { new Occurrence(Utc(2026, 10, 1), Utc(2026, 10, 10)) }, all);
        Assert.Equal(new[] { new Occurrence(Utc(2026, 10, 3), Utc(2026, 10, 5)) }, part);
        Assert.Empty(ScheduleOccurrences.For(window, null, Kolkata, Utc(2026, 11, 1), Utc(2026, 12, 1)));
    }

    // ----- daily hours

    [Fact]
    public void DailyHours_AreInTheShopsTimeZone_OnEveryDayOfTheWindow()
    {
        var daily = new DailySchedule(Minutes(9), Minutes(12));

        var list = ScheduleOccurrences.For(Week(), daily, Kolkata, Utc(2026, 1, 1), Utc(2027, 1, 1));

        Assert.Equal(7, list.Count);
        Assert.Equal(new Occurrence(Utc(2026, 10, 5, 3, 30), Utc(2026, 10, 5, 6, 30)), list[0]); // 09:00-12:00 in Kolkata is 03:30-06:30 UTC
        Assert.Equal(new Occurrence(Utc(2026, 10, 11, 3, 30), Utc(2026, 10, 11, 6, 30)), list[6]);
    }

    [Fact]
    public void TheSameHours_ShiftWithTheTimeZone()
    {
        var daily = new DailySchedule(Minutes(9), Minutes(12));
        var window = new PublishWindow(Utc(2026, 1, 12), Utc(2026, 1, 13)); // a Monday in winter

        var london = ScheduleOccurrences.For(window, daily, London, window.Start, window.End);
        var newYork = ScheduleOccurrences.For(window, daily, NewYork, window.Start.AddDays(-1), window.End.AddDays(1));

        Assert.Equal(new Occurrence(Utc(2026, 1, 12, 9), Utc(2026, 1, 12, 12)), london.Single());       // GMT: same as UTC
        Assert.Contains(new Occurrence(Utc(2026, 1, 12, 14), Utc(2026, 1, 12, 17)), newYork);            // EST is UTC-5
    }

    [Fact]
    public void OnlyTheChosenWeekdays_AreIncluded()
    {
        var weekdays = new DailySchedule(Minutes(9), Minutes(17), DailySchedule.Bit(DayOfWeek.Monday) | DailySchedule.Bit(DayOfWeek.Tuesday) | DailySchedule.Bit(DayOfWeek.Wednesday)
            | DailySchedule.Bit(DayOfWeek.Thursday) | DailySchedule.Bit(DayOfWeek.Friday));

        var list = ScheduleOccurrences.For(Week(), weekdays, Kolkata, Week().Start, Week().End);

        Assert.Equal(5, list.Count);
        Assert.All(list, o => Assert.NotEqual(DayOfWeek.Saturday, TimeZoneInfo.ConvertTimeFromUtc(o.Start, Kolkata).DayOfWeek));
        Assert.All(list, o => Assert.NotEqual(DayOfWeek.Sunday, TimeZoneInfo.ConvertTimeFromUtc(o.Start, Kolkata).DayOfWeek));
    }

    [Fact]
    public void HoursThatRunPastMidnight_AreOneStretch_ClippedToTheWindow()
    {
        var night = new DailySchedule(Minutes(22), Minutes(2));
        var window = new PublishWindow(Utc(2026, 10, 5, 0), Utc(2026, 10, 7, 0)); // UTC; Kolkata is 5:30 ahead

        var list = ScheduleOccurrences.For(window, night, Kolkata, window.Start, window.End);

        // 22:00 Mon Kolkata = 16:30Z, until 02:00 Tue = 20:30Z; the one that started before the window and the one after it are clipped away
        Assert.Equal(new[]
        {
            new Occurrence(Utc(2026, 10, 5, 16, 30), Utc(2026, 10, 5, 20, 30)),
            new Occurrence(Utc(2026, 10, 6, 16, 30), Utc(2026, 10, 6, 20, 30)),
        }, list);
        Assert.True(night.IsOvernight);
    }

    [Fact]
    public void AnOvernightStretch_BelongsToTheDayItStarts()
    {
        // Friday-only 22:00-02:00 in London (summer time, UTC+1): live Friday night and Saturday small hours, not Saturday night or Sunday small hours
        var friday = new DailySchedule(Minutes(22), Minutes(2), DailySchedule.Bit(DayOfWeek.Friday));
        var window = new PublishWindow(Utc(2026, 10, 1), Utc(2026, 10, 20));

        bool Live(int day, int hour, int minute) => ScheduleOccurrences.IsLive(window, friday, London, Utc(2026, 10, day, hour, minute));

        Assert.True(Live(9, 21, 30));    // Friday 22:30 local
        Assert.True(Live(10, 0, 30));    // Saturday 01:30 local: it started on Friday
        Assert.False(Live(10, 1, 30));   // Saturday 02:30 local: over
        Assert.False(Live(10, 21, 30));  // Saturday 22:30 local: Saturday is not chosen
        Assert.False(Live(11, 0, 30));   // Sunday 01:30 local
    }

    [Fact]
    public void IsLive_FollowsTheDailyHours()
    {
        var daily = new DailySchedule(Minutes(9), Minutes(12));
        var window = Week();

        Assert.False(ScheduleOccurrences.IsLive(window, daily, Kolkata, Utc(2026, 10, 5, 3, 0)));   // 08:30 local
        Assert.True(ScheduleOccurrences.IsLive(window, daily, Kolkata, Utc(2026, 10, 5, 3, 30)));   // 09:00 local
        Assert.True(ScheduleOccurrences.IsLive(window, daily, Kolkata, Utc(2026, 10, 5, 6, 29)));   // 11:59 local
        Assert.False(ScheduleOccurrences.IsLive(window, daily, Kolkata, Utc(2026, 10, 5, 6, 30)));  // 12:00 local: the end is exclusive
        Assert.False(ScheduleOccurrences.IsLive(window, daily, Kolkata, Utc(2026, 10, 12, 4, 0)));  // after the window
    }

    [Fact]
    public void IsLive_WithoutDailyHours_IsJustTheWindow()
    {
        var window = new PublishWindow(Utc(2026, 10, 5), Utc(2026, 10, 6));

        Assert.True(ScheduleOccurrences.IsLive(window, null, Kolkata, Utc(2026, 10, 5, 12)));
        Assert.False(ScheduleOccurrences.IsLive(window, null, Kolkata, Utc(2026, 10, 6)));
    }

    // ----- daylight saving

    [Fact]
    public void WhenTheClocksGoForward_AnHourThatDoesNotExistMovesOn()
    {
        // London, Sunday 29 March 2026: 01:00-02:00 does not exist. Hours 01:30-03:30 start in the gap.
        var daily = new DailySchedule(Minutes(1, 30), Minutes(3, 30));
        var window = new PublishWindow(Utc(2026, 3, 29, 0), Utc(2026, 3, 30, 0));

        var list = ScheduleOccurrences.For(window, daily, London, window.Start, window.End);

        var day = list.Single(o => o.Start >= Utc(2026, 3, 29, 0));
        Assert.Equal(Utc(2026, 3, 29, 1, 30), day.Start);   // 01:30 does not exist, so it becomes 02:30 BST = 01:30Z
        Assert.Equal(Utc(2026, 3, 29, 2, 30), day.End);     // 03:30 BST = 02:30Z
    }

    [Fact]
    public void WhenTheClocksGoBack_AnHourThatHappensTwiceTakesTheFirst()
    {
        // London, Sunday 25 October 2026: 01:00-02:00 happens twice
        var daily = new DailySchedule(Minutes(1, 30), Minutes(4));
        var window = new PublishWindow(Utc(2026, 10, 25, 0), Utc(2026, 10, 26, 0));

        var list = ScheduleOccurrences.For(window, daily, London, window.Start, window.End);

        var day = list.Single(o => o.Start >= Utc(2026, 10, 25, 0));
        Assert.Equal(Utc(2026, 10, 25, 0, 30), day.Start);  // the first 01:30 is still BST (UTC+1)
        Assert.Equal(Utc(2026, 10, 25, 4, 0), day.End);     // 04:00 is GMT
    }

    // ----- two banners at once

    private static (PublishWindow, DailySchedule?) A(int startHour, int endHour, int days = DailySchedule.AllDays) =>
        (Week(), new DailySchedule(Minutes(startHour), Minutes(endHour), days));

    [Fact]
    public void BannersWithoutDailyHours_ConflictWhenTheirWindowsOverlap()
    {
        var one = new PublishWindow(Utc(2026, 10, 1), Utc(2026, 10, 10));

        Assert.True(ScheduleOccurrences.Conflict(one, null, new PublishWindow(Utc(2026, 10, 9), Utc(2026, 10, 12)), null, Kolkata));
        Assert.False(ScheduleOccurrences.Conflict(one, null, new PublishWindow(Utc(2026, 10, 10), Utc(2026, 10, 12)), null, Kolkata)); // touching is fine
    }

    [Fact]
    public void SameDates_DifferentHours_DoNotConflict()
    {
        var (w1, d1) = A(9, 12);
        var (w2, d2) = A(12, 15);

        Assert.False(ScheduleOccurrences.Conflict(w1, d1, w2, d2, Kolkata));
    }

    [Fact]
    public void SameDates_OverlappingHours_Conflict()
    {
        var (w1, d1) = A(9, 12);
        var (w2, d2) = A(11, 13);

        Assert.True(ScheduleOccurrences.Conflict(w1, d1, w2, d2, Kolkata));
    }

    [Fact]
    public void SameHours_OnDifferentWeekdays_DoNotConflict()
    {
        var weekdays = DailySchedule.Bit(DayOfWeek.Monday) | DailySchedule.Bit(DayOfWeek.Tuesday) | DailySchedule.Bit(DayOfWeek.Wednesday) | DailySchedule.Bit(DayOfWeek.Thursday) | DailySchedule.Bit(DayOfWeek.Friday);
        var weekend = DailySchedule.Bit(DayOfWeek.Saturday) | DailySchedule.Bit(DayOfWeek.Sunday);
        var (w1, d1) = A(9, 17, weekdays);
        var (w2, d2) = A(9, 17, weekend);
        var (w3, d3) = A(9, 17, DailySchedule.Bit(DayOfWeek.Friday));

        Assert.False(ScheduleOccurrences.Conflict(w1, d1, w2, d2, Kolkata));
        Assert.True(ScheduleOccurrences.Conflict(w1, d1, w3, d3, Kolkata));
    }

    [Fact]
    public void ABannerAllDayAndOneWithDailyHours_Conflict_WhenTheirDatesMeet()
    {
        var (w1, d1) = A(9, 12);

        Assert.True(ScheduleOccurrences.Conflict(w1, d1, new PublishWindow(Utc(2026, 10, 6), Utc(2026, 10, 7)), null, Kolkata));
        Assert.False(ScheduleOccurrences.Conflict(w1, d1, new PublishWindow(Utc(2026, 10, 20), Utc(2026, 10, 21)), null, Kolkata));
    }

    [Fact]
    public void OvernightHours_ConflictWithEarlyMorningHours_OfTheNextDay()
    {
        var window = new PublishWindow(Utc(2026, 10, 1), Utc(2026, 11, 1));
        var night = new DailySchedule(Minutes(22), Minutes(2));

        Assert.True(ScheduleOccurrences.Conflict(window, night, window, new DailySchedule(Minutes(1), Minutes(3)), Kolkata));
        Assert.False(ScheduleOccurrences.Conflict(window, night, window, new DailySchedule(Minutes(2), Minutes(6)), Kolkata));
    }

    [Fact]
    public void ARangeLongerThanTheSafetyStop_IsRefusedNotWalked()
    {
        var window = new PublishWindow(Utc(2000, 1, 1), Utc(2030, 1, 1));

        Assert.Throws<ArgumentException>(() => ScheduleOccurrences.For(window, new DailySchedule(Minutes(9), Minutes(10)), Kolkata, window.Start, window.End));
    }
}

public class DailyScheduleTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(540, 1020)]
    [InlineData(1320, 120)]
    [InlineData(1439, 0)]
    public void ValidHours_AreAccepted(int start, int end) => new DailySchedule(start, end).Validate();

    [Theory]
    [InlineData(-1, 60, 127)]
    [InlineData(60, 1440, 127)]
    [InlineData(600, 600, 127)]
    [InlineData(60, 120, 0)]
    [InlineData(60, 120, 128)]
    public void BadHours_AreRefused(int start, int end, int days)
    {
        Assert.Throws<ArgumentException>(() => new DailySchedule(start, end, days).Validate());
    }

    [Fact]
    public void Weekdays_UseTheDotNetNumbering()
    {
        Assert.Equal(1, DailySchedule.Bit(DayOfWeek.Sunday));
        Assert.Equal(2, DailySchedule.Bit(DayOfWeek.Monday));
        Assert.Equal(64, DailySchedule.Bit(DayOfWeek.Saturday));
        Assert.True(new DailySchedule(60, 120, 2).Includes(DayOfWeek.Monday));
        Assert.False(new DailySchedule(60, 120, 2).Includes(DayOfWeek.Tuesday));
    }
}

public class BannerDailyScheduleTests
{
    private static DateTime Utc(int d, int h = 0) => new(2026, 10, d, h, 0, 0, DateTimeKind.Utc);
    private static Banner ABanner() => new(Guid.NewGuid(), Guid.NewGuid(), "Lunch", "", 100, 50);

    [Fact]
    public void TheFirstScheduleNeedsNoReapproval_AChangeOfHoursDoes()
    {
        var banner = ABanner();
        var window = new PublishWindow(Utc(5), Utc(12));

        Assert.False(banner.SetSchedule(window, new DailySchedule(540, 720)));
        banner.Publish();
        Assert.False(banner.SetSchedule(window, new DailySchedule(540, 720)));   // nothing changed
        Assert.True(banner.IsPublished);

        Assert.True(banner.SetSchedule(window, new DailySchedule(540, 780)));    // the hours changed
        Assert.False(banner.IsPublished);
    }

    [Fact]
    public void AddingOrRemovingDailyHours_Counts_AsAChange()
    {
        var banner = ABanner();
        var window = new PublishWindow(Utc(5), Utc(12));
        banner.SetSchedule(window);
        banner.Publish();

        Assert.True(banner.SetSchedule(window, new DailySchedule(540, 720)));
        banner.Publish();
        Assert.True(banner.SetSchedule(window, null));
        Assert.Null(banner.GetDailySchedule());
        Assert.Equal(DailySchedule.AllDays, banner.ActiveDays);
    }

    [Fact]
    public void TheWeekdaysAreKept()
    {
        var banner = ABanner();

        banner.SetSchedule(new PublishWindow(Utc(5), Utc(12)), new DailySchedule(60, 120, 0b0101010));

        Assert.Equal(new DailySchedule(60, 120, 0b0101010), banner.GetDailySchedule());
    }

    [Fact]
    public void BadHours_AreRefused_AndNothingChanges()
    {
        var banner = ABanner();

        Assert.Throws<ArgumentException>(() => banner.SetSchedule(new PublishWindow(Utc(5), Utc(12)), new DailySchedule(600, 600)));
        Assert.Null(banner.PublishStartAt);
    }
}

public class ScheduleServiceWithHoursTests
{
    private static readonly TimeZoneInfo Kolkata = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
    private static DateTime Utc(int d, int h = 0, int m = 0) => new(2026, 10, d, h, m, 0, DateTimeKind.Utc);
    private readonly BannerScheduleService _service = new();
    private readonly Guid _shop = Guid.NewGuid();

    private Banner Scheduled(string name, PublishWindow window, DailySchedule? daily)
    {
        var banner = new Banner(_shop, Guid.NewGuid(), name, "", 100, 50);
        banner.SetSchedule(window, daily);
        return banner;
    }

    [Fact]
    public void Overlap_OnlyWhenTheHoursMeet()
    {
        var window = new PublishWindow(Utc(5), Utc(12));
        var morning = Scheduled("Morning", window, new DailySchedule(540, 720));
        var candidate = new Banner(_shop, Guid.NewGuid(), "Afternoon", "", 100, 50);

        _service.ValidateNoOverlap(candidate, window, new[] { morning }, new DailySchedule(720, 900), Kolkata);

        var ex = Assert.Throws<InvalidOperationException>(() => _service.ValidateNoOverlap(candidate, window, new[] { morning }, new DailySchedule(600, 900), Kolkata));
        Assert.Contains("Morning", ex.Message);
    }

    [Fact]
    public void WithoutHoursOrZone_ItIsTheOldRule()
    {
        var existing = Scheduled("Existing", new PublishWindow(Utc(5), Utc(8)), null);
        var candidate = new Banner(_shop, Guid.NewGuid(), "New", "", 100, 50);

        _service.ValidateNoOverlap(candidate, new PublishWindow(Utc(8), Utc(9)), new[] { existing });
        Assert.Throws<InvalidOperationException>(() => _service.ValidateNoOverlap(candidate, new PublishWindow(Utc(7), Utc(9)), new[] { existing }));
    }

    [Fact]
    public void ResolveActive_ShowsTheBannerOnlyInItsHours()
    {
        var banner = Scheduled("Lunch", new PublishWindow(Utc(5), Utc(12)), new DailySchedule(720, 840)); // 12:00-14:00 Kolkata
        var workflow = new PublishWorkflow { Status = PublishStatus.Published };
        var candidates = new[] { (banner, (PublishWorkflow?)workflow) };

        Assert.Same(banner, _service.ResolveActive(candidates, Utc(6, 7, 0), Kolkata));    // 12:30 Kolkata
        Assert.Null(_service.ResolveActive(candidates, Utc(6, 3, 0), Kolkata));            // 08:30 Kolkata
        Assert.Null(_service.ResolveActive(candidates, Utc(6, 9, 0), Kolkata));            // 14:30 Kolkata
    }

    [Fact]
    public void ResolveActive_IgnoresBannersThatAreNotPublished()
    {
        var banner = Scheduled("Lunch", new PublishWindow(Utc(5), Utc(12)), null);

        Assert.Null(_service.ResolveActive(new[] { (banner, (PublishWorkflow?)new PublishWorkflow { Status = PublishStatus.Approved }) }, Utc(6), Kolkata));
    }
}

public class ShopTimeZoneTests
{
    private static Shop AShop(string? own = null, string? city = null, string? country = null) => new()
    {
        TimeZoneId = own,
        CityNav = city == null ? null : new City { TimeZoneId = city },
        CountryNav = country == null ? null : new Country { TimeZoneId = country },
    };

    [Fact]
    public void TheShopsOwnSettingWins_ThenItsCity_ThenItsCountry()
    {
        Assert.Equal(("Europe/London", "shop"), Pair(ShopTimeZone.Resolve(AShop("Europe/London", "Asia/Kolkata", "America/New_York"))));
        Assert.Equal(("Asia/Kolkata", "city"), Pair(ShopTimeZone.Resolve(AShop(null, "Asia/Kolkata", "America/New_York"))));
        Assert.Equal(("America/New_York", "country"), Pair(ShopTimeZone.Resolve(AShop(null, null, "America/New_York"))));
    }

    [Fact]
    public void WithNothingSet_ItIsUtc()
    {
        Assert.Equal(("UTC", "default"), Pair(ShopTimeZone.Resolve(AShop())));
    }

    [Fact]
    public void AnUnknownName_IsSkipped_NotAnError()
    {
        Assert.Equal(("Asia/Kolkata", "city"), Pair(ShopTimeZone.Resolve(AShop("Mars/Olympus", "Asia/Kolkata"))));
        Assert.Equal(("UTC", "default"), Pair(ShopTimeZone.Resolve(AShop("Mars/Olympus", "Nowhere/Land", "Not/Real"))));
    }

    [Theory]
    [InlineData("Asia/Kolkata", true)]
    [InlineData(" Europe/London ", true)]
    [InlineData("UTC", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("Mars/Olympus", false)]
    public void TimeZones_KnowsRealNamesOnly(string? id, bool expected) => Assert.Equal(expected, TimeZones.TryGet(id, out _));

    private static (string, string) Pair(ShopTimeZoneInfo info) => (info.Id, info.Source);
}

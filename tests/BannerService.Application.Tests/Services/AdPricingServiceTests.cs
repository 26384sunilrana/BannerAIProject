using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.ValueObjects;
using BannerService.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class AdPricingServiceTests
{
    private static readonly DateTime Monday = new(2030, 1, 7, 0, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IShopAdRepository> _ads = new();
    private readonly Mock<IShopRepository> _shops = new();
    private readonly Mock<IMediaFileRepository> _media = new();
    private readonly Mock<IMediaUrlSigner> _signer = new();
    private readonly Mock<INotificationRepository> _notificationStore = new();
    private readonly Mock<IAdRateRepository> _rateStore = new();
    private readonly Mock<ILocationRepository> _locations = new();
    private readonly ShopAdService _service;
    private readonly AdStatementService _statements;
    private readonly Shop _shop = new() { Id = Guid.NewGuid(), Name = "Olive Mart", CountryCode = "IN", StateId = 7, CityId = 70, TimeZoneId = "UTC" };
    private readonly List<InAppNotification> _notified = new();
    private readonly List<ShopAdEvent> _events = new();

    private readonly AdActor _admin = new(Guid.NewGuid(), "Admin", ShopAdSource.Admin, null);
    private readonly AdActor _owner;
    private readonly AdActor _executive;

    public AdPricingServiceTests()
    {
        _owner = new AdActor(Guid.NewGuid(), "Olive", ShopAdSource.ShopOwner, _shop.Id);
        _executive = new AdActor(Guid.NewGuid(), "Sam", ShopAdSource.SalesExecutive, _shop.Id);

        _shops.Setup(r => r.GetByIdAsync(_shop.Id)).ReturnsAsync(_shop);
        _ads.Setup(r => r.FindSlotHoldersAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<ShopAd>());
        _ads.Setup(r => r.SaveAsync(It.IsAny<ShopAd>(), It.IsAny<ShopAdEvent?>())).Callback((ShopAd _, ShopAdEvent? e) => { if (e != null) _events.Add(e); }).Returns(Task.CompletedTask);
        _notificationStore.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyCollection<InAppNotification>>()))
            .Callback((IReadOnlyCollection<InAppNotification> items) => _notified.AddRange(items)).Returns(Task.CompletedTask);
        _notificationStore.Setup(r => r.FindUserIdsByRoleAsync("Admin", null)).ReturnsAsync(new List<string> { "admin-1", "admin-2" });
        _rateStore.Setup(r => r.FindCandidatesAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(new List<AdRate>());
        _rateStore.Setup(r => r.FindSamePlaceAsync(It.IsAny<AdRateLevel>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<ShopAdKind?>(), It.IsAny<Guid?>()))
            .ReturnsAsync((AdRate?)null);

        var notifications = new NotificationService(_notificationStore.Object, NullLogger<NotificationService>.Instance);
        var rates = new AdRateService(_rateStore.Object, _locations.Object);
        _service = new ShopAdService(_ads.Object, _shops.Object, _media.Object, _signer.Object, notifications, rates, _locations.Object);
        _statements = new AdStatementService(_ads.Object, _shops.Object);
    }

    private void RateIs(decimal price, int share = 100) =>
        _rateStore.Setup(r => r.FindCandidatesAsync("IN", 7, 70)).ReturnsAsync(new List<AdRate> { new() { Level = AdRateLevel.All, PricePerHour = price, ShopSharePercent = share } });

    private ShopAd Ad(ShopAdSource source, ShopAdStatus status = ShopAdStatus.Draft, ShopAdKind kind = ShopAdKind.Side, int percent = 20, DateTime? start = null, int days = 10) => new()
    {
        ShopId = _shop.Id, Source = source, Status = status, AdvertiserName = "City Bank", Headline = "Save more", Background = "#ffffff", TextColor = "#000000",
        Kind = kind, Placement = kind == ShopAdKind.Minor ? ShopAdPlacement.TopLeft : ShopAdPlacement.Left, SpacePercent = percent,
        PopupSeconds = kind == ShopAdKind.Popup ? 10 : 0, PopupEveryMinutes = kind == ShopAdKind.Popup ? 5 : 0,
        StartAt = start ?? Monday, EndAt = (start ?? Monday).AddDays(days),
        CreatedByUserId = source == ShopAdSource.SalesExecutive ? _executive.UserId : source == ShopAdSource.Admin ? _admin.UserId : _owner.UserId,
    };

    private ShopAd Stored(ShopAd ad)
    {
        _ads.Setup(r => r.GetByIdAsync(ad.Id)).ReturnsAsync(ad);
        return ad;
    }

    // ----- pricing an administrator ad

    [Fact]
    public async Task SendingInAnAdminAd_WithNoRateForThePlace_IsRefused()
    {
        var ad = Stored(Ad(ShopAdSource.Admin));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitAsync(_admin, ad.Id, Monday.AddDays(-1)));

        Assert.Contains("No rate is set", ex.Message);
        Assert.Equal(ShopAdStatus.Draft, ad.Status);
    }

    [Fact]
    public async Task SendingInAnAdminAd_FixesTheHourlyPriceAndTheShopShare()
    {
        RateIs(price: 200m, share: 70);
        var ad = Stored(Ad(ShopAdSource.Admin, percent: 25));

        await _service.SubmitAsync(_admin, ad.Id, Monday.AddDays(-1));

        Assert.Equal(ShopAdStatus.Approved, ad.Status);
        Assert.Equal(50m, ad.PricePerHour); // 25% of 200
        Assert.Equal(70, ad.ShopSharePercent);
    }

    [Fact]
    public async Task AnOwnersAdIsNotPriced_AndNeedsNoRate()
    {
        var ad = Stored(Ad(ShopAdSource.ShopOwner));

        await _service.SubmitAsync(_owner, ad.Id, Monday.AddDays(-1));

        Assert.Equal(ShopAdStatus.Approved, ad.Status);
        Assert.Null(ad.PricePerHour);
    }

    // ----- override

    [Fact]
    public async Task TheOwnerCanOverrideAnApprovedAdminAd_TheSlotIsFreed_AndAdminsAreToldWithTheReason()
    {
        var ad = Stored(Ad(ShopAdSource.Admin, ShopAdStatus.Approved));
        ad.DecidedAt = Monday.AddDays(-1);
        var now = Monday.AddDays(2);

        var dto = await _service.OverrideAsync(_owner, ad.Id, "  A local shop pays more  ", now);

        Assert.Equal(ShopAdStatus.Overridden, ad.Status);
        Assert.Equal(now, ad.StoppedAt);
        Assert.Equal("Overridden", dto.Status);
        Assert.False(ad.HoldsSlot);
        Assert.Equal("Overridden", Assert.Single(_events).Action);
        Assert.Equal("A local shop pays more", _events[0].Note);
        Assert.Equal(new[] { "admin-1", "admin-2" }, _notified.Select(n => n.UserId));
        Assert.All(_notified, n => Assert.Equal("AdOverridden", n.Kind));
        Assert.Contains("A local shop pays more", _notified[0].Message);
        Assert.Contains("Olive Mart", _notified[0].Message);
    }

    [Fact]
    public async Task OnlyTheOwnerOverrides_AndOnlyAnApprovedAdminAdThatIsNotOver()
    {
        var approved = Stored(Ad(ShopAdSource.Admin, ShopAdStatus.Approved));
        var now = Monday.AddDays(2);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.OverrideAsync(_executive, approved.Id, null, now));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.OverrideAsync(_admin, approved.Id, null, now));

        var ownAd = Stored(Ad(ShopAdSource.ShopOwner, ShopAdStatus.Approved));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.OverrideAsync(_owner, ownAd.Id, null, now));

        var pending = Stored(Ad(ShopAdSource.Admin, ShopAdStatus.Draft));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.OverrideAsync(_owner, pending.Id, null, now));

        var over = Stored(Ad(ShopAdSource.Admin, ShopAdStatus.Approved, days: 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.OverrideAsync(_owner, over.Id, null, Monday.AddDays(3)));

        var another = new AdActor(Guid.NewGuid(), "Other", ShopAdSource.ShopOwner, Guid.NewGuid());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.OverrideAsync(another, approved.Id, null, now));
    }

    [Fact]
    public async Task TheOwnerSeesTheOverrideOption_OnlyOnAnApprovedAdminAd()
    {
        var future = DateTime.UtcNow.Date.AddDays(1);
        var adminAd = Ad(ShopAdSource.Admin, ShopAdStatus.Approved, start: future);
        var ownAd = Ad(ShopAdSource.ShopOwner, ShopAdStatus.Approved, start: future);
        _ads.Setup(r => r.ListAsync(_shop.Id, null, null, false)).ReturnsAsync(new List<ShopAd> { adminAd, ownAd });

        var list = await _service.ListAsync(_owner, null, null, null);

        Assert.Contains("override", list.Single(a => a.Source == "Admin").Can);
        Assert.DoesNotContain("override", list.Single(a => a.Source == "ShopOwner").Can);
        Assert.DoesNotContain("cancel", list.Single(a => a.Source == "Admin").Can);
    }

    [Fact]
    public async Task CancellingAnApprovedAdRecordsWhenItStopped_ButADraftDoesNot()
    {
        var approved = Stored(Ad(ShopAdSource.ShopOwner, ShopAdStatus.Approved));
        var draft = Stored(Ad(ShopAdSource.ShopOwner));

        await _service.CancelAsync(_owner, approved.Id);
        await _service.CancelAsync(_owner, draft.Id);

        Assert.NotNull(approved.StoppedAt);
        Assert.Null(draft.StoppedAt);
    }

    // ----- the monthly statement

    [Fact]
    public void HoursRan_CountsOnlyTheMonthTheApprovalAndTheStop()
    {
        var zone = TimeZoneInfo.Utc;
        var ad = Ad(ShopAdSource.Admin, ShopAdStatus.Approved, start: Monday, days: 30);
        ad.DecidedAt = Monday.AddDays(2); // approved on the 9th
        var monthStart = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = new DateTime(2030, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2030, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        // 9 Jan to 1 Feb: 23 days of 24 hours
        Assert.Equal(23 * 24m, AdStatementService.HoursRan(ad, zone, monthStart, monthEnd, now));

        // stopped on the 12th at noon: 2.5 days
        ad.StoppedAt = new DateTime(2030, 1, 12, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(3.5m * 24, AdStatementService.HoursRan(ad, zone, monthStart, monthEnd, now));

        // the clock stops the count too: 'now' is the 10th at 06:00
        ad.StoppedAt = null;
        Assert.Equal(24m + 6, AdStatementService.HoursRan(ad, zone, monthStart, monthEnd, new DateTime(2030, 1, 10, 6, 0, 0, DateTimeKind.Utc)));

        // nothing in a month it never touched
        Assert.Equal(0m, AdStatementService.HoursRan(ad, zone, new DateTime(2029, 11, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2029, 12, 1, 0, 0, 0, DateTimeKind.Utc), now));
    }

    [Fact]
    public void HoursRan_RespectsDailyHoursAndWeekdaysOnTheShopClock()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        var ad = Ad(ShopAdSource.Admin, ShopAdStatus.Approved, start: new DateTime(2030, 1, 6, 0, 0, 0, DateTimeKind.Utc), days: 60);
        ad.DecidedAt = ad.StartAt;
        ad.SetDaily(new DailySchedule(9 * 60, 12 * 60, 2 | 4 | 8 | 16 | 32)); // Monday to Friday, 9 to 12
        var monthStart = new DateTime(2030, 1, 6, 18, 30, 0, DateTimeKind.Utc); // 7 Jan 00:00 in Kolkata
        var monthEnd = monthStart.AddDays(7);

        // one working week: five days of three hours
        Assert.Equal(15m, AdStatementService.HoursRan(ad, zone, monthStart, monthEnd, new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    private void Approved(params ShopAd[] ads) =>
        _ads.Setup(r => r.ListApprovedAsync(It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(ads.ToList());

    private static readonly DateTime After = new(2030, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task TheStatementPaysTheShopForAdminAdHours_AndOnlyListsHoursForItsOwnAds()
    {
        var paid = Ad(ShopAdSource.Admin, ShopAdStatus.Approved, start: new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), days: 2);
        paid.DecidedAt = paid.StartAt;
        paid.PricePerHour = 12.50m;
        paid.ShopSharePercent = 70;
        var own = Ad(ShopAdSource.ShopOwner, ShopAdStatus.Approved, start: new DateTime(2030, 1, 10, 0, 0, 0, DateTimeKind.Utc), days: 1);
        own.Headline = "Own offer";
        own.DecidedAt = own.StartAt;
        Approved(paid, own);

        var statement = await _statements.GetAsync(_owner, "2030-01", null, After);

        var shop = Assert.Single(statement.Shops);
        Assert.Equal(48m, shop.AdminAdHours);
        Assert.Equal(24m, shop.OwnAdHours);
        Assert.Equal(420.00m, shop.Payout); // 48 h x 12.50 x 70%
        Assert.Equal(420.00m, statement.TotalPayout);
        Assert.Equal(420.00m, shop.Lines.Single(l => l.Source == "Admin").Payout);
        Assert.Null(shop.Lines.Single(l => l.Headline == "Own offer").Payout);
    }

    [Fact]
    public async Task AnOverriddenAdIsPaidOnlyForTheHoursItRan()
    {
        var ad = Ad(ShopAdSource.Admin, ShopAdStatus.Overridden, start: new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), days: 10);
        ad.DecidedAt = ad.StartAt;
        ad.StoppedAt = new DateTime(2030, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        ad.PricePerHour = 10m;
        ad.ShopSharePercent = 100;
        Approved(ad);

        var statement = await _statements.GetAsync(_admin, "2030-01", null, After);

        Assert.Equal(360m, statement.TotalPayout); // 36 h x 10
    }

    [Fact]
    public async Task TheStatementIsForOwnersAndAdminsOnly_ForARealMonth_AndOwnersSeeOnlyTheirShop()
    {
        Approved();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _statements.GetAsync(_executive, "2030-01", null, After));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _statements.GetAsync(_owner, "2030-01", Guid.NewGuid(), After));
        await Assert.ThrowsAsync<ArgumentException>(() => _statements.GetAsync(_owner, "January", null, After));
        await Assert.ThrowsAsync<ArgumentException>(() => _statements.GetAsync(_owner, "2030-13", null, After));

        var empty = await _statements.GetAsync(_owner, "2030-01", null, After);
        Assert.Empty(empty.Shops);
        Assert.Equal(0m, empty.TotalPayout);

        _ads.Verify(r => r.ListApprovedAsync(_shop.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
    }
}

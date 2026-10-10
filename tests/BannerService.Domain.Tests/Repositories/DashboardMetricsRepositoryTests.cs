namespace BannerService.Domain.Tests.Repositories;

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;

public class DashboardMetricsRepositoryTests : IAsyncLifetime
{
    private ApplicationDbContext _context = null!;
    private DashboardMetricsRepository _repository = null!;
    private readonly Guid _shopA = Guid.NewGuid();
    private readonly Guid _shopB = Guid.NewGuid();

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _repository = new DashboardMetricsRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task EmptyDatabase_ReturnsZeroCounts()
    {
        var shops = await _repository.GetShopCountsAsync();
        var users = await _repository.GetUserCountsAsync();
        var banners = await _repository.GetBannerCountsAsync();
        var payments = await _repository.GetPaymentCountsAsync();

        Assert.Equal(0, shops.Total);
        Assert.Equal(0, users.Total);
        Assert.Equal(0, banners.Banners);
        Assert.Equal(0L, banners.MediaBytes);
        Assert.Equal(0, payments.Total);
        Assert.Equal(0m, payments.AverageAmount);
        Assert.Null(payments.LastPaidDate);
    }

    [Fact]
    public async Task GetShopCounts_CountsTopLevelActiveAndSubscribedShops()
    {
        var parent = new Shop { Id = _shopA, Name = "Parent", Status = ShopStatus.Active };
        var child = new Shop { Id = _shopB, Name = "Child", ParentShopId = _shopA, Status = ShopStatus.Inactive };
        _context.Set<Shop>().AddRange(parent, child);
        _context.Set<Subscription>().AddRange(
            new Subscription { Id = Guid.NewGuid(), ShopId = _shopA, Status = SubscriptionStatus.Active },
            new Subscription { Id = Guid.NewGuid(), ShopId = _shopA, Status = SubscriptionStatus.Active },
            new Subscription { Id = Guid.NewGuid(), ShopId = _shopB, Status = SubscriptionStatus.Cancelled });
        await _context.SaveChangesAsync();

        var counts = await _repository.GetShopCountsAsync();

        Assert.Equal(2, counts.Total);
        Assert.Equal(1, counts.TopLevel);
        Assert.Equal(1, counts.Active);
        Assert.Equal(1, counts.WithActiveSubscription);
        Assert.Equal(3, await _repository.GetSubscriptionCountAsync());
    }

    [Fact]
    public async Task GetUserCounts_AllUsers_IncludesRoleCounts()
    {
        _context.Set<User>().AddRange(
            new User { Id = "u1", ShopId = _shopA.ToString(), IsActive = true },
            new User { Id = "u2", ShopId = _shopA.ToString(), IsActive = false, LoginAttempts = 2 },
            new User { Id = "u3", ShopId = _shopB.ToString(), IsActive = true, IsLockedOut = true });
        _context.Set<UserRole>().AddRange(
            new UserRole { UserId = "u1", RoleId = "1" },
            new UserRole { UserId = "u2", RoleId = "2" },
            new UserRole { UserId = "u3", RoleId = "2" },
            new UserRole { UserId = "u3", RoleId = "3" });
        await _context.SaveChangesAsync();

        var counts = await _repository.GetUserCountsAsync();

        Assert.Equal(3, counts.Total);
        Assert.Equal(2, counts.Active);
        Assert.Equal(1, counts.WithFailedLogin);
        Assert.Equal(1, counts.LockedOut);
        Assert.Equal(1, counts.AdminRole);
        Assert.Equal(2, counts.ShopOwnerRole);
        Assert.Equal(1, counts.SalesExecutiveRole);
        Assert.Equal(3, await _repository.GetUserTotalCountAsync());
    }

    [Fact]
    public async Task GetUserCounts_ForShop_OnlyCountsThatShopsUsers()
    {
        _context.Set<User>().AddRange(
            new User { Id = "u1", ShopId = _shopA.ToString(), IsActive = true },
            new User { Id = "u2", ShopId = _shopA.ToString(), IsActive = false },
            new User { Id = "u3", ShopId = _shopB.ToString(), IsActive = true, IsLockedOut = true });
        await _context.SaveChangesAsync();

        var counts = await _repository.GetUserCountsAsync(_shopA);

        Assert.Equal(2, counts.Total);
        Assert.Equal(1, counts.Active);
        Assert.Equal(0, counts.LockedOut);
        Assert.Equal(0, counts.AdminRole);
    }

    [Fact]
    public async Task GetBannerCounts_ForShop_ScopesBannersComponentsEffectsAndMedia()
    {
        var bannerA = new Banner(_shopA, Guid.NewGuid(), "A", "", 100, 100) { IsPublished = true };
        var bannerA2 = new Banner(_shopA, Guid.NewGuid(), "A2", "", 100, 100);
        var bannerB = new Banner(_shopB, Guid.NewGuid(), "B", "", 100, 100);
        _context.Set<Banner>().AddRange(bannerA, bannerA2, bannerB);

        var compA = new Component { Id = Guid.NewGuid(), BannerId = bannerA.Id };
        var compB = new Component { Id = Guid.NewGuid(), BannerId = bannerB.Id };
        _context.Set<Component>().AddRange(compA, compB);
        _context.Set<Effect>().AddRange(
            new Effect { Id = Guid.NewGuid(), ComponentId = compA.Id },
            new Effect { Id = Guid.NewGuid(), ComponentId = compA.Id },
            new Effect { Id = Guid.NewGuid(), ComponentId = compB.Id });
        _context.Set<MediaFile>().AddRange(
            new MediaFile { Id = Guid.NewGuid(), ShopId = _shopA, SizeBytes = 1000 },
            new MediaFile { Id = Guid.NewGuid(), ShopId = _shopA, SizeBytes = 500 },
            new MediaFile { Id = Guid.NewGuid(), ShopId = _shopB, SizeBytes = 7 });
        await _context.SaveChangesAsync();

        var shop = await _repository.GetBannerCountsAsync(_shopA);
        var all = await _repository.GetBannerCountsAsync();

        Assert.Equal(2, shop.Banners);
        Assert.Equal(1, shop.Published);
        Assert.Equal(1, shop.Components);
        Assert.Equal(2, shop.Effects);
        Assert.Equal(1500L, shop.MediaBytes);

        Assert.Equal(3, all.Banners);
        Assert.Equal(2, all.Components);
        Assert.Equal(3, all.Effects);
        Assert.Equal(1507L, all.MediaBytes);
    }

    [Fact]
    public async Task GetRecentBannerSummaries_ReturnsNewestFirstWithChildCounts()
    {
        var older = new Banner(_shopA, Guid.NewGuid(), "Older", "", 100, 100) { CreatedAt = DateTime.UtcNow.AddDays(-2) };
        var newer = new Banner(_shopA, Guid.NewGuid(), "Newer", "", 100, 100) { CreatedAt = DateTime.UtcNow.AddDays(-1) };
        _context.Set<Banner>().AddRange(older, newer);
        var comp = new Component { Id = Guid.NewGuid(), BannerId = newer.Id };
        _context.Set<Component>().Add(comp);
        _context.Set<Effect>().Add(new Effect { Id = Guid.NewGuid(), ComponentId = comp.Id });
        await _context.SaveChangesAsync();

        var result = await _repository.GetRecentBannerSummariesAsync(_shopA, 10);
        var limited = await _repository.GetRecentBannerSummariesAsync(_shopA, 1);

        Assert.Equal(new[] { "Newer", "Older" }, result.Select(r => r.Name));
        Assert.Equal(1, result[0].ComponentCount);
        Assert.Equal(1, result[0].EffectCount);
        Assert.Equal(0, result[1].ComponentCount);
        Assert.Single(limited);
    }

    [Fact]
    public async Task GetPaymentCounts_CountsByStatusAndTracksLastPaidDate()
    {
        var paid = DateTime.UtcNow.AddDays(-3);
        var latest = DateTime.UtcNow.AddDays(-1);
        _context.Set<Invoice>().AddRange(
            new Invoice { Id = Guid.NewGuid(), Amount = 100m, Status = InvoiceStatus.Paid, PaidDate = paid },
            new Invoice { Id = Guid.NewGuid(), Amount = 300m, Status = InvoiceStatus.Paid, PaidDate = latest },
            new Invoice { Id = Guid.NewGuid(), Amount = 200m, Status = InvoiceStatus.Overdue },
            new Invoice { Id = Guid.NewGuid(), Amount = 0m, Status = InvoiceStatus.Refunded });
        _context.Set<Subscription>().Add(
            new Subscription { Id = Guid.NewGuid(), ShopId = _shopA, Status = SubscriptionStatus.GracePeriod });
        await _context.SaveChangesAsync();

        var counts = await _repository.GetPaymentCountsAsync();

        Assert.Equal(4, counts.Total);
        Assert.Equal(2, counts.Paid);
        Assert.Equal(1, counts.Overdue);
        Assert.Equal(1, counts.Refunded);
        Assert.Equal(150m, counts.AverageAmount);
        Assert.Equal(latest, counts.LastPaidDate);
        Assert.Equal(1, counts.SubscriptionsInGracePeriod);
        Assert.Equal(4, await _repository.GetInvoiceCountAsync());
    }

    [Fact]
    public async Task GetAllPlans_IncludesInactivePlans()
    {
        _context.Set<SubscriptionPlan>().AddRange(
            new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Active", IsActive = true },
            new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Retired", IsActive = false });
        await _context.SaveChangesAsync();

        var plans = await _repository.GetAllPlansAsync();

        Assert.Equal(2, plans.Count);
    }
}

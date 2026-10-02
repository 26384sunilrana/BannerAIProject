using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BannerService.Domain.Tests.Repositories;

public class SubscriptionPagingTests
{
    private static async Task<SubscriptionRepository> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new ApplicationDbContext(options);
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Silver", MonthlyPrice = 10, AnnualPrice = 100 };
        context.SubscriptionPlans.Add(plan);

        var today = DateTime.UtcNow.Date;
        var shops = new[] { "Olive Mart", "Blue Bakery", "Olive Garden", "Corner Shop" };
        for (var i = 0; i < shops.Length; i++)
        {
            var shop = new Shop { Id = Guid.NewGuid(), Name = shops[i] };
            context.Shops.Add(shop);
            context.Subscriptions.Add(new Subscription
            {
                Id = Guid.NewGuid(), ShopId = shop.Id, PlanId = plan.Id,
                Status = i == 3 ? SubscriptionStatus.GracePeriod : SubscriptionStatus.Active,
                RenewalDate = today.AddDays(10 - i * 5)   // 10, 5, 0, -5 days
            });
        }
        await context.SaveChangesAsync();
        return new SubscriptionRepository(context);
    }

    [Fact]
    public async Task ListsSoonestRenewalFirst_WithShopAndPlan()
    {
        var repository = await SeedAsync();

        var (items, total) = await repository.GetPagedAsync(null, null, 1, 10);

        Assert.Equal(4, total);
        Assert.Equal(new[] { "Corner Shop", "Olive Garden", "Blue Bakery", "Olive Mart" }, items.Select(s => s.Shop!.Name));
        Assert.All(items, s => Assert.Equal("Silver", s.Plan!.Name));
    }

    [Fact]
    public async Task FiltersByStatusAndShopName_IgnoringCase()
    {
        var repository = await SeedAsync();

        var (grace, graceTotal) = await repository.GetPagedAsync(SubscriptionStatus.GracePeriod, null, 1, 10);
        var (olives, oliveTotal) = await repository.GetPagedAsync(null, "  OLIVE ", 1, 10);

        Assert.Equal(1, graceTotal);
        Assert.Equal("Corner Shop", grace.Single().Shop!.Name);
        Assert.Equal(2, oliveTotal);
        Assert.All(olives, s => Assert.Contains("Olive", s.Shop!.Name));
    }

    [Fact]
    public async Task PagesWithoutLosingTheTotal()
    {
        var repository = await SeedAsync();

        var (page2, total) = await repository.GetPagedAsync(null, null, 2, 3);

        Assert.Equal(4, total);
        Assert.Single(page2);
    }
}

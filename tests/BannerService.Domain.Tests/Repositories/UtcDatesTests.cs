using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;
using BannerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BannerService.Domain.Tests.Repositories;

public class UtcDatesTests
{
    [Fact]
    public async Task DatesComeBackMarkedAsUtc_SoClientsDoNotReadThemAsLocalTime()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var banner = new Banner(Guid.NewGuid(), Guid.NewGuid(), "Sale", "d", 100, 100);
        banner.SetSchedule(new PublishWindow(
            DateTime.SpecifyKind(new DateTime(2026, 11, 3, 9, 0, 0), DateTimeKind.Unspecified),
            DateTime.SpecifyKind(new DateTime(2026, 11, 3, 12, 0, 0), DateTimeKind.Unspecified)));

        await using (var write = new ApplicationDbContext(options))
        {
            write.Banners.Add(banner);
            await write.SaveChangesAsync();
        }

        await using var read = new ApplicationDbContext(options);
        var loaded = await read.Banners.SingleAsync(b => b.Id == banner.Id);

        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, loaded.PublishStartAt!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, loaded.PublishEndAt!.Value.Kind);
        Assert.Equal(new DateTime(2026, 11, 3, 9, 0, 0), loaded.PublishStartAt.Value);
    }

    [Fact]
    public async Task LocalTimesAreStoredAsUtc()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var local = new DateTime(2026, 11, 3, 9, 0, 0, DateTimeKind.Local);
        var banner = new Banner(Guid.NewGuid(), Guid.NewGuid(), "Sale", "d", 100, 100);
        banner.SetSchedule(new PublishWindow(local, local.AddHours(3)));

        await using (var write = new ApplicationDbContext(options))
        {
            write.Banners.Add(banner);
            await write.SaveChangesAsync();
        }

        await using var read = new ApplicationDbContext(options);
        var loaded = await read.Banners.SingleAsync(b => b.Id == banner.Id);

        Assert.Equal(local.ToUniversalTime(), loaded.PublishStartAt);
    }
}

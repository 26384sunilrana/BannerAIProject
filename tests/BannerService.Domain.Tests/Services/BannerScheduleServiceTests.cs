using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using BannerService.Domain.ValueObjects;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class BannerScheduleServiceTests
{
    private readonly BannerScheduleService _service = new();
    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private static DateTime At(int hour, int day = 1) => new(2026, 11, day, hour, 0, 0, DateTimeKind.Utc);

    private Banner NewBanner(string name = "Banner") => new(_shopId, _userId, name, "desc", 1200, 600);

    private Banner Scheduled(string name, int fromHour, int toHour)
    {
        var banner = NewBanner(name);
        banner.SetSchedule(new PublishWindow(At(fromHour), At(toHour)));
        return banner;
    }

    [Fact]
    public void PublishWindow_EndNotAfterStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PublishWindow(At(10), At(10)));
        Assert.Throws<ArgumentException>(() => new PublishWindow(At(10), At(9)));
    }

    [Fact]
    public void PublishWindow_BackToBackWindows_DoNotOverlap()
    {
        var morning = new PublishWindow(At(9), At(12));
        var afternoon = new PublishWindow(At(12), At(15));

        Assert.False(morning.Overlaps(afternoon));
        Assert.False(afternoon.Overlaps(morning));
    }

    [Fact]
    public void PublishWindow_Contains_IsHalfOpen()
    {
        var window = new PublishWindow(At(9), At(12));

        Assert.True(window.Contains(At(9)));
        Assert.True(window.Contains(At(11)));
        Assert.False(window.Contains(At(12)));
    }

    [Fact]
    public void ValidateNoOverlap_OverlappingSameDayHours_Throws()
    {
        var existing = Scheduled("Morning sale", 9, 13);
        var candidate = NewBanner("Lunch offer");

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateNoOverlap(candidate, new PublishWindow(At(12), At(15)), new[] { existing }));
    }

    [Fact]
    public void ValidateNoOverlap_AdjacentHours_Passes()
    {
        var existing = Scheduled("Morning sale", 9, 12);
        var candidate = NewBanner("Lunch offer");

        _service.ValidateNoOverlap(candidate, new PublishWindow(At(12), At(15)), new[] { existing });
    }

    [Fact]
    public void ValidateNoOverlap_IgnoresTheBannerBeingRescheduled()
    {
        var banner = Scheduled("Morning sale", 9, 12);

        _service.ValidateNoOverlap(banner, new PublishWindow(At(10), At(13)), new[] { banner });
    }

    [Fact]
    public void ValidateNoOverlap_IgnoresOtherShops()
    {
        var other = new Banner(Guid.NewGuid(), _userId, "Other shop", "d", 100, 100);
        other.SetSchedule(new PublishWindow(At(9), At(12)));

        _service.ValidateNoOverlap(NewBanner(), new PublishWindow(At(9), At(12)), new[] { other });
    }

    [Fact]
    public void SetSchedule_FirstTime_DoesNotRequireReapproval()
    {
        var banner = NewBanner();

        Assert.False(banner.SetSchedule(new PublishWindow(At(9), At(12))));
    }

    [Fact]
    public void SetSchedule_ChangingPublishedBanner_RequiresReapprovalAndUnpublishes()
    {
        var banner = Scheduled("Sale", 9, 12);
        banner.Publish();

        var requiresReapproval = banner.SetSchedule(new PublishWindow(At(10), At(12)));

        Assert.True(requiresReapproval);
        Assert.False(banner.IsPublished);
    }

    [Fact]
    public void SetSchedule_SameWindow_DoesNotRequireReapproval()
    {
        var banner = Scheduled("Sale", 9, 12);
        banner.Publish();

        Assert.False(banner.SetSchedule(new PublishWindow(At(9), At(12))));
        Assert.True(banner.IsPublished);
    }

    [Fact]
    public void ResolveActive_ReturnsPublishedBannerWhoseWindowContainsNow()
    {
        var live = Scheduled("Live", 9, 12);
        var workflow = new PublishWorkflow { BannerId = live.Id, Status = PublishStatus.Published };

        var result = _service.ResolveActive(new[] { (live, (PublishWorkflow?)workflow) }, At(10));

        Assert.Same(live, result);
    }

    [Fact]
    public void ResolveActive_NothingInWindow_ReturnsNullSoClientUsesDefaultBanner()
    {
        var banner = Scheduled("Later", 15, 18);
        var workflow = new PublishWorkflow { BannerId = banner.Id, Status = PublishStatus.Published };

        Assert.Null(_service.ResolveActive(new[] { (banner, (PublishWorkflow?)workflow) }, At(10)));
    }

    [Fact]
    public void ResolveActive_NotYetPublished_ReturnsNull()
    {
        var banner = Scheduled("Pending", 9, 12);
        var workflow = new PublishWorkflow { BannerId = banner.Id, Status = PublishStatus.PendingApproval };

        Assert.Null(_service.ResolveActive(new[] { (banner, (PublishWorkflow?)workflow) }, At(10)));
        Assert.Null(_service.ResolveActive(new[] { (banner, (PublishWorkflow?)null) }, At(10)));
    }

    [Fact]
    public void ResubmitForApproval_ResetsWorkflowToPending()
    {
        var workflow = new PublishWorkflow { Status = PublishStatus.Published, PublishedAt = DateTime.UtcNow, ApprovedAt = DateTime.UtcNow };

        workflow.ResubmitForApproval(_userId, "owner", "Publish schedule changed");

        Assert.Equal(PublishStatus.PendingApproval, workflow.Status);
        Assert.Null(workflow.PublishedAt);
        Assert.Null(workflow.ApprovedAt);
        Assert.Contains(workflow.Events, e => e.Comment == "Publish schedule changed");
    }
}

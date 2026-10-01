using BannerService.Domain.Entities;
using Xunit;

namespace BannerService.Domain.Tests.Entities;

public class SubscriptionPeriodTests
{
    private static SubscriptionPlan Plan() => new() { Name = "Basic", MonthlyPrice = 30m, AnnualPrice = 300m };

    [Theory]
    [InlineData(BillingPeriod.Monthly, 1)]
    [InlineData(BillingPeriod.Quarterly, 3)]
    [InlineData(BillingPeriod.HalfYearly, 6)]
    [InlineData(BillingPeriod.Annual, 12)]
    public void Months_ReturnsPeriodLength(BillingPeriod period, int months)
    {
        Assert.Equal(months, period.Months());
    }

    [Fact]
    public void GetPrice_QuarterlyAndHalfYearly_AreProRataOfYearly()
    {
        var plan = Plan();

        Assert.Equal(75m, plan.GetPrice(BillingPeriod.Quarterly));
        Assert.Equal(150m, plan.GetPrice(BillingPeriod.HalfYearly));
        Assert.Equal(300m, plan.GetPrice(BillingPeriod.Annual));
    }

    [Fact]
    public void AddPeriod_AddsCalendarMonths()
    {
        var from = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), BillingPeriod.Quarterly.AddPeriod(from));
        Assert.Equal(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), BillingPeriod.HalfYearly.AddPeriod(from));
    }

    [Fact]
    public void Subscription_AutoRenew_DefaultsToTrue()
    {
        Assert.True(new Subscription().AutoRenew);
    }

    [Fact]
    public void SchedulePlanChange_TakesEffectNextDay_NotImmediately()
    {
        var oldPlan = Guid.NewGuid();
        var newPlan = Guid.NewGuid();
        var sub = new Subscription { PlanId = oldPlan, CurrentPrice = 100m };
        var now = new DateTime(2026, 10, 1, 14, 30, 0, DateTimeKind.Utc);

        sub.SchedulePlanChange(newPlan, 200m, now);

        Assert.Equal(new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc), sub.PendingPlanEffectiveAt);
        Assert.False(sub.ApplyPendingPlanChange(now));
        Assert.Equal(oldPlan, sub.PlanId);
        Assert.Equal(100m, sub.CurrentPrice);
    }

    [Fact]
    public void ApplyPendingPlanChange_OnNextDay_SwitchesPlanAndClearsPending()
    {
        var oldPlan = Guid.NewGuid();
        var newPlan = Guid.NewGuid();
        var sub = new Subscription { PlanId = oldPlan, CurrentPrice = 100m };
        sub.SchedulePlanChange(newPlan, 200m, new DateTime(2026, 10, 1, 14, 30, 0, DateTimeKind.Utc));

        var applied = sub.ApplyPendingPlanChange(new DateTime(2026, 10, 2, 0, 5, 0, DateTimeKind.Utc));

        Assert.True(applied);
        Assert.Equal(newPlan, sub.PlanId);
        Assert.Equal(oldPlan, sub.PreviousPlanId);
        Assert.Equal(200m, sub.CurrentPrice);
        Assert.Null(sub.PendingPlanId);
        Assert.Null(sub.PendingPlanEffectiveAt);
    }
}

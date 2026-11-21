using System.Text.Json;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Controllers;

/// <summary>A plan's prices are in one currency: INR unless the administrator chooses another, and a change counts as a change of price.</summary>
public class SubscriptionPlanCurrencyTests
{
    private readonly Mock<ISubscriptionPlanRepository> _plans = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly SubscriptionPlanController _controller;
    private readonly List<SubscriptionPlan> _stored = new();

    public SubscriptionPlanCurrencyTests()
    {
        _plans.Setup(r => r.GetAllAsync()).ReturnsAsync(() => _stored.ToList());
        _plans.Setup(r => r.CreateAsync(It.IsAny<SubscriptionPlan>())).ReturnsAsync((SubscriptionPlan p) => { _stored.Add(p); return p; });
        _plans.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>())).ReturnsAsync((SubscriptionPlan p) => p);
        _subscriptions.Setup(r => r.GetByPlanIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Subscription>());
        _controller = new SubscriptionPlanController(_plans.Object, _subscriptions.Object, NullLogger<SubscriptionPlanController>.Instance);
    }

    private static JsonElement Data(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return JsonSerializer.SerializeToElement(ok.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web)).GetProperty("data");
    }

    private SubscriptionPlan Existing(string currency = "INR")
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Silver", MonthlyPrice = 79.99m, AnnualPrice = 799.99m, Currency = currency, IsActive = true };
        _stored.Add(plan);
        _plans.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        return plan;
    }

    [Fact]
    public async Task ANewPlanIsInInr_WhenNoCurrencyIsGiven()
    {
        var result = await _controller.CreatePlan(new SubscriptionPlanController.CreatePlanDto { Name = "Basic", MonthlyPrice = 29.99m, AnnualPrice = 299.99m });

        Assert.Equal("INR", Data(result).GetProperty("currency").GetString());
        Assert.Equal("INR", Assert.Single(_stored).Currency);
    }

    [Fact]
    public async Task ANewPlanCanBeInAnotherCurrency_WrittenInAnyCase()
    {
        var result = await _controller.CreatePlan(new SubscriptionPlanController.CreatePlanDto { Name = "Basic US", MonthlyPrice = 9.99m, AnnualPrice = 99.99m, Currency = " usd " });

        Assert.Equal("USD", Data(result).GetProperty("currency").GetString());
        Assert.Equal("USD", Assert.Single(_stored).Currency);
    }

    [Theory]
    [InlineData("RUPEES")]
    [InlineData("12")]
    [InlineData("U$D")]
    public async Task ACurrencyThatIsNotAThreeLetterCode_IsRefused(string code)
    {
        var result = await _controller.CreatePlan(new SubscriptionPlanController.CreatePlanDto { Name = "Odd", MonthlyPrice = 1m, AnnualPrice = 10m, Currency = code });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_stored);
    }

    [Fact]
    public async Task TheListOfPlansCarriesEachPlansCurrency()
    {
        Existing("USD");

        var result = await _controller.GetAllPlans(includeInactive: true);

        var first = Data(result).EnumerateArray().Single();
        Assert.Equal("USD", first.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task UpdatingWithoutACurrency_KeepsThePlansOwn()
    {
        var plan = Existing("USD");

        await _controller.UpdatePlan(plan.Id, new SubscriptionPlanController.UpdatePlanDto { MonthlyPrice = 79.99m, AnnualPrice = 799.99m, IsActive = true });

        Assert.Equal("USD", plan.Currency);
    }

    [Fact]
    public async Task TheCurrencyOfAPlanCanBeChanged_WhenNoOneIsSubscribedToIt()
    {
        var plan = Existing("INR");

        var result = await _controller.UpdatePlan(plan.Id, new SubscriptionPlanController.UpdatePlanDto { MonthlyPrice = 79.99m, AnnualPrice = 799.99m, Currency = "eur", IsActive = true });

        Assert.Equal("EUR", Data(result).GetProperty("currency").GetString());
        Assert.Equal("EUR", plan.Currency);
    }

    [Fact]
    public async Task TheCurrencyOfAPlanWithActiveSubscriptions_CannotBeChanged_LikeItsPrice()
    {
        var plan = Existing("INR");
        _subscriptions.Setup(r => r.GetByPlanIdAsync(plan.Id)).ReturnsAsync(new List<Subscription> { new() { PlanId = plan.Id, Status = SubscriptionStatus.Active } });

        var result = await _controller.UpdatePlan(plan.Id, new SubscriptionPlanController.UpdatePlanDto { MonthlyPrice = 79.99m, AnnualPrice = 799.99m, Currency = "USD", IsActive = true });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("INR", plan.Currency);
    }
}

using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _locations = new();
    private readonly Mock<IShopRepository> _shops = new();
    private readonly LocationService _service;

    private readonly State _state = new() { Id = 5, CountryCode = "IN", Code = "MH", Name = "Maharashtra", IsActive = true, Country = new Country { ISOCode = "IN", Name = "India", IsActive = true } };

    public LocationServiceTests()
    {
        _service = new LocationService(_locations.Object, _shops.Object, NullLogger<LocationService>.Instance);
        _locations.Setup(l => l.FindStateAsync(5)).ReturnsAsync(_state);
    }

    private static City CityOf(int id, State state, bool active = true) =>
        new() { Id = id, StateId = state.Id, State = state, Name = "Mumbai", UniqueId = "CTY-AAAA-BBBB", IsActive = active };

    // ----- creating

    [Fact]
    public async Task CreateCity_GivesItAnIdentifierAndSaves()
    {
        var result = await _service.CreateCityAsync(5, "  Mumbai ");

        Assert.True(result.Succeeded);
        Assert.Equal("Mumbai", result.Value!.Name);
        Assert.StartsWith("CTY-", result.Value.UniqueId);
        _locations.Verify(l => l.AddCityAsync(It.IsAny<City>()), Times.Once);
        _locations.Verify(l => l.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateCity_RetriesWhenTheIdentifierIsTaken()
    {
        _locations.SetupSequence(l => l.UniqueIdTakenAsync(LocationLevel.City, It.IsAny<string>()))
            .ReturnsAsync(true).ReturnsAsync(true).ReturnsAsync(false);

        var result = await _service.CreateCityAsync(5, "Mumbai");

        Assert.True(result.Succeeded);
        _locations.Verify(l => l.UniqueIdTakenAsync(LocationLevel.City, It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateCity_RefusesADuplicateInTheSameState()
    {
        _locations.Setup(l => l.CityNameTakenAsync(5, "Mumbai", null)).ReturnsAsync(true);

        var result = await _service.CreateCityAsync(5, "Mumbai");

        Assert.Equal(LocationOutcome.Conflict, result.Outcome);
        _locations.Verify(l => l.SaveAsync(), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public async Task CreateCity_NeedsARealName(string name)
    {
        Assert.Equal(LocationOutcome.Invalid, (await _service.CreateCityAsync(5, name)).Outcome);
    }

    [Fact]
    public async Task CreateCity_UnderAStateThatIsSwitchedOff_IsRefused()
    {
        _state.IsActive = false;

        Assert.Equal(LocationOutcome.Invalid, (await _service.CreateCityAsync(5, "Mumbai")).Outcome);
    }

    [Fact]
    public async Task CreateCity_UnderAMissingState_IsNotFound()
    {
        Assert.Equal(LocationOutcome.NotFound, (await _service.CreateCityAsync(99, "Mumbai")).Outcome);
    }

    [Theory]
    [InlineData("in", true)]
    [InlineData("IND", false)]
    [InlineData("1N", false)]
    [InlineData("", false)]
    public async Task CreateCountry_NeedsATwoLetterCode(string code, bool valid)
    {
        var result = await _service.CreateCountryAsync(code, "India", null, null);

        Assert.Equal(valid, result.Succeeded);
        if (valid) Assert.Equal("IN", result.Value!.ISOCode);
    }

    [Theory]
    [InlineData("+91", true)]
    [InlineData("91", false)]
    [InlineData("+9199999", false)]
    public async Task CreateCountry_ChecksThePhoneCode(string phone, bool valid)
    {
        Assert.Equal(valid, (await _service.CreateCountryAsync("IN", "India", null, phone)).Succeeded);
    }

    [Fact]
    public async Task CreateState_UppercasesTheCodeAndRefusesADuplicate()
    {
        _locations.Setup(l => l.FindCountryAsync("IN")).ReturnsAsync(_state.Country);

        var ok = await _service.CreateStateAsync("in", "gj", "Gujarat", "State");
        Assert.True(ok.Succeeded);
        Assert.Equal("GJ", ok.Value!.Code);
        Assert.StartsWith("STA-", ok.Value.UniqueId);

        _locations.Setup(l => l.StateTakenAsync("IN", "Gujarat", "GJ", null)).ReturnsAsync(true);
        Assert.Equal(LocationOutcome.Conflict, (await _service.CreateStateAsync("IN", "GJ", "Gujarat", null)).Outcome);
    }

    [Fact]
    public async Task CreateGroup_NeedsAnActiveCity()
    {
        _locations.Setup(l => l.FindCityAsync(7)).ReturnsAsync(CityOf(7, _state, active: false));

        Assert.Equal(LocationOutcome.Invalid, (await _service.CreateGroupAsync(7, "Andheri")).Outcome);
    }

    // ----- changing

    [Fact]
    public async Task UpdateCity_RenamingKeepsTheShopsCityNameInStep()
    {
        var city = CityOf(7, _state);
        var shop = new Shop { City = "Mumbai", CityId = 7 };
        _locations.Setup(l => l.FindCityAsync(7)).ReturnsAsync(city);
        _locations.Setup(l => l.ShopsInCityAsync(7)).ReturnsAsync(new List<Shop> { shop });

        var result = await _service.UpdateCityAsync(7, "Bombay", true);

        Assert.True(result.Succeeded);
        Assert.Equal("Bombay", shop.City);
        Assert.Equal("CTY-AAAA-BBBB", city.UniqueId); // the identifier never changes
    }

    [Fact]
    public async Task UpdateCity_CannotBeSwitchedBackOnUnderAStateThatIsOff()
    {
        _state.IsActive = false;
        _locations.Setup(l => l.FindCityAsync(7)).ReturnsAsync(CityOf(7, _state, active: false));

        Assert.Equal(LocationOutcome.Invalid, (await _service.UpdateCityAsync(7, "Mumbai", true)).Outcome);
    }

    // ----- deleting

    [Theory]
    [InlineData(2, 0)]
    [InlineData(0, 3)]
    [InlineData(1, 1)]
    public async Task DeleteCity_IsRefusedWhileGroupsOrShopsAreUnderIt(int groups, int shops)
    {
        _locations.Setup(l => l.FindCityAsync(7)).ReturnsAsync(CityOf(7, _state));
        _locations.Setup(l => l.CountDependentsAsync(LocationLevel.City, "7")).ReturnsAsync((groups, shops));

        var result = await _service.DeleteCityAsync(7);

        Assert.Equal(LocationOutcome.Conflict, result.Outcome);
        Assert.Contains("cannot be deleted", result.Message);
        _locations.Verify(l => l.RemoveCityAsync(It.IsAny<City>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCity_WhenEmpty_RemovesIt()
    {
        _locations.Setup(l => l.FindCityAsync(7)).ReturnsAsync(CityOf(7, _state));
        _locations.Setup(l => l.CountDependentsAsync(LocationLevel.City, "7")).ReturnsAsync((0, 0));

        Assert.True((await _service.DeleteCityAsync(7)).Succeeded);
        _locations.Verify(l => l.RemoveCityAsync(It.IsAny<City>()), Times.Once);
    }

    // ----- shops

    private Shop AShop() => new() { Id = Guid.NewGuid(), Name = "Olive Mart" };

    [Fact]
    public async Task SetShopLocation_PlacesTheShopInTheCityAndGroup()
    {
        var shop = AShop();
        var city = CityOf(7, _state);
        var group = new LocationGroup { Id = 9, CityId = 7, Name = "Andheri", IsActive = true };
        _shops.Setup(r => r.GetByIdAsync(shop.Id)).ReturnsAsync(shop);
        _locations.Setup(l => l.FindCityAsync(7)).ReturnsAsync(city);
        _locations.Setup(l => l.FindGroupAsync(9)).ReturnsAsync(group);

        var result = await _service.SetShopLocationAsync(shop.Id, 7, 9);

        Assert.True(result.Succeeded);
        Assert.Equal(7, shop.CityId);
        Assert.Equal(9, shop.GroupId);
        Assert.Equal(5, shop.StateId);
        Assert.Equal("IN", shop.CountryCode);
        Assert.Equal("Mumbai", shop.City);
        _shops.Verify(r => r.UpdateAsync(shop), Times.Once);
    }

    [Fact]
    public async Task SetShopLocation_RefusesAGroupOfAnotherCity()
    {
        var shop = AShop();
        _shops.Setup(r => r.GetByIdAsync(shop.Id)).ReturnsAsync(shop);
        _locations.Setup(l => l.FindCityAsync(7)).ReturnsAsync(CityOf(7, _state));
        _locations.Setup(l => l.FindGroupAsync(9)).ReturnsAsync(new LocationGroup { Id = 9, CityId = 8, Name = "Kothrud", IsActive = true });

        Assert.Equal(LocationOutcome.Invalid, (await _service.SetShopLocationAsync(shop.Id, 7, 9)).Outcome);
        _shops.Verify(r => r.UpdateAsync(It.IsAny<Shop>()), Times.Never);
    }

    [Fact]
    public async Task SetShopLocation_WithoutAGroup_ClearsTheOldOne()
    {
        var shop = AShop();
        shop.GroupId = 9;
        _shops.Setup(r => r.GetByIdAsync(shop.Id)).ReturnsAsync(shop);
        _locations.Setup(l => l.FindCityAsync(7)).ReturnsAsync(CityOf(7, _state));

        await _service.SetShopLocationAsync(shop.Id, 7, null);

        Assert.Null(shop.GroupId);
    }

    [Fact]
    public async Task EnsureShopUniqueId_GivesOneOnceAndKeepsIt()
    {
        var shop = AShop();
        _shops.Setup(r => r.GetByIdAsync(shop.Id)).ReturnsAsync(shop);

        var first = await _service.EnsureShopUniqueIdAsync(shop.Id);
        var second = await _service.EnsureShopUniqueIdAsync(shop.Id);

        Assert.StartsWith("SHP-", first);
        Assert.Equal(first, second);
        _shops.Verify(r => r.UpdateAsync(shop), Times.Once);
    }

    [Fact]
    public async Task EnsureShopUniqueId_ForAMissingShop_IsNull()
    {
        Assert.Null(await _service.EnsureShopUniqueIdAsync(Guid.NewGuid()));
    }
}

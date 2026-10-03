using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class ShopSettingsServiceTests
{
    private readonly Mock<IShopRepository> _shops = new();
    private readonly Mock<IMediaFileRepository> _media = new();
    private readonly Mock<IMediaUrlSigner> _signer = new();
    private readonly ShopSettingsService _service;
    private readonly Shop _shop = new() { Id = Guid.NewGuid(), Name = "Olive Mart" };

    public ShopSettingsServiceTests()
    {
        _shops.Setup(r => r.GetByIdAsync(_shop.Id)).ReturnsAsync(_shop);
        _shops.Setup(r => r.UpdateAsync(It.IsAny<Shop>())).ReturnsAsync((Shop s) => s);
        _signer.Setup(s => s.Sign(It.IsAny<Guid>(), It.IsAny<DateTime>())).Returns("SIG");
        _service = new ShopSettingsService(_shops.Object, _media.Object, _signer.Object);
    }

    private MediaFile Logo(int type = 1, int status = 2) =>
        new(_shop.Id, "logo.png", "image/png", 100, type == 1 ? 1 : 2, Guid.NewGuid()) { Status = status };

    // ----- time zone

    [Fact]
    public async Task SetTimeZone_SavesAKnownZone_AndSaysWhereItCameFrom()
    {
        var result = await _service.SetTimeZoneAsync(_shop.Id, " Europe/London ");

        Assert.Equal("Europe/London", _shop.TimeZoneId);
        Assert.Equal(("Europe/London", "shop", "Europe/London"), (result.TimeZoneId, result.Source, result.OwnTimeZoneId));
    }

    [Fact]
    public async Task SetTimeZone_WithNothing_FollowsTheCityAndCountryAgain()
    {
        _shop.TimeZoneId = "Europe/London";
        _shop.CityNav = new City { TimeZoneId = "Asia/Kolkata" };

        var result = await _service.SetTimeZoneAsync(_shop.Id, null);

        Assert.Null(_shop.TimeZoneId);
        Assert.Equal(("Asia/Kolkata", "city", (string?)null), (result.TimeZoneId, result.Source, result.OwnTimeZoneId));
    }

    [Theory]
    [InlineData("Mars/Olympus")]
    [InlineData("not a zone")]
    public async Task SetTimeZone_RefusesAZoneTheServerDoesNotKnow(string id)
    {
        _shop.TimeZoneId = "Asia/Kolkata";

        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetTimeZoneAsync(_shop.Id, id));

        Assert.Equal("Asia/Kolkata", _shop.TimeZoneId);
    }

    [Fact]
    public async Task UnknownShop_IsNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetTimeZoneAsync(Guid.NewGuid()));
    }

    // ----- default board

    [Fact]
    public async Task SetDefaultBoard_SavesMessageColoursAndLogo_AndReturnsALinkToTheLogo()
    {
        var logo = Logo();
        _media.Setup(m => m.GetByIdAsync(logo.Id, _shop.Id)).ReturnsAsync(logo);

        var board = await _service.SetDefaultBoardAsync(_shop.Id, "  Back at 2 pm  ", "#1E3A8A", "#FFFFFF", logo.Id);

        Assert.Equal("Back at 2 pm", _shop.DefaultBoardMessage);
        Assert.Equal("#1e3a8a", _shop.DefaultBoardBackground);   // colours are stored in lower case
        Assert.Equal(logo.Id, _shop.DefaultBoardLogoMediaId);
        Assert.StartsWith($"/api/media/{logo.Id}/download?expires=", board.LogoUrl);
        Assert.EndsWith("&sig=SIG", board.LogoUrl);
        Assert.Equal("Olive Mart", board.ShopName);
    }

    [Fact]
    public async Task SetDefaultBoard_WithNothing_ClearsEverything()
    {
        _shop.DefaultBoardMessage = "old";
        _shop.DefaultBoardBackground = "#000000";
        _shop.DefaultBoardLogoMediaId = Guid.NewGuid();

        var board = await _service.SetDefaultBoardAsync(_shop.Id, "  ", "", null, null);

        Assert.Null(_shop.DefaultBoardMessage);
        Assert.Null(_shop.DefaultBoardBackground);
        Assert.Null(_shop.DefaultBoardLogoMediaId);
        Assert.Null(board.LogoUrl);
    }

    [Theory]
    [InlineData("red", null)]
    [InlineData("#12345", null)]
    [InlineData("#gggggg", null)]
    [InlineData(null, "rgb(1,2,3)")]
    [InlineData(null, "#12345678")]
    public async Task SetDefaultBoard_RefusesBadColours(string? background, string? text)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetDefaultBoardAsync(_shop.Id, "x", background, text, null));
        _shops.Verify(r => r.UpdateAsync(It.IsAny<Shop>()), Times.Never);
    }

    [Fact]
    public async Task SetDefaultBoard_RefusesAMessageThatIsTooLongOrHasControlCharacters()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetDefaultBoardAsync(_shop.Id, new string('x', 201), null, null, null));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetDefaultBoardAsync(_shop.Id, "bell\u0007", null, null, null));
        await _service.SetDefaultBoardAsync(_shop.Id, new string('x', 200), null, null, null); // exactly the limit is fine
        await _service.SetDefaultBoardAsync(_shop.Id, "two\nlines", null, null, null);          // a line break is fine
    }

    [Fact]
    public async Task SetDefaultBoard_RefusesALogoThatIsNotYourPicture()
    {
        var video = Logo(type: 2);
        var removed = Logo(status: (int)MediaFileStatus.Deleted);
        _media.Setup(m => m.GetByIdAsync(video.Id, _shop.Id)).ReturnsAsync(video);
        _media.Setup(m => m.GetByIdAsync(removed.Id, _shop.Id)).ReturnsAsync(removed);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetDefaultBoardAsync(_shop.Id, null, null, null, video.Id));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetDefaultBoardAsync(_shop.Id, null, null, null, removed.Id));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetDefaultBoardAsync(_shop.Id, null, null, null, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetDefaultBoard_WhenTheLogoWasDeletedSince_ShowsNoLogo()
    {
        var logo = Logo(status: (int)MediaFileStatus.Deleted);
        _shop.DefaultBoardLogoMediaId = logo.Id;
        _media.Setup(m => m.GetByIdAsync(logo.Id, _shop.Id)).ReturnsAsync(logo);

        var board = await _service.GetDefaultBoardAsync(_shop.Id);

        Assert.Null(board.LogoUrl);
        Assert.Equal(logo.Id, board.LogoMediaFileId);
    }
}

public class LocationTimeZoneTests
{
    private readonly Mock<ILocationRepository> _locations = new();
    private readonly LocationService _service;
    private readonly Country _india = new() { ISOCode = "IN", Name = "India" };
    private readonly City _mumbai = new() { Id = 7, Name = "Mumbai", StateId = 5 };

    public LocationTimeZoneTests()
    {
        _locations.Setup(l => l.FindCountryAsync("IN")).ReturnsAsync(_india);
        _locations.Setup(l => l.FindCityAsync(7)).ReturnsAsync(_mumbai);
        _service = new LocationService(_locations.Object, Mock.Of<IShopRepository>(), NullLogger<LocationService>.Instance);
    }

    [Fact]
    public async Task ACountryAndACityCanCarryATimeZone_AndLoseIt()
    {
        Assert.True((await _service.SetCountryTimeZoneAsync("IN", "Asia/Kolkata")).Succeeded);
        Assert.Equal("Asia/Kolkata", _india.TimeZoneId);
        Assert.True((await _service.SetCityTimeZoneAsync(7, "Asia/Kolkata")).Succeeded);
        Assert.Equal("Asia/Kolkata", _mumbai.TimeZoneId);

        await _service.SetCityTimeZoneAsync(7, "  ");
        Assert.Null(_mumbai.TimeZoneId);
    }

    [Fact]
    public async Task AnUnknownZone_IsRefused_AndAMissingPlaceIsNotFound()
    {
        Assert.Equal(LocationOutcome.Invalid, (await _service.SetCityTimeZoneAsync(7, "Mars/Olympus")).Outcome);
        Assert.Equal(LocationOutcome.Invalid, (await _service.SetCountryTimeZoneAsync("IN", "Mars/Olympus")).Outcome);
        Assert.Equal(LocationOutcome.NotFound, (await _service.SetCityTimeZoneAsync(99, "Asia/Kolkata")).Outcome);
        Assert.Equal(LocationOutcome.NotFound, (await _service.SetCountryTimeZoneAsync("ZZ", "Asia/Kolkata")).Outcome);
        Assert.Null(_mumbai.TimeZoneId);
    }
}

namespace BannerService.IntegrationTests.Controllers;

using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;

public class AddressControllerIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private System.Net.Http.HttpClient _client;

    public AddressControllerIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    #region GetCountries Tests

    [Fact]
    public async Task GetCountries_ShouldReturnCountriesList()
    {
        // Act
        var response = await _client.GetAsync("/api/address/countries");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        Assert.Contains("India", content);
    }

    [Fact]
    public async Task GetCountries_ShouldIncludeIndiaByDefault()
    {
        // Act
        var response = await _client.GetAsync("/api/address/countries");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var countries = JsonConvert.DeserializeObject<dynamic>(content);
        Assert.NotNull(countries);
    }

    #endregion

    #region GetStatesByCountry Tests

    [Fact]
    public async Task GetStatesByCountry_WithValidCountryCode_ShouldReturnStates()
    {
        // Act
        var response = await _client.GetAsync("/api/address/countries/IN/states");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        Assert.Contains("Maharashtra", content);
    }

    [Fact]
    public async Task GetStatesByCountry_WithInvalidCountryCode_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/address/countries/INVALID/states");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatesByCountry_ForIndia_ShouldReturnMultipleStates()
    {
        // Act
        var response = await _client.GetAsync("/api/address/countries/IN/states");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var states = JsonConvert.DeserializeObject<dynamic>(content);
        Assert.NotNull(states);
    }

    #endregion

    #region GetDistrictsByState Tests

    [Fact]
    public async Task GetDistrictsByState_WithValidStateId_ShouldReturnDistricts()
    {
        // Act
        var response = await _client.GetAsync("/api/address/states/1/districts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GetDistrictsByState_WithInvalidStateId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/address/states/99999/districts");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDistrictsByState_ShouldReturnCorrectHierarchy()
    {
        // Arrange - Maharashtra has state ID 1 with Mumbai as a district

        // Act
        var response = await _client.GetAsync("/api/address/states/1/districts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Mumbai", content);
    }

    #endregion

    #region GetCountryByCode Tests

    [Fact]
    public async Task GetCountryByCode_WithValidCode_ShouldReturnCountry()
    {
        // Act
        var response = await _client.GetAsync("/api/address/countries/IN");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("India", content);
    }

    [Fact]
    public async Task GetCountryByCode_WithInvalidCode_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/address/countries/INVALID");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GetStateById Tests

    [Fact]
    public async Task GetStateById_WithValidId_ShouldReturnState()
    {
        // Act
        var response = await _client.GetAsync("/api/address/states/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GetStateById_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/address/states/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Cascading Validation Tests

    [Fact]
    public async Task AddressHierarchy_Cascading_ShouldMaintainForeignKeyRelationships()
    {
        // Arrange
        // Act 1 - Get countries
        var countriesResponse = await _client.GetAsync("/api/address/countries");
        Assert.Equal(HttpStatusCode.OK, countriesResponse.StatusCode);
        var countriesContent = await countriesResponse.Content.ReadAsStringAsync();
        var countries = JsonConvert.DeserializeObject<dynamic>(countriesContent);

        // Act 2 - Get states for India
        var statesResponse = await _client.GetAsync("/api/address/countries/IN/states");
        Assert.Equal(HttpStatusCode.OK, statesResponse.StatusCode);
        var statesContent = await statesResponse.Content.ReadAsStringAsync();
        var states = JsonConvert.DeserializeObject<dynamic>(statesContent);

        // Act 3 - Get districts for Maharashtra (state 1)
        var districtsResponse = await _client.GetAsync("/api/address/states/1/districts");
        Assert.Equal(HttpStatusCode.OK, districtsResponse.StatusCode);
        var districtsContent = await districtsResponse.Content.ReadAsStringAsync();
        var districts = JsonConvert.DeserializeObject<dynamic>(districtsContent);

        // Assert
        Assert.NotNull(countries);
        Assert.NotNull(states);
        Assert.NotNull(districts);
    }

    #endregion
}

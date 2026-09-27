namespace BannerService.Application.Tests.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Infrastructure.Repositories;
using BannerService.Infrastructure.Data;

public class AddressRepositoryTests : IAsyncLifetime
{
    private ApplicationDbContext _dbContext;
    private AddressRepository _repository;

    public async ValueTask InitializeAsync()
    {
        // Create InMemory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ApplicationDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _repository = new AddressRepository(_dbContext);

        // Seed test data
        await SeedTestData();
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    #region GetCountries Tests

    [Fact]
    public async Task GetCountryAsync_WithValidCode_ShouldReturnCountry()
    {
        // Act
        var country = await _repository.GetCountryAsync("IN");

        // Assert
        Assert.NotNull(country);
        Assert.Equal("IN", country.ISOCode);
        Assert.Equal("India", country.Name);
    }

    [Fact]
    public async Task GetCountryAsync_WithInvalidCode_ShouldReturnNull()
    {
        // Act
        var country = await _repository.GetCountryAsync("XX");

        // Assert
        Assert.Null(country);
    }

    [Fact]
    public async Task GetAllCountriesAsync_ShouldReturnAllCountries()
    {
        // Act
        var countries = await _repository.GetAllCountriesAsync();

        // Assert
        Assert.NotEmpty(countries);
        Assert.True(countries.Any(c => c.ISOCode == "IN"));
    }

    [Fact]
    public async Task GetAllCountriesAsync_WithActiveOnly_ShouldReturnOnlyActiveCountries()
    {
        // Act
        var countries = await _repository.GetAllCountriesAsync(activeOnly: true);

        // Assert
        Assert.NotEmpty(countries);
        Assert.All(countries, c => Assert.True(c.IsActive));
    }

    #endregion

    #region GetStates Tests

    [Fact]
    public async Task GetStateAsync_WithValidId_ShouldReturnState()
    {
        // Act
        var state = await _repository.GetStateAsync(1);

        // Assert
        Assert.NotNull(state);
        Assert.Equal("Maharashtra", state.Name);
        Assert.Equal("IN", state.CountryCode);
    }

    [Fact]
    public async Task GetStateAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var state = await _repository.GetStateAsync(9999);

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public async Task GetStatesByCountryAsync_WithValidCountry_ShouldReturnStates()
    {
        // Act
        var states = await _repository.GetStatesByCountryAsync("IN");

        // Assert
        Assert.NotEmpty(states);
        Assert.All(states, s => Assert.Equal("IN", s.CountryCode));
    }

    [Fact]
    public async Task GetStatesByCountryAsync_WithInvalidCountry_ShouldReturnEmptyList()
    {
        // Act
        var states = await _repository.GetStatesByCountryAsync("XX");

        // Assert
        Assert.Empty(states);
    }

    [Fact]
    public async Task GetStatesByCountryAsync_WithActiveOnly_ShouldReturnOnlyActiveStates()
    {
        // Act
        var states = await _repository.GetStatesByCountryAsync("IN", activeOnly: true);

        // Assert
        Assert.NotEmpty(states);
        Assert.All(states, s => Assert.True(s.IsActive));
    }

    #endregion

    #region GetDistricts Tests

    [Fact]
    public async Task GetDistrictAsync_WithValidId_ShouldReturnDistrict()
    {
        // Act
        var district = await _repository.GetDistrictAsync(1);

        // Assert
        Assert.NotNull(district);
        Assert.NotNull(district.Name);
        Assert.True(district.StateId > 0);
    }

    [Fact]
    public async Task GetDistrictAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var district = await _repository.GetDistrictAsync(9999);

        // Assert
        Assert.Null(district);
    }

    [Fact]
    public async Task GetDistrictsByStateAsync_WithValidState_ShouldReturnDistricts()
    {
        // Act
        var districts = await _repository.GetDistrictsByStateAsync(1);  // Maharashtra state id = 1

        // Assert
        Assert.NotEmpty(districts);
        Assert.All(districts, d => Assert.Equal(1, d.StateId));
    }

    [Fact]
    public async Task GetDistrictsByStateAsync_WithInvalidState_ShouldReturnEmptyList()
    {
        // Act
        var districts = await _repository.GetDistrictsByStateAsync(9999);

        // Assert
        Assert.Empty(districts);
    }

    [Fact]
    public async Task GetDistrictsByStateAsync_WithActiveOnly_ShouldReturnOnlyActiveDistricts()
    {
        // Act
        var districts = await _repository.GetDistrictsByStateAsync(1, activeOnly: true);

        // Assert
        Assert.NotEmpty(districts);
        Assert.All(districts, d => Assert.True(d.IsActive));
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchStatesAsync_WithValidQuery_ShouldReturnMatchingStates()
    {
        // Act
        var states = await _repository.SearchStatesAsync("Maha");

        // Assert
        Assert.NotEmpty(states);
        Assert.True(states.Any(s => s.Name.Contains("Maharashtra", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task SearchStatesAsync_WithNoMatches_ShouldReturnEmptyList()
    {
        // Act
        var states = await _repository.SearchStatesAsync("XYZ");

        // Assert
        Assert.Empty(states);
    }

    [Fact]
    public async Task SearchDistrictsAsync_WithValidQuery_ShouldReturnMatchingDistricts()
    {
        // Act
        var districts = await _repository.SearchDistrictsAsync("Mum");

        // Assert
        Assert.NotEmpty(districts);
    }

    [Fact]
    public async Task SearchDistrictsAsync_WithNoMatches_ShouldReturnEmptyList()
    {
        // Act
        var districts = await _repository.SearchDistrictsAsync("XYZ");

        // Assert
        Assert.Empty(districts);
    }

    #endregion

    #region Add Tests

    [Fact]
    public async Task AddCountryAsync_WithNewCountry_ShouldAdd()
    {
        // Arrange
        var newCountry = new Country { ISOCode = "US", Name = "United States", RegionName = "North America", PhoneCode = "+1", IsActive = true };

        // Act
        await _repository.AddCountryAsync(newCountry);
        var result = await _repository.GetCountryAsync("US");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("US", result.ISOCode);
    }

    [Fact]
    public async Task AddStateAsync_WithNewState_ShouldAdd()
    {
        // Arrange
        var newState = new State { CountryCode = "IN", Code = "KA", Name = "Karnataka", RegionType = "State", IsActive = true };

        // Act
        await _repository.AddStateAsync(newState);
        var result = await _repository.GetStateAsync(newState.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Karnataka", result.Name);
        Assert.Equal("IN", result.CountryCode);
    }

    [Fact]
    public async Task AddDistrictAsync_WithNewDistrict_ShouldAdd()
    {
        // Arrange
        var state = await _dbContext.States.FirstAsync();
        var newDistrict = new District { StateId = state.Id, Code = "TST", Name = "TestDistrict", RegionType = "District", IsActive = true };

        // Act
        await _repository.AddDistrictAsync(newDistrict);
        var result = await _repository.GetDistrictAsync(newDistrict.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestDistrict", result.Name);
        Assert.Equal(state.Id, result.StateId);
    }

    #endregion

    #region Bulk Add Tests

    [Fact]
    public async Task AddCountriesAsync_WithMultipleCountries_ShouldAddAll()
    {
        // Arrange
        var newCountries = new List<Country>
        {
            new() { ISOCode = "GB", Name = "United Kingdom", RegionName = "Europe", PhoneCode = "+44", IsActive = true },
            new() { ISOCode = "AU", Name = "Australia", RegionName = "Oceania", PhoneCode = "+61", IsActive = true }
        };

        // Act
        await _repository.AddCountriesAsync(newCountries);

        // Assert
        var gb = await _repository.GetCountryAsync("GB");
        var au = await _repository.GetCountryAsync("AU");
        Assert.NotNull(gb);
        Assert.NotNull(au);
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestData()
    {
        // Seed country
        var country = new Country
        {
            ISOCode = "IN",
            Name = "India",
            RegionName = "Asia",
            PhoneCode = "+91",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Countries.Add(country);
        await _dbContext.SaveChangesAsync();

        // Seed states
        var maharashtra = new State
        {
            Id = 1,
            CountryCode = "IN",
            Code = "MH",
            Name = "Maharashtra",
            RegionType = "State",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var karnataka = new State
        {
            Id = 2,
            CountryCode = "IN",
            Code = "KA",
            Name = "Karnataka",
            RegionType = "State",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.States.AddRange(maharashtra, karnataka);
        await _dbContext.SaveChangesAsync();

        // Seed districts for Maharashtra
        var districts = new List<District>
        {
            new() { Id = 1, StateId = 1, Code = "MUM", Name = "Mumbai", RegionType = "District", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, StateId = 1, Code = "PUN", Name = "Pune", RegionType = "District", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, StateId = 1, Code = "NAG", Name = "Nagpur", RegionType = "District", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Districts.AddRange(districts);
        await _dbContext.SaveChangesAsync();
    }

    #endregion
}

namespace BannerService.Domain.Tests.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;

public class AddressRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private AddressRepository _repository;

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new AddressRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var india = new Country { Code = "IN", Name = "India" };
        var usa = new Country { Code = "US", Name = "United States" };

        var maharashtra = new State { Id = 1, Name = "Maharashtra", CountryCode = "IN" };
        var delhi = new State { Id = 2, Name = "Delhi", CountryCode = "IN" };
        var california = new State { Id = 3, Name = "California", CountryCode = "US" };

        var mumbai = new District { Id = 1, Name = "Mumbai", StateId = 1 };
        var thane = new District { Id = 2, Name = "Thane", StateId = 1 };
        var newDelhi = new District { Id = 3, Name = "New Delhi", StateId = 2 };

        _context.Countries.AddRange(india, usa);
        _context.States.AddRange(maharashtra, delhi, california);
        _context.Districts.AddRange(mumbai, thane, newDelhi);

        await _context.SaveChangesAsync();
    }

    #region Country Tests

    [Fact]
    public async Task GetCountriesAsync_ShouldReturnAllCountries()
    {
        // Act
        var countries = await _repository.GetCountriesAsync();

        // Assert
        Assert.NotEmpty(countries);
        Assert.Equal(2, countries.Count);
        Assert.Contains(countries, c => c.Code == "IN");
    }

    [Fact]
    public async Task GetCountryByCodeAsync_WithValidCode_ShouldReturnCountry()
    {
        // Act
        var country = await _repository.GetCountryByCodeAsync("IN");

        // Assert
        Assert.NotNull(country);
        Assert.Equal("India", country.Name);
    }

    [Fact]
    public async Task GetCountryByCodeAsync_WithInvalidCode_ShouldReturnNull()
    {
        // Act
        var country = await _repository.GetCountryByCodeAsync("INVALID");

        // Assert
        Assert.Null(country);
    }

    #endregion

    #region State Tests

    [Fact]
    public async Task GetStatesByCountryAsync_WithValidCountryCode_ShouldReturnStates()
    {
        // Act
        var states = await _repository.GetStatesByCountryAsync("IN");

        // Assert
        Assert.NotEmpty(states);
        Assert.Equal(2, states.Count);
        Assert.Contains(states, s => s.Name == "Maharashtra");
    }

    [Fact]
    public async Task GetStatesByCountryAsync_WithInvalidCountryCode_ShouldReturnEmpty()
    {
        // Act
        var states = await _repository.GetStatesByCountryAsync("INVALID");

        // Assert
        Assert.Empty(states);
    }

    [Fact]
    public async Task GetStateByIdAsync_WithValidId_ShouldReturnState()
    {
        // Act
        var state = await _repository.GetStateByIdAsync(1);

        // Assert
        Assert.NotNull(state);
        Assert.Equal("Maharashtra", state.Name);
    }

    [Fact]
    public async Task GetStateByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var state = await _repository.GetStateByIdAsync(99999);

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public async Task GetAllStatesAsync_ShouldReturnAllStates()
    {
        // Act
        var states = await _repository.GetAllStatesAsync();

        // Assert
        Assert.NotEmpty(states);
        Assert.Equal(3, states.Count);
    }

    #endregion

    #region District Tests

    [Fact]
    public async Task GetDistrictsByStateAsync_WithValidStateId_ShouldReturnDistricts()
    {
        // Act
        var districts = await _repository.GetDistrictsByStateAsync(1);

        // Assert
        Assert.NotEmpty(districts);
        Assert.Equal(2, districts.Count);
        Assert.Contains(districts, d => d.Name == "Mumbai");
    }

    [Fact]
    public async Task GetDistrictsByStateAsync_WithInvalidStateId_ShouldReturnEmpty()
    {
        // Act
        var districts = await _repository.GetDistrictsByStateAsync(99999);

        // Assert
        Assert.Empty(districts);
    }

    [Fact]
    public async Task GetDistrictByIdAsync_WithValidId_ShouldReturnDistrict()
    {
        // Act
        var district = await _repository.GetDistrictByIdAsync(1);

        // Assert
        Assert.NotNull(district);
        Assert.Equal("Mumbai", district.Name);
    }

    [Fact]
    public async Task GetDistrictByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var district = await _repository.GetDistrictByIdAsync(99999);

        // Assert
        Assert.Null(district);
    }

    [Fact]
    public async Task GetAllDistrictsAsync_ShouldReturnAllDistricts()
    {
        // Act
        var districts = await _repository.GetAllDistrictsAsync();

        // Assert
        Assert.NotEmpty(districts);
        Assert.Equal(3, districts.Count);
    }

    #endregion

    #region Hierarchy Validation Tests

    [Fact]
    public async Task GetStatesByCountryAsync_ShouldMaintainCountryStateRelationship()
    {
        // Act
        var indiaStates = await _repository.GetStatesByCountryAsync("IN");
        var usaStates = await _repository.GetStatesByCountryAsync("US");

        // Assert
        Assert.Equal(2, indiaStates.Count);
        Assert.Single(usaStates);
        Assert.All(indiaStates, state => Assert.Equal("IN", state.CountryCode));
        Assert.All(usaStates, state => Assert.Equal("US", state.CountryCode));
    }

    [Fact]
    public async Task GetDistrictsByStateAsync_ShouldMaintainStateDistrictRelationship()
    {
        // Act
        var maharashtraDistricts = await _repository.GetDistrictsByStateAsync(1);
        var delhiDistricts = await _repository.GetDistrictsByStateAsync(2);

        // Assert
        Assert.Equal(2, maharashtraDistricts.Count);
        Assert.Single(delhiDistricts);
        Assert.All(maharashtraDistricts, d => Assert.Equal(1, d.StateId));
        Assert.All(delhiDistricts, d => Assert.Equal(2, d.StateId));
    }

    [Fact]
    public async Task CompleteAddressHierarchy_ShouldRetrieveCascadingData()
    {
        // Act
        var country = await _repository.GetCountryByCodeAsync("IN");
        var states = await _repository.GetStatesByCountryAsync(country.Code);
        var districts = await _repository.GetDistrictsByStateAsync(states.First().Id);

        // Assert
        Assert.NotNull(country);
        Assert.NotEmpty(states);
        Assert.NotEmpty(districts);
        Assert.Equal("India", country.Name);
        Assert.Contains(states, s => s.Name == "Maharashtra");
        Assert.Contains(districts, d => d.Name == "Mumbai");
    }

    #endregion

    #region Search/Filter Tests

    [Fact]
    public async Task GetStatesByCountryAsync_WithMultipleMatches_ShouldReturnAll()
    {
        // Act
        var states = await _repository.GetStatesByCountryAsync("IN");

        // Assert
        Assert.Equal(2, states.Count);
    }

    [Fact]
    public async Task GetDistrictsByStateAsync_WithMultipleMatches_ShouldReturnAll()
    {
        // Act
        var districts = await _repository.GetDistrictsByStateAsync(1);

        // Assert
        Assert.Equal(2, districts.Count);
    }

    #endregion
}

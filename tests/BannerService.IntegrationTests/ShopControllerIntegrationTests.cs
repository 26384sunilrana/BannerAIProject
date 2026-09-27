namespace BannerService.IntegrationTests.Controllers;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;
using BannerService.Presentation.Controllers;

public class ShopControllerIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client;
    private string _authToken;

    public ShopControllerIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _authToken = _factory.GenerateTestJwt();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    #region GetMyShops Tests

    [Fact]
    public async Task GetMyShops_WithValidToken_ShouldReturnOwnedShops()
    {
        // Arrange
        var createDto = new
        {
            name = "My Shop",
            city = "Mumbai",
            address = "123 Main St",
            countryCode = "IN",
            stateId = 1,
            districtId = 1,
            postalCode = "400001"
        };

        var createContent = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act - Create a shop first
        var createResponse = await _client.PostAsync("/api/shops", createContent);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Act - Get my shops
        var response = await _client.GetAsync("/api/shops/my-shops");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("My Shop", content);
    }

    [Fact]
    public async Task GetMyShops_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var unauthorizedClient = _factory.CreateClient();

        // Act
        var response = await unauthorizedClient.GetAsync("/api/shops/my-shops");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyShops_WithStatusFilter_ShouldReturnFilteredShops()
    {
        // Arrange
        var createDto = new
        {
            name = "Active Shop",
            city = "Delhi",
            address = "456 Oak Ave",
            countryCode = "IN",
            stateId = 1,
            districtId = 1,
            postalCode = "110001"
        };

        var createContent = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act - Create shop
        await _client.PostAsync("/api/shops", createContent);

        // Act - Get with Active status filter
        var response = await _client.GetAsync("/api/shops/my-shops?status=Active");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Active Shop", content);
    }

    #endregion

    #region CreateShop Tests

    [Fact]
    public async Task CreateShop_WithValidData_ShouldReturnCreatedShop()
    {
        // Arrange
        var createDto = new
        {
            name = "New Shop",
            city = "Bangalore",
            address = "789 Pine Rd",
            countryCode = "IN",
            stateId = 1,
            districtId = 1,
            postalCode = "560001",
            phoneNumber = "+91-80-1234-5678",
            website = "https://newshop.com"
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/shops", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("New Shop", responseContent);
    }

    [Fact]
    public async Task CreateShop_WithInvalidAddressFK_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidDto = new
        {
            name = "Invalid Shop",
            city = "City",
            address = "Address",
            countryCode = "INVALID",  // Invalid country code
            stateId = 999,  // Non-existent state
            districtId = 999,  // Non-existent district
            postalCode = "123456"
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(invalidDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/shops", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateShop_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var unauthorizedClient = _factory.CreateClient();
        var createDto = new { name = "Shop", city = "City", address = "Address", countryCode = "IN", stateId = 1, districtId = 1, postalCode = "123456" };
        var content = new StringContent(JsonConvert.SerializeObject(createDto), Encoding.UTF8, "application/json");

        // Act
        var response = await unauthorizedClient.PostAsync("/api/shops", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GetShopById Tests

    [Fact]
    public async Task GetShopById_WithValidShopId_ShouldReturnShop()
    {
        // Arrange
        var createDto = new
        {
            name = "Test Shop",
            city = "Pune",
            address = "101 Elm St",
            countryCode = "IN",
            stateId = 1,
            districtId = 1,
            postalCode = "411001"
        };

        var createContent = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act - Create shop
        var createResponse = await _client.PostAsync("/api/shops", createContent);
        var shopContent = await createResponse.Content.ReadAsStringAsync();
        var shopData = JsonConvert.DeserializeObject<dynamic>(shopContent);
        var shopId = shopData?.id ?? Guid.NewGuid().ToString();

        // Act - Get shop
        var response = await _client.GetAsync($"/api/shops/{shopId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Shop", responseContent);
    }

    [Fact]
    public async Task GetShopById_WithInvalidShopId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/shops/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region UpdateShop Tests

    [Fact]
    public async Task UpdateShop_WithValidData_ShouldUpdateAndReturnShop()
    {
        // Arrange
        var createDto = new
        {
            name = "Original Shop",
            city = "Original City",
            address = "Original Address",
            countryCode = "IN",
            stateId = 1,
            districtId = 1,
            postalCode = "400001"
        };

        var createContent = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act - Create shop
        var createResponse = await _client.PostAsync("/api/shops", createContent);
        var shopContent = await createResponse.Content.ReadAsStringAsync();
        var shopData = JsonConvert.DeserializeObject<dynamic>(shopContent);
        var shopId = shopData?.id ?? Guid.NewGuid().ToString();

        // Act - Update shop
        var updateDto = new
        {
            name = "Updated Shop",
            city = "Updated City",
            address = "Updated Address",
            countryCode = "IN",
            stateId = 1,
            districtId = 1,
            postalCode = "400001"
        };

        var updateContent = new StringContent(
            JsonConvert.SerializeObject(updateDto),
            Encoding.UTF8,
            "application/json"
        );

        var updateResponse = await _client.PutAsync($"/api/shops/{shopId}", updateContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();
        Assert.Contains("Updated Shop", updateResponseContent);
    }

    #endregion

    #region DeleteShop Tests

    [Fact]
    public async Task DeleteShop_WithValidShopId_ShouldDeleteShop()
    {
        // Arrange
        var createDto = new
        {
            name = "Shop to Delete",
            city = "Delete City",
            address = "Delete Address",
            countryCode = "IN",
            stateId = 1,
            districtId = 1,
            postalCode = "123456"
        };

        var createContent = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act - Create shop
        var createResponse = await _client.PostAsync("/api/shops", createContent);
        var shopContent = await createResponse.Content.ReadAsStringAsync();
        var shopData = JsonConvert.DeserializeObject<dynamic>(shopContent);
        var shopId = shopData?.id ?? Guid.NewGuid().ToString();

        // Act - Delete shop
        var deleteResponse = await _client.DeleteAsync($"/api/shops/{shopId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Assert - Verify shop is deleted
        var getResponse = await _client.GetAsync($"/api/shops/{shopId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    #endregion

    #region ListShops Tests

    [Fact]
    public async Task ListShops_ShouldReturnAllShops()
    {
        // Act
        var response = await _client.GetAsync("/api/shops/list");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    #endregion
}

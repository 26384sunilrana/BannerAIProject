namespace BannerService.IntegrationTests.Controllers;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;

public class SubscriptionPlanControllerIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client;
    private string _authToken;

    public SubscriptionPlanControllerIntegrationTests()
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

    #region GetAllPlans Tests

    [Fact]
    public async Task GetAllPlans_ShouldReturnPlans()
    {
        // Act
        var response = await _client.GetAsync("/api/subscription-plans");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GetAllPlans_ShouldReturnJsonArray()
    {
        // Act
        var response = await _client.GetAsync("/api/subscription-plans");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var plans = JsonConvert.DeserializeObject<List<dynamic>>(content);
        Assert.NotNull(plans);
    }

    #endregion

    #region CreatePlan Tests

    [Fact]
    public async Task CreatePlan_WithValidData_ShouldReturnCreatedPlan()
    {
        // Arrange
        var createDto = new
        {
            name = "Test Plan",
            description = "Test Description",
            monthlyPrice = 9.99,
            annualPrice = 99.99,
            features = new[] { "Feature 1", "Feature 2" }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/subscription-plans", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Plan", responseContent);
    }

    [Fact]
    public async Task CreatePlan_WithInvalidPricing_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidDto = new
        {
            name = "Invalid Plan",
            description = "Invalid",
            monthlyPrice = 100,
            annualPrice = 50,  // Annual < Monthly is invalid
            features = new[] { "Feature 1" }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(invalidDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/subscription-plans", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlan_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var unauthorizedClient = _factory.CreateClient();
        var createDto = new
        {
            name = "Plan",
            monthlyPrice = 9.99,
            annualPrice = 99.99
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await unauthorizedClient.PostAsync("/api/subscription-plans", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GetPlanById Tests

    [Fact]
    public async Task GetPlanById_WithValidId_ShouldReturnPlan()
    {
        // Arrange
        var createDto = new
        {
            name = "Retrievable Plan",
            description = "Test",
            monthlyPrice = 19.99,
            annualPrice = 199.99,
            features = new[] { "Feature 1" }
        };

        var createContent = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act - Create plan
        var createResponse = await _client.PostAsync("/api/subscription-plans", createContent);
        var planContent = await createResponse.Content.ReadAsStringAsync();
        var planData = JsonConvert.DeserializeObject<dynamic>(planContent);
        var planId = planData?.id ?? Guid.NewGuid().ToString();

        // Act - Get plan
        var response = await _client.GetAsync($"/api/subscription-plans/{planId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Retrievable Plan", content);
    }

    [Fact]
    public async Task GetPlanById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/subscription-plans/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region UpdatePlan Tests

    [Fact]
    public async Task UpdatePlan_WithValidData_ShouldReturnUpdatedPlan()
    {
        // Arrange
        var createDto = new
        {
            name = "Original Plan",
            description = "Original",
            monthlyPrice = 9.99,
            annualPrice = 99.99,
            features = new[] { "Feature 1" }
        };

        var createContent = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act - Create plan
        var createResponse = await _client.PostAsync("/api/subscription-plans", createContent);
        var planContent = await createResponse.Content.ReadAsStringAsync();
        var planData = JsonConvert.DeserializeObject<dynamic>(planContent);
        var planId = planData?.id ?? Guid.NewGuid().ToString();

        // Act - Update plan
        var updateDto = new
        {
            name = "Updated Plan",
            description = "Updated",
            monthlyPrice = 19.99,
            annualPrice = 199.99,
            features = new[] { "Feature 1", "Feature 2" }
        };

        var updateContent = new StringContent(
            JsonConvert.SerializeObject(updateDto),
            Encoding.UTF8,
            "application/json"
        );

        var updateResponse = await _client.PutAsync($"/api/subscription-plans/{planId}", updateContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();
        Assert.Contains("Updated Plan", updateResponseContent);
    }

    #endregion

    #region DeletePlan Tests

    [Fact]
    public async Task DeletePlan_WithValidId_ShouldDeletePlan()
    {
        // Arrange
        var createDto = new
        {
            name = "Plan to Delete",
            description = "Delete",
            monthlyPrice = 4.99,
            annualPrice = 49.99,
            features = new[] { "Feature" }
        };

        var createContent = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act - Create plan
        var createResponse = await _client.PostAsync("/api/subscription-plans", createContent);
        var planContent = await createResponse.Content.ReadAsStringAsync();
        var planData = JsonConvert.DeserializeObject<dynamic>(planContent);
        var planId = planData?.id ?? Guid.NewGuid().ToString();

        // Act - Delete plan
        var deleteResponse = await _client.DeleteAsync($"/api/subscription-plans/{planId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Assert - Verify plan is deleted
        var getResponse = await _client.GetAsync($"/api/subscription-plans/{planId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    #endregion

    #region DeactivatePlan Tests

    [Fact]
    public async Task DeactivatePlan_WithActivePlan_ShouldReturnDeactivatedPlan()
    {
        // Arrange
        var createDto = new
        {
            name = "Plan to Deactivate",
            description = "Deactivate",
            monthlyPrice = 29.99,
            annualPrice = 299.99,
            features = new[] { "Feature" }
        };

        var createContent = new StringContent(
            JsonConvert.SerializeObject(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act - Create plan
        var createResponse = await _client.PostAsync("/api/subscription-plans", createContent);
        var planContent = await createResponse.Content.ReadAsStringAsync();
        var planData = JsonConvert.DeserializeObject<dynamic>(planContent);
        var planId = planData?.id ?? Guid.NewGuid().ToString();

        // Act - Deactivate plan
        var deactivateResponse = await _client.PatchAsync($"/api/subscription-plans/{planId}/deactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        var deactivateContent = await deactivateResponse.Content.ReadAsStringAsync();
        Assert.Contains("Inactive", deactivateContent);
    }

    #endregion

    #region PricingValidation Tests

    [Fact]
    public async Task CreatePlan_WithAnnualPriceEqualToMonthly_ShouldBeValid()
    {
        // Arrange
        var validDto = new
        {
            name = "Equal Price Plan",
            description = "Same price",
            monthlyPrice = 50,
            annualPrice = 50,  // Equal is valid
            features = new[] { "Feature" }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(validDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/subscription-plans", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlan_WithAnnualPriceGreaterThanMonthly_ShouldBeValid()
    {
        // Arrange
        var validDto = new
        {
            name = "Discount Plan",
            description = "Annual discount",
            monthlyPrice = 10,
            annualPrice = 100,  // Annual > Monthly is valid
            features = new[] { "Feature" }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(validDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/subscription-plans", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    #endregion
}

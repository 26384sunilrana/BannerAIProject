namespace BannerService.Application.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BannerService.Presentation.Controllers;
using BannerService.Application.DTOs;
using BannerService.Application.Services;

public class AuthenticationControllerTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<AuthenticationController>> _mockLogger;
    private readonly AuthenticationController _controller;

    public AuthenticationControllerTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<AuthenticationController>>();
        _controller = new AuthenticationController(_mockAuthService.Object, _mockLogger.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ShouldReturnOkWithTokens()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        var tokens = new { AccessToken = "jwt_token", RefreshToken = "refresh_token" };
        _mockAuthService.Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync((true, "Registration successful", tokens));

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockAuthService.Verify(s => s.RegisterAsync(It.IsAny<RegisterDto>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "SecurePassword123!",
            FirstName = "Jane",
            LastName = "Doe"
        };

        _mockAuthService.Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync((false, "Email already exists", null));

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "invalid-email",
            Password = "SecurePassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithTokens()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "user@example.com",
            Password = "SecurePassword123!"
        };

        var tokens = new { AccessToken = "jwt_token", RefreshToken = "refresh_token" };
        _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync((true, "Login successful", tokens));

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync((false, "Invalid credentials", null));

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithNonexistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "SecurePassword123!"
        };

        _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync((false, "User not found", null));

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnOkWithNewTokens()
    {
        // Arrange
        var refreshDto = new RefreshTokenDto { RefreshToken = "valid_refresh_token" };
        var tokens = new { AccessToken = "new_jwt_token", RefreshToken = "new_refresh_token" };

        _mockAuthService.Setup(s => s.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((true, "Token refreshed", tokens));

        // Act
        var result = await _controller.RefreshToken(refreshDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var refreshDto = new RefreshTokenDto { RefreshToken = "expired_refresh_token" };

        _mockAuthService.Setup(s => s.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, "Token expired", null));

        // Act
        var result = await _controller.RefreshToken(refreshDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_WithValidToken_ShouldReturnOk()
    {
        // Arrange
        var logoutDto = new RefreshTokenDto { RefreshToken = "valid_refresh_token" };

        _mockAuthService.Setup(s => s.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Logout(logoutDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Logout_WithInvalidToken_ShouldReturnBadRequest()
    {
        // Arrange
        var logoutDto = new RefreshTokenDto { RefreshToken = "invalid_refresh_token" };

        _mockAuthService.Setup(s => s.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Logout(logoutDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}

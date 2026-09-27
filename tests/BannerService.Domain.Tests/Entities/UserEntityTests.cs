namespace BannerService.Domain.Tests.Entities;

using System;
using Xunit;
using BannerService.Domain.Entities;

public class UserEntityTests
{
    #region Constructor Tests

    [Fact]
    public void CreateUser_WithDefaultValues_ShouldInitializeProperties()
    {
        // Act
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            ShopId = Guid.NewGuid().ToString(),
            PasswordHash = "hash123"
        };

        // Assert
        Assert.NotEmpty(user.Id);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.True(user.IsActive);
        Assert.False(user.EmailVerified);
        Assert.Equal(0, user.LoginAttempts);
        Assert.False(user.IsLockedOut);
    }

    #endregion

    #region GetFullName Tests

    [Fact]
    public void GetFullName_WithFirstAndLastName_ShouldReturnCombined()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = user.GetFullName();

        // Assert
        Assert.Equal("John Doe", fullName);
    }

    [Fact]
    public void GetFullName_WithOnlyFirstName_ShouldReturnFirstName()
    {
        // Arrange
        var user = new User { FirstName = "John", LastName = "" };

        // Act
        var fullName = user.GetFullName();

        // Assert
        Assert.Equal("John", fullName);
    }

    #endregion

    #region LockOut Tests

    [Fact]
    public void LockOut_ShouldSetIsLockedOutTrue()
    {
        // Arrange
        var user = new User { IsLockedOut = false };
        var originalTime = user.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(10);
        user.LockOut();

        // Assert
        Assert.True(user.IsLockedOut);
        Assert.True(user.UpdatedAt > originalTime);
    }

    #endregion

    #region UnlockAccount Tests

    [Fact]
    public void UnlockAccount_ShouldResetLoginAttemptsAndUnlock()
    {
        // Arrange
        var user = new User
        {
            IsLockedOut = true,
            LoginAttempts = 5
        };

        // Act
        user.UnlockAccount();

        // Assert
        Assert.False(user.IsLockedOut);
        Assert.Equal(0, user.LoginAttempts);
    }

    #endregion

    #region RecordLoginAttempt Tests

    [Fact]
    public void RecordLoginAttempt_ShouldIncrementAttempts()
    {
        // Arrange
        var user = new User { LoginAttempts = 0 };

        // Act
        user.RecordLoginAttempt();

        // Assert
        Assert.Equal(1, user.LoginAttempts);
        Assert.NotNull(user.LastLoginAttempt);
    }

    [Fact]
    public void RecordLoginAttempt_AfterFiveAttempts_ShouldLockAccount()
    {
        // Arrange
        var user = new User { LoginAttempts = 4 };

        // Act
        user.RecordLoginAttempt();

        // Assert
        Assert.Equal(5, user.LoginAttempts);
        Assert.True(user.IsLockedOut);
    }

    [Fact]
    public void RecordLoginAttempt_ShouldUpdateModificationTime()
    {
        // Arrange
        var user = new User();
        var originalTime = user.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(10);
        user.RecordLoginAttempt();

        // Assert
        Assert.True(user.UpdatedAt > originalTime);
    }

    #endregion

    #region ResetLoginAttempts Tests

    [Fact]
    public void ResetLoginAttempts_ShouldClearAttemptsAndUnlock()
    {
        // Arrange
        var user = new User
        {
            LoginAttempts = 5,
            IsLockedOut = true
        };

        // Act
        user.ResetLoginAttempts();

        // Assert
        Assert.Equal(0, user.LoginAttempts);
        Assert.False(user.IsLockedOut);
    }

    #endregion

    #region VerifyEmail Tests

    [Fact]
    public void VerifyEmail_ShouldSetEmailVerifiedAndClearToken()
    {
        // Arrange
        var user = new User
        {
            EmailVerified = false,
            VerificationToken = "token123",
            VerificationTokenExpires = DateTime.UtcNow.AddHours(1)
        };

        // Act
        user.VerifyEmail();

        // Assert
        Assert.True(user.EmailVerified);
        Assert.Null(user.VerificationToken);
        Assert.Null(user.VerificationTokenExpires);
    }

    #endregion

    #region SetPasswordResetToken Tests

    [Fact]
    public void SetPasswordResetToken_ShouldSetTokenAndExpiry()
    {
        // Arrange
        var user = new User();
        var token = "reset-token-123";

        // Act
        user.SetPasswordResetToken(token);

        // Assert
        Assert.Equal(token, user.PasswordResetToken);
        Assert.NotNull(user.PasswordResetExpires);
        Assert.True(user.PasswordResetExpires > DateTime.UtcNow);
    }

    [Fact]
    public void SetPasswordResetToken_ShouldSetExpiryToOneHour()
    {
        // Arrange
        var user = new User();
        var beforeTime = DateTime.UtcNow;

        // Act
        user.SetPasswordResetToken("token");

        // Assert
        var afterTime = DateTime.UtcNow;
        var expectedExpiry = afterTime.AddHours(1);
        Assert.True(user.PasswordResetExpires < expectedExpiry.AddSeconds(1));
    }

    #endregion

    #region IsPasswordResetTokenValid Tests

    [Fact]
    public void IsPasswordResetTokenValid_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = new User
        {
            PasswordResetToken = "valid-token",
            PasswordResetExpires = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var result = user.IsPasswordResetTokenValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPasswordResetTokenValid_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var user = new User
        {
            PasswordResetToken = "expired-token",
            PasswordResetExpires = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var result = user.IsPasswordResetTokenValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPasswordResetTokenValid_WithNullToken_ShouldReturnFalse()
    {
        // Arrange
        var user = new User { PasswordResetToken = null };

        // Act
        var result = user.IsPasswordResetTokenValid();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ClearPasswordResetToken Tests

    [Fact]
    public void ClearPasswordResetToken_ShouldRemoveTokenAndExpiry()
    {
        // Arrange
        var user = new User
        {
            PasswordResetToken = "token",
            PasswordResetExpires = DateTime.UtcNow.AddHours(1)
        };

        // Act
        user.ClearPasswordResetToken();

        // Assert
        Assert.Null(user.PasswordResetToken);
        Assert.Null(user.PasswordResetExpires);
    }

    #endregion

    #region HasRole Tests

    [Fact]
    public void HasRole_WithRoleInCollection_ShouldReturnTrue()
    {
        // Arrange
        var role = new Role { Name = "Admin" };
        var userRole = new UserRole { Role = role };
        var user = new User
        {
            UserRoles = new List<UserRole> { userRole }
        };

        // Act
        var hasRole = user.HasRole("Admin");

        // Assert
        Assert.True(hasRole);
    }

    [Fact]
    public void HasRole_WithoutRole_ShouldReturnFalse()
    {
        // Arrange
        var user = new User { UserRoles = new List<UserRole>() };

        // Act
        var hasRole = user.HasRole("Admin");

        // Assert
        Assert.False(hasRole);
    }

    #endregion
}

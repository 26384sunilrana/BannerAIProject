namespace BannerService.Application.Services;

using System.Text.RegularExpressions;
using DTOs;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;

public class AccountDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ShopId { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>A person looking after their own login: details, password, and ending their sessions.</summary>
public class AccountService
{
    public const int MinPasswordLength = 8;
    private static readonly Regex PhonePattern = new(@"^\+?[0-9][0-9 \-()]{5,19}$", RegexOptions.Compiled);

    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHashService _passwords;
    private readonly IJwtTokenService _jwt;

    public AccountService(
        IUserRepository users,
        IRoleRepository roles,
        IRefreshTokenRepository refreshTokens,
        IPasswordHashService passwords,
        IJwtTokenService jwt)
    {
        _users = users;
        _roles = roles;
        _refreshTokens = refreshTokens;
        _passwords = passwords;
        _jwt = jwt;
    }

    public async Task<AccountDto> GetAsync(string userId) => await ToDtoAsync(await LoadAsync(userId));

    public async Task<AccountDto> UpdateAsync(string userId, string firstName, string lastName, string? phoneNumber)
    {
        firstName = (firstName ?? string.Empty).Trim();
        lastName = (lastName ?? string.Empty).Trim();
        phoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        if (firstName.Length is < 1 or > 100)
            throw new ArgumentException("Enter your first name (up to 100 characters).");
        if (lastName.Length > 100)
            throw new ArgumentException("The last name can have up to 100 characters.");
        if (phoneNumber != null && !PhonePattern.IsMatch(phoneNumber))
            throw new ArgumentException("The phone number can have digits, spaces, dashes and brackets, and may start with +.");
        if (firstName.Any(char.IsControl) || lastName.Any(char.IsControl))
            throw new ArgumentException("The name has characters that are not allowed.");

        var user = await LoadAsync(userId);
        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        return await ToDtoAsync(user);
    }

    /// <summary>
    /// Changes the password after checking the current one (a wrong one counts as a failed sign-in, so a stolen session cannot guess it).
    /// Every other session is ended; the caller gets a fresh one.
    /// </summary>
    public async Task<AuthTokenDto> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword, string confirmPassword, string? ipAddress, string? userAgent)
    {
        var user = await LoadAsync(userId);
        if (user.IsLockedOut)
            throw new InvalidOperationException("This account is locked. Ask the administrator to unlock it.");

        if (string.IsNullOrEmpty(currentPassword) || !_passwords.VerifyPassword(currentPassword, user.PasswordHash))
        {
            user.RecordLoginAttempt();
            await _users.UpdateAsync(user);
            throw new ArgumentException("Your current password is not right.");
        }

        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < MinPasswordLength)
            throw new ArgumentException($"The new password needs at least {MinPasswordLength} characters.");
        if (newPassword != confirmPassword)
            throw new ArgumentException("The two new passwords are not the same.");
        if (newPassword == currentPassword)
            throw new ArgumentException("Choose a password you have not been using.");

        user.PasswordHash = _passwords.HashPassword(newPassword);
        user.ResetLoginAttempts();
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        await RevokeAllAsync(user.Id);
        return await StartSessionAsync(user, ipAddress, userAgent);
    }

    /// <summary>Ends every session of this person, on every device. Access tokens already issued run out within 15 minutes.</summary>
    public async Task<int> SignOutEverywhereAsync(string userId)
    {
        await LoadAsync(userId);
        return await RevokeAllAsync(userId);
    }

    private async Task<AuthTokenDto> StartSessionAsync(User user, string? ipAddress, string? userAgent)
    {
        var withRoles = await _users.GetByIdAsync(user.Id) ?? user;
        var (accessToken, jwtId) = _jwt.GenerateAccessToken(withRoles);
        var refreshToken = _jwt.GenerateRefreshToken();
        var expires = DateTime.UtcNow.Add(AuthenticationService.RefreshLifetime);

        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            JwtId = jwtId,
            ExpiresAt = expires,
            IpAddress = ipAddress ?? string.Empty,
            UserAgent = userAgent ?? string.Empty,
        });

        return new AuthTokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900,
            RefreshTokenExpiresAt = expires,
            TokenType = "Bearer",
        };
    }

    private async Task<int> RevokeAllAsync(string userId)
    {
        var count = 0;
        foreach (var token in await _refreshTokens.GetActiveByUserIdAsync(userId))
        {
            token.Revoke();
            await _refreshTokens.UpdateAsync(token);
            count++;
        }
        return count;
    }

    private async Task<User> LoadAsync(string userId) =>
        await _users.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");

    private async Task<AccountDto> ToDtoAsync(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        ShopId = string.IsNullOrEmpty(user.ShopId) ? null : user.ShopId,
        Roles = (await _roles.GetByUserIdAsync(user.Id)).Select(r => r.Name).ToList(),
        EmailVerified = user.EmailVerified,
        CreatedAt = user.CreatedAt,
    };
}

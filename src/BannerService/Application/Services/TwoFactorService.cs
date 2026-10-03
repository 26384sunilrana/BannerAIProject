namespace BannerService.Application.Services;

using System.Text.Json;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;

public class TwoFactorStatusDto
{
    public bool Enabled { get; set; }
    public int RecoveryCodesLeft { get; set; }

    /// <summary>Administrators must use it when the system is set to require it; they cannot switch it off.</summary>
    public bool Required { get; set; }
}

public class TwoFactorSetupDto
{
    /// <summary>The secret, in groups of four, to type into an authenticator app that cannot read the picture.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>The otpauth:// address an authenticator app reads from the QR picture.</summary>
    public string Uri { get; set; } = string.Empty;
}

public class RecoveryCodesDto
{
    /// <summary>Shown once. Each works one time.</summary>
    public List<string> Codes { get; set; } = new();
}

/// <summary>Setting up, switching off and renewing the recovery codes of two-step sign-in (an authenticator app) for the signed-in person.</summary>
public class TwoFactorService
{
    private const string Issuer = "BannerAI";

    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHashService _passwords;
    private readonly IConfiguration _configuration;

    public TwoFactorService(IUserRepository users, IRoleRepository roles, IPasswordHashService passwords, IConfiguration configuration)
    {
        _users = users;
        _roles = roles;
        _passwords = passwords;
        _configuration = configuration;
    }

    public async Task<TwoFactorStatusDto> StatusAsync(string userId)
    {
        var user = await LoadAsync(userId);
        return new TwoFactorStatusDto
        {
            Enabled = user.TwoFactorEnabled,
            RecoveryCodesLeft = RecoveryCount(user),
            Required = await IsRequiredAsync(user),
        };
    }

    /// <summary>Makes a new secret (nothing changes for sign-in until the first code is confirmed).</summary>
    public async Task<TwoFactorSetupDto> BeginSetupAsync(string userId, string? password)
    {
        var user = await LoadAsync(userId);
        if (user.TwoFactorEnabled) throw new InvalidOperationException("Two-step sign-in is already on. Switch it off first to set it up again.");
        await CheckPasswordAsync(user, password);

        var secret = Totp.GenerateSecret();
        user.TwoFactorSecret = secret;
        user.TwoFactorLastStep = 0;
        await _users.UpdateAsync(user);

        var grouped = string.Join(' ', Enumerable.Range(0, (secret.Length + 3) / 4).Select(i => secret.Substring(i * 4, Math.Min(4, secret.Length - i * 4))));
        return new TwoFactorSetupDto { Secret = grouped, Uri = Totp.Uri(Issuer, user.Email, secret) };
    }

    /// <summary>The first code from the app proves it was set up properly; then two-step sign-in is on and recovery codes are made.</summary>
    public async Task<RecoveryCodesDto> EnableAsync(string userId, string? code)
    {
        var user = await LoadAsync(userId);
        if (user.TwoFactorEnabled) throw new InvalidOperationException("Two-step sign-in is already on.");
        if (string.IsNullOrEmpty(user.TwoFactorSecret)) throw new InvalidOperationException("Start the set-up first.");
        if (!Totp.Verify(user.TwoFactorSecret, code, DateTime.UtcNow, user.TwoFactorLastStep, out var step))
            throw new ArgumentException("That code is not right. Check that the clock of the phone is correct and try the next code.");

        user.TwoFactorEnabled = true;
        user.TwoFactorLastStep = step;
        var codes = Totp.GenerateRecoveryCodes();
        user.TwoFactorRecoveryHashes = JsonSerializer.Serialize(codes.Select(Totp.HashRecoveryCode).ToList());
        await _users.UpdateAsync(user);
        return new RecoveryCodesDto { Codes = codes };
    }

    public async Task DisableAsync(string userId, string? password, string? code)
    {
        var user = await LoadAsync(userId);
        if (!user.TwoFactorEnabled) throw new InvalidOperationException("Two-step sign-in is not on.");
        if (await IsRequiredAsync(user)) throw new InvalidOperationException("Administrators must use two-step sign-in. It cannot be switched off.");
        await CheckPasswordAsync(user, password);
        CheckCode(user, code);

        Clear(user);
        await _users.UpdateAsync(user);
    }

    public async Task<RecoveryCodesDto> NewRecoveryCodesAsync(string userId, string? password, string? code)
    {
        var user = await LoadAsync(userId);
        if (!user.TwoFactorEnabled) throw new InvalidOperationException("Two-step sign-in is not on.");
        await CheckPasswordAsync(user, password);
        CheckCode(user, code);

        var codes = Totp.GenerateRecoveryCodes();
        user.TwoFactorRecoveryHashes = JsonSerializer.Serialize(codes.Select(Totp.HashRecoveryCode).ToList());
        await _users.UpdateAsync(user);
        return new RecoveryCodesDto { Codes = codes };
    }

    /// <summary>An administrator switches two-step sign-in off for someone who lost their phone and their recovery codes.</summary>
    public async Task ResetForUserAsync(string targetUserId)
    {
        var user = await LoadAsync(targetUserId);
        Clear(user);
        await _users.UpdateAsync(user);
    }

    // ----- helpers

    private static void Clear(User user)
    {
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.TwoFactorRecoveryHashes = null;
        user.TwoFactorLastStep = 0;
    }

    private async Task<User> LoadAsync(string userId) =>
        await _users.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found.");

    private async Task<bool> IsRequiredAsync(User user) =>
        _configuration.GetValue("Security:RequireTwoFactorForAdmins", false) && (await _roles.GetByUserIdAsync(user.Id)).Any(r => r.Name == Role.Admin);

    private async Task CheckPasswordAsync(User user, string? password)
    {
        if (user.IsLockedOut) throw new InvalidOperationException("This account is locked. Ask the administrator to unlock it.");
        if (string.IsNullOrEmpty(password) || !_passwords.VerifyPassword(password, user.PasswordHash))
        {
            user.RecordLoginAttempt();
            await _users.UpdateAsync(user);
            throw new ArgumentException("Your password is not right.");
        }
    }

    private static void CheckCode(User user, string? code)
    {
        // these actions also need the password, so a code that was just used to sign in may be used again here
        if (string.IsNullOrEmpty(user.TwoFactorSecret) || !Totp.Verify(user.TwoFactorSecret, code, DateTime.UtcNow, 0, out _))
            throw new ArgumentException("That code is not right.");
    }

    private static int RecoveryCount(User user) =>
        string.IsNullOrEmpty(user.TwoFactorRecoveryHashes) ? 0 : JsonSerializer.Deserialize<List<string>>(user.TwoFactorRecoveryHashes)?.Count ?? 0;
}

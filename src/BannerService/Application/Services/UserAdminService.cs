namespace BannerService.Application.Services;

using Domain.Entities;
using Domain.Interfaces;

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ShopId { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAttempt { get; set; }
}

public class AdminUserPageDto
{
    public List<AdminUserDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>User administration for the product owner: search, deactivate, reactivate, unlock.</summary>
public class UserAdminService
{
    public const int MaxPageSize = 100;

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;

    public UserAdminService(IUserRepository users, IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _refreshTokens = refreshTokens;
    }

    public async Task<AdminUserPageDto> ListAsync(string? search, string? shopId, bool includeInactive, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, total) = await _users.GetPagedAsync(search, shopId, includeInactive, page, pageSize);
        return new AdminUserPageDto { Items = items.Select(ToDto).ToList(), Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<AdminUserDto> GetAsync(string userId) => ToDto(await Load(userId));

    public async Task<AdminUserDto> DeactivateAsync(string userId, string actingUserId)
    {
        if (string.Equals(userId, actingUserId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("You cannot deactivate your own account");

        var user = await Load(userId);
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        // End existing sessions: refresh tokens stop working immediately
        foreach (var token in await _refreshTokens.GetActiveByUserIdAsync(userId))
        {
            token.Revoke();
            await _refreshTokens.UpdateAsync(token);
        }

        return ToDto(user);
    }

    public async Task<AdminUserDto> ActivateAsync(string userId)
    {
        var user = await Load(userId);
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        return ToDto(user);
    }

    public async Task<AdminUserDto> UnlockAsync(string userId)
    {
        var user = await Load(userId);
        user.UnlockAccount();
        await _users.UpdateAsync(user);
        return ToDto(user);
    }

    private async Task<User> Load(string userId) =>
        await _users.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");

    private static AdminUserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.GetFullName(),
        ShopId = string.IsNullOrEmpty(user.ShopId) ? null : user.ShopId,
        Roles = user.UserRoles.Select(r => r.Role?.Name).Where(n => n != null).Select(n => n!).ToList(),
        IsActive = user.IsActive,
        IsLockedOut = user.IsLockedOut,
        EmailVerified = user.EmailVerified,
        CreatedAt = user.CreatedAt,
        LastLoginAttempt = user.LastLoginAttempt
    };
}

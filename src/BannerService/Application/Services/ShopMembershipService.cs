namespace BannerService.Application.Services;

using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;

public class OwnershipChangeDto
{
    public Guid ShopId { get; set; }
    public string NewOwnerUserId { get; set; } = string.Empty;
    public string NewOwnerEmail { get; set; } = string.Empty;
    /// <summary>The previous owner, who stays in the shop as a sales executive. Null when the shop had no owner.</summary>
    public string? PreviousOwnerUserId { get; set; }
}

public class UserMoveDto
{
    public string UserId { get; set; } = string.Empty;
    public Guid FromShopId { get; set; }
    public Guid ToShopId { get; set; }
}

/// <summary>
/// Who belongs to which shop and who owns it: handing a shop to another member, and moving a sales executive to another shop.
/// Both end the affected people's sessions, so their next sign-in carries the new role and shop.
/// </summary>
public class ShopMembershipService
{
    private const string OwnerRoleId = "2";
    private const string ExecutiveRoleId = "3";
    private const string OwnerRole = "ShopOwner";
    private const string ExecutiveRole = "SalesExecutive";

    private static readonly SubscriptionStatus[] SubscribedStatuses =
    {
        SubscriptionStatus.Trial, SubscriptionStatus.Active, SubscriptionStatus.RenewalPending, SubscriptionStatus.GracePeriod
    };

    private readonly IShopRepository _shops;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPasswordHashService _passwords;

    public ShopMembershipService(
        IShopRepository shops,
        IUserRepository users,
        IRoleRepository roles,
        IRefreshTokenRepository refreshTokens,
        ISubscriptionRepository subscriptions,
        IPasswordHashService passwords)
    {
        _shops = shops;
        _users = users;
        _roles = roles;
        _refreshTokens = refreshTokens;
        _subscriptions = subscriptions;
        _passwords = passwords;
    }

    /// <summary>The owner hands the shop to one of the shop's sales executives. The owner's password is asked for again.</summary>
    public async Task<OwnershipChangeDto> TransferOwnershipAsync(Guid shopId, string callerId, string newOwnerUserId, string password)
    {
        var shop = await LoadShopAsync(shopId);
        if (!shop.OwnerUserId.HasValue || !string.Equals(shop.OwnerUserId.Value.ToString(), callerId, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Only the shop owner can hand the shop over");

        var caller = await _users.GetByIdAsync(callerId) ?? throw new KeyNotFoundException("User not found");
        if (caller.IsLockedOut)
            throw new InvalidOperationException("This account is locked. Ask the administrator to unlock it.");

        // a stolen sign-in must not be enough to take the shop away: the password counts as a sign-in attempt
        if (string.IsNullOrEmpty(password) || !_passwords.VerifyPassword(password, caller.PasswordHash))
        {
            caller.RecordLoginAttempt();
            await _users.UpdateAsync(caller);
            throw new ArgumentException("Your password is not right.");
        }

        caller.ResetLoginAttempts();
        await _users.UpdateAsync(caller);

        return await SwapOwnerAsync(shop, newOwnerUserId);
    }

    /// <summary>An administrator makes a member of the shop its owner (for a shop whose owner is gone, or on the owner's request).</summary>
    public async Task<OwnershipChangeDto> AssignOwnerAsAdminAsync(Guid shopId, string newOwnerUserId)
    {
        var shop = await LoadShopAsync(shopId);
        return await SwapOwnerAsync(shop, newOwnerUserId);
    }

    /// <summary>An administrator moves a sales executive to another shop (not the owner of a shop: hand that over first).</summary>
    public async Task<UserMoveDto> MoveExecutiveAsync(string userId, Guid toShopId)
    {
        var user = await _users.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");
        if (!user.IsActive)
            throw new InvalidOperationException("This login is switched off. Reactivate it before moving it.");
        if (!Guid.TryParse(user.ShopId, out var fromShopId))
            throw new InvalidOperationException("This user does not belong to a shop");
        if (fromShopId == toShopId)
            throw new InvalidOperationException("The user is already in that shop");

        var roles = await RoleNamesAsync(user);
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Administrators do not belong to a shop");
        if (roles.Contains(OwnerRole, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("A shop owner cannot be moved. Hand the shop over to someone else first.");
        if (!roles.Contains(ExecutiveRole, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only sales executives can be moved");

        var target = await _shops.GetByIdAsync(toShopId) ?? throw new KeyNotFoundException("The shop to move to was not found");
        if (target.Status != ShopStatus.Active)
            throw new InvalidOperationException("The shop to move to is not active");

        var subscription = await _subscriptions.GetByShopIdAsync(toShopId);
        if (subscription == null || !SubscribedStatuses.Contains(subscription.Status))
            throw new InvalidOperationException("The shop to move to has no active subscription, so it cannot have more logins");

        var executives = await ActiveExecutivesAsync(target);
        if (executives.Count >= Shop.MaxSalesExecutives)
            throw new InvalidOperationException($"The shop to move to already has {Shop.MaxSalesExecutives} sales executives");

        var source = await _shops.GetByIdAsync(fromShopId);
        if (source != null)
        {
            source.RemoveApprover(user.Id);
            await _shops.UpdateAsync(source);
        }

        user.ShopId = toShopId.ToString();
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        await RevokeSessionsAsync(user.Id);

        return new UserMoveDto { UserId = user.Id, FromShopId = fromShopId, ToShopId = toShopId };
    }

    // ----- shared

    private async Task<OwnershipChangeDto> SwapOwnerAsync(Shop shop, string newOwnerUserId)
    {
        var newOwner = await _users.GetByIdAsync(newOwnerUserId) ?? throw new KeyNotFoundException("User not found");
        if (!string.Equals(newOwner.ShopId, shop.Id.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The new owner must already be a login of this shop");
        if (!newOwner.IsActive)
            throw new InvalidOperationException("The new owner's login is switched off");
        if (shop.OwnerUserId.HasValue && string.Equals(shop.OwnerUserId.Value.ToString(), newOwner.Id, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("That person already owns the shop");

        var newOwnerRoles = await RoleNamesAsync(newOwner);
        if (!newOwnerRoles.Contains(ExecutiveRole, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only a sales executive of the shop can become its owner");

        string? previousId = null;
        if (shop.OwnerUserId.HasValue)
        {
            previousId = shop.OwnerUserId.Value.ToString();
            var previous = await _users.GetByIdAsync(previousId);
            if (previous != null)
            {
                // the previous owner takes the place the new owner leaves
                await _roles.RemoveRoleAsync(previous.Id, OwnerRoleId);
                await _roles.AssignRoleAsync(previous.Id, ExecutiveRoleId);
            }
        }

        await _roles.RemoveRoleAsync(newOwner.Id, ExecutiveRoleId);
        await _roles.AssignRoleAsync(newOwner.Id, OwnerRoleId);

        shop.AssignOwner(Guid.Parse(newOwner.Id));
        shop.RemoveApprover(newOwner.Id); // the owner approves through OwnerIsApprover, not as an executive
        await _shops.UpdateAsync(shop);

        await RevokeSessionsAsync(newOwner.Id);
        if (previousId != null) await RevokeSessionsAsync(previousId);

        return new OwnershipChangeDto
        {
            ShopId = shop.Id,
            NewOwnerUserId = newOwner.Id,
            NewOwnerEmail = newOwner.Email,
            PreviousOwnerUserId = previousId,
        };
    }

    private async Task<Shop> LoadShopAsync(Guid shopId) =>
        await _shops.GetByIdAsync(shopId) ?? throw new KeyNotFoundException("Shop not found");

    private async Task<List<string>> RoleNamesAsync(User user) =>
        (await _roles.GetByUserIdAsync(user.Id)).Select(r => r.Name).ToList();

    private async Task<List<User>> ActiveExecutivesAsync(Shop shop)
    {
        var members = await _users.GetByShopIdAsync(shop.Id.ToString());
        return members
            .Where(u => u.IsActive && !(shop.OwnerUserId.HasValue &&
                string.Equals(u.Id, shop.OwnerUserId.Value.ToString(), StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private async Task RevokeSessionsAsync(string userId)
    {
        foreach (var token in await _refreshTokens.GetActiveByUserIdAsync(userId))
        {
            token.Revoke();
            await _refreshTokens.UpdateAsync(token);
        }
    }
}

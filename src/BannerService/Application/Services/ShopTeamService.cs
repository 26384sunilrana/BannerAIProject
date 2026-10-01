namespace BannerService.Application.Services;

using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;
using DTOs;

/// <summary>
/// Lets a shop owner manage the (at most two) sales executive logins under the shop
/// and choose who approves banners.
/// </summary>
public class ShopTeamService
{
    private static readonly SubscriptionStatus[] SubscribedStatuses =
    {
        SubscriptionStatus.Trial, SubscriptionStatus.Active, SubscriptionStatus.RenewalPending, SubscriptionStatus.GracePeriod
    };

    private readonly IShopRepository _shopRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHashService _passwordHashService;

    public ShopTeamService(
        IShopRepository shopRepository,
        IUserRepository userRepository,
        ISubscriptionRepository subscriptionRepository,
        IRoleRepository roleRepository,
        IPasswordHashService passwordHashService)
    {
        _shopRepository = shopRepository;
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
        _roleRepository = roleRepository;
        _passwordHashService = passwordHashService;
    }

    public async Task<ShopTeamDto> GetTeamAsync(Guid shopId, Guid callerId)
    {
        var shop = await GetOwnedShopAsync(shopId, callerId);
        var executives = await GetActiveExecutivesAsync(shop);

        return new ShopTeamDto
        {
            ShopId = shop.Id,
            OwnerUserId = shop.OwnerUserId,
            OwnerIsApprover = shop.OwnerIsApprover,
            MaxSalesExecutives = Shop.MaxSalesExecutives,
            SalesExecutives = executives.Select(u => ToMember(u, shop)).ToList()
        };
    }

    /// <summary>What the caller may do in this shop's approval flow (any member of the shop can ask).</summary>
    public async Task<MyApprovalRoleDto> GetMyApprovalRoleAsync(Guid shopId, Guid callerId)
    {
        var shop = await _shopRepository.GetByIdAsync(shopId)
            ?? throw new KeyNotFoundException("Shop not found");

        return new MyApprovalRoleDto
        {
            ShopId = shop.Id,
            IsOwner = shop.OwnerUserId == callerId,
            CanApprove = shop.CanApprove(callerId)
        };
    }

    public async Task<TeamMemberDto> AddSalesExecutiveAsync(Guid shopId, Guid callerId, AddSalesExecutiveDto request)
    {
        var shop = await GetOwnedShopAsync(shopId, callerId);

        var subscription = await _subscriptionRepository.GetByShopIdAsync(shopId);
        if (subscription == null || !SubscribedStatuses.Contains(subscription.Status))
            throw new InvalidOperationException("An active subscription is required to add logins");

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Email and password are required");
        if (request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters");

        var executives = await GetActiveExecutivesAsync(shop);
        if (executives.Count >= Shop.MaxSalesExecutives)
            throw new InvalidOperationException(
                $"A shop can have at most {Shop.MaxSalesExecutives} sales executive logins. Remove one before adding another.");

        if (await _userRepository.ExistsAsync(request.Email))
            throw new InvalidOperationException("Email already registered");

        var user = await _userRepository.CreateAsync(new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHashService.HashPassword(request.Password),
            FirstName = request.FirstName ?? string.Empty,
            LastName = request.LastName ?? string.Empty,
            ShopId = shop.Id.ToString(),
            IsActive = true
        });

        await _roleRepository.AssignRoleAsync(user.Id, "3");
        return ToMember(user, shop);
    }

    public async Task RemoveSalesExecutiveAsync(Guid shopId, Guid callerId, string userId)
    {
        var shop = await GetOwnedShopAsync(shopId, callerId);
        var executives = await GetActiveExecutivesAsync(shop);

        var target = executives.FirstOrDefault(u => string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException("Sales executive not found in this shop");

        target.IsActive = false;
        target.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(target);

        shop.RemoveApprover(target.Id);
        await _shopRepository.UpdateAsync(shop);
    }

    public async Task<ShopTeamDto> SetApproversAsync(Guid shopId, Guid callerId, SetApproversDto request)
    {
        var shop = await GetOwnedShopAsync(shopId, callerId);
        var executives = await GetActiveExecutivesAsync(shop);

        var approverIds = request.ApproverUserIds ?? new List<string>();
        foreach (var id in approverIds)
        {
            if (!executives.Any(u => string.Equals(u.Id, id, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"User {id} is not an active sales executive of this shop");
        }

        shop.SetApprovers(request.OwnerIsApprover, approverIds);
        await _shopRepository.UpdateAsync(shop);

        return await GetTeamAsync(shopId, callerId);
    }

    private async Task<Shop> GetOwnedShopAsync(Guid shopId, Guid callerId)
    {
        var shop = await _shopRepository.GetByIdAsync(shopId)
            ?? throw new KeyNotFoundException("Shop not found");

        if (shop.OwnerUserId != callerId)
            throw new UnauthorizedAccessException("Only the shop owner can manage logins");

        return shop;
    }

    private async Task<List<User>> GetActiveExecutivesAsync(Shop shop)
    {
        var users = await _userRepository.GetByShopIdAsync(shop.Id.ToString());
        return users
            .Where(u => u.IsActive && !(shop.OwnerUserId.HasValue &&
                string.Equals(u.Id, shop.OwnerUserId.Value.ToString(), StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static TeamMemberDto ToMember(User user, Shop shop) => new()
    {
        UserId = user.Id,
        Email = user.Email,
        FullName = user.GetFullName(),
        IsApprover = shop.ApproverUserIds.Contains(user.Id, StringComparer.OrdinalIgnoreCase)
    };
}

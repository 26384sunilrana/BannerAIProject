namespace BannerService.Application.Services;

using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;

public class CreateAdminDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

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

/// <summary>Admin user management for Super Admin: create global admins, manage deletions, handle approvals.</summary>
public class UserAdminService
{
    public const int MaxPageSize = 100;
    private const int MinPasswordLength = 10;
    private const string SuperAdminEmail = "26384sunilrana@gmail.com";

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHashService _passwords;
    private readonly IShopRepository _shops;
    private readonly IAdminDeletionRequestRepository _deletionRequests;

    public UserAdminService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IRoleRepository roles,
        IPasswordHashService passwords,
        IShopRepository shops,
        IAdminDeletionRequestRepository deletionRequests)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _roles = roles;
        _passwords = passwords;
        _shops = shops;
        _deletionRequests = deletionRequests;
    }

    public async Task<AdminUserDto> CreateAdminAsync(CreateAdminDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (!email.Contains('@') || email.Length > 200)
            throw new ArgumentException("That does not look like an e-mail address.");
        if (request.Password.Length < MinPasswordLength)
            throw new ArgumentException($"An administrator password needs at least {MinPasswordLength} characters.");

        if (await _users.ExistsAsync(email))
            throw new InvalidOperationException("An account with this email already exists.");

        var shopId = await GetSystemShopIdAsync();
        var user = new User
        {
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            ShopId = shopId,
            EmailVerified = true,
            IsActive = true,
        };

        user.PasswordHash = _passwords.HashPassword(request.Password);
        var createdUser = await _users.CreateAsync(user);

        await _roles.AssignRoleAsync(createdUser.Id, "1");

        var userWithRoles = await _users.GetByIdAsync(createdUser.Id)
            ?? throw new InvalidOperationException("Failed to load created user.");
        return ToDto(userWithRoles);
    }

    public async Task<List<AdminUserDto>> GetAllAdminsAsync()
    {
        var admins = await _users.GetAllActiveAsync();
        var result = new List<AdminUserDto>();

        foreach (var admin in admins)
        {
            if (admin.HasRole(Role.SuperAdmin) || admin.HasRole(Role.Admin))
                result.Add(ToDto(admin));
        }

        return result.OrderBy(a => a.Email).ToList();
    }

    public async Task<AdminDeletionRequestDto> RequestAdminDeletionAsync(string adminIdToDelete, string requestedByAdminId, string? reason = null)
    {
        if (adminIdToDelete == requestedByAdminId)
            throw new InvalidOperationException("You cannot request deletion of your own account");

        var adminToDelete = await _users.GetByIdAsync(adminIdToDelete)
            ?? throw new KeyNotFoundException("Admin not found");

        if (IsOwner(adminToDelete))
            throw new InvalidOperationException("The Super Admin cannot be deleted");

        if (!adminToDelete.IsActive || !adminToDelete.HasRole(Role.Admin))
            throw new InvalidOperationException("That person is not an active administrator");

        var existingRequest = await _deletionRequests.GetPendingByAdminIdAsync(adminIdToDelete);
        if (existingRequest != null)
            throw new InvalidOperationException("There is already a pending deletion request for this admin");

        var request = new AdminDeletionRequest
        {
            AdminIdToDelete = adminIdToDelete,
            RequestedByAdminId = requestedByAdminId,
            Reason = reason
        };

        await _deletionRequests.CreateAsync(request);
        return ToDeletionRequestDto(request, adminToDelete, null);
    }

    public async Task<AdminDeletionRequestDto> ApproveDeletionAsync(string requestId, string superAdminId)
    {
        var superAdmin = await _users.GetByIdAsync(superAdminId)
            ?? throw new KeyNotFoundException("Super Admin not found");

        if (superAdmin.Email != SuperAdminEmail)
            throw new UnauthorizedAccessException("Only the Super Admin can approve admin deletions");

        var request = await _deletionRequests.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Deletion request not found");

        if (request.Status != AdminDeletionStatus.Pending)
            throw new InvalidOperationException("This request has already been processed");

        var adminToDelete = await _users.GetByIdAsync(request.AdminIdToDelete)
            ?? throw new KeyNotFoundException("Admin not found");
        if (IsOwner(adminToDelete))
            throw new InvalidOperationException("The Super Admin cannot be deleted");

        // "Deleted" means the active flag is switched off and every session ends; the record stays for the audit trail
        await SwitchOffAsync(adminToDelete);

        request.Status = AdminDeletionStatus.Completed;
        request.ApprovedByAdminId = superAdminId;
        request.ApprovedAt = DateTime.UtcNow;
        await _deletionRequests.UpdateAsync(request);

        var requestedByAdmin = await _users.GetByIdAsync(request.RequestedByAdminId);
        return ToDeletionRequestDto(request, adminToDelete, requestedByAdmin);
    }

    public async Task<AdminDeletionRequestDto> RejectDeletionAsync(string requestId, string superAdminId)
    {
        var superAdmin = await _users.GetByIdAsync(superAdminId)
            ?? throw new KeyNotFoundException("Super Admin not found");

        if (superAdmin.Email != SuperAdminEmail)
            throw new UnauthorizedAccessException("Only the Super Admin can reject admin deletions");

        var request = await _deletionRequests.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Deletion request not found");

        if (request.Status != AdminDeletionStatus.Pending)
            throw new InvalidOperationException("This request has already been processed");

        request.Status = AdminDeletionStatus.Rejected;
        request.ApprovedByAdminId = superAdminId;
        request.ApprovedAt = DateTime.UtcNow;

        await _deletionRequests.UpdateAsync(request);

        var adminToDelete = await _users.GetByIdAsync(request.AdminIdToDelete);
        var requestedByAdmin = await _users.GetByIdAsync(request.RequestedByAdminId);

        return ToDeletionRequestDto(request, adminToDelete, requestedByAdmin);
    }

    public async Task<List<AdminDeletionRequestDto>> GetPendingDeletionRequestsAsync(string superAdminId)
    {
        var superAdmin = await _users.GetByIdAsync(superAdminId)
            ?? throw new KeyNotFoundException("Super Admin not found");

        if (superAdmin.Email != SuperAdminEmail)
            throw new UnauthorizedAccessException("Only the Super Admin can view deletion requests");

        var requests = await _deletionRequests.GetPendingAsync();
        var result = new List<AdminDeletionRequestDto>();

        foreach (var req in requests)
        {
            var adminToDelete = await _users.GetByIdAsync(req.AdminIdToDelete);
            var requestedByAdmin = await _users.GetByIdAsync(req.RequestedByAdminId);
            result.Add(ToDeletionRequestDto(req, adminToDelete, requestedByAdmin));
        }

        return result;
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
        if (IsOwner(user))
            throw new InvalidOperationException("The Super Admin cannot be deactivated");

        await SwitchOffAsync(user);
        return ToDto(user);
    }

    /// <summary>The owner's login is changed only by the owner (and by the command line on the server): nobody else may touch it.</summary>
    public async Task EnsureNotOwnerAsync(string userId, string actingUserId)
    {
        var target = await Load(userId);
        if (IsOwner(target) && !string.Equals(userId, actingUserId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only the Super Admin can change the Super Admin's sign-in");
    }

    private static bool IsOwner(User user) => string.Equals(user.Email, SuperAdminEmail, StringComparison.OrdinalIgnoreCase);

    private async Task SwitchOffAsync(User user)
    {
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        // End existing sessions: refresh tokens stop working immediately
        foreach (var token in await _refreshTokens.GetActiveByUserIdAsync(user.Id))
        {
            token.Revoke();
            await _refreshTokens.UpdateAsync(token);
        }
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

    private async Task<string> GetSystemShopIdAsync()
    {
        var systemShop = await _shops.GetByNameAsync("System Shop");
        return systemShop?.Id.ToString() ?? Guid.NewGuid().ToString();
    }

    private static AdminDeletionRequestDto ToDeletionRequestDto(AdminDeletionRequest request, User? adminToDelete, User? requestedByAdmin) => new()
    {
        Id = request.Id,
        AdminIdToDelete = request.AdminIdToDelete,
        AdminToDeleteEmail = adminToDelete?.Email ?? "Unknown",
        AdminToDeleteName = adminToDelete?.GetFullName() ?? "Unknown",
        RequestedByAdminId = request.RequestedByAdminId,
        RequestedByAdminEmail = requestedByAdmin?.Email ?? "Unknown",
        Status = (int)request.Status,
        StatusName = request.Status.ToString(),
        ApprovedByAdminId = request.ApprovedByAdminId,
        ApprovedByAdminEmail = null,
        Reason = request.Reason,
        CreatedAt = request.CreatedAt,
        ApprovedAt = request.ApprovedAt
    };

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

namespace BannerService.Application.Services;

using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;

public class ShopTakeoverDto
{
    public Guid Id { get; set; }

    /// <summary>Incoming: someone asks to take over the caller's shop. Outgoing: the caller asked. Admin: seen by an administrator.</summary>
    public string Direction { get; set; } = string.Empty;
    public string ExistingShopName { get; set; } = string.Empty;
    public string NewShopName { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string ExistingOwnerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Associates { get; set; }
    public string? DecidedByName { get; set; }
    public bool DecidedByAdmin { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }

    /// <summary>What the caller may do now: confirm, decline, cancel.</summary>
    public List<string> Can { get; set; } = new();
}

/// <summary>
/// A shop changing hands. The new owner asks (naming the shop and its address), the old owner confirms with their password and chooses what
/// happens to the associates, or an administrator confirms for an owner who cannot be reached. When the associates stay only the owner is
/// swapped and the new owner has to confirm who approves banners; when they change the old shop is closed and the new owner's shop takes its place.
/// </summary>
public class ShopTakeoverService
{
    private readonly IShopTakeoverRepository _takeovers;
    private readonly IShopRepository _shops;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPasswordHashService _passwords;
    private readonly NotificationService _notifications;

    public ShopTakeoverService(
        IShopTakeoverRepository takeovers,
        IShopRepository shops,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        ISubscriptionRepository subscriptions,
        IPasswordHashService passwords,
        NotificationService notifications)
    {
        _takeovers = takeovers;
        _shops = shops;
        _users = users;
        _refreshTokens = refreshTokens;
        _subscriptions = subscriptions;
        _passwords = passwords;
        _notifications = notifications;
    }

    // ----- asking

    /// <summary>The owner of a shop says that the shop with this name and address is theirs now.</summary>
    public async Task<ShopTakeoverDto> RequestAsync(Guid callerShopId, string callerUserId, string? name, string? address, string? postalCode)
    {
        var mine = await _shops.GetByIdAsync(callerShopId) ?? throw new KeyNotFoundException("Shop not found.");
        if (mine.OwnerUserId == null || !SameId(mine.OwnerUserId.Value.ToString(), callerUserId))
            throw new UnauthorizedAccessException("Only the owner of a shop can ask to take another shop over.");
        if (mine.Status != ShopStatus.Active) throw new InvalidOperationException("Your shop is not active.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Give the name of the shop.");
        if (!ShopIdentity.HasAddress(address)) throw new ArgumentException("Give the street address of the shop.");

        var existing = (await _shops.ListByNameAsync(name))
            .FirstOrDefault(s => s.Id != callerShopId && s.Status == ShopStatus.Active && ShopIdentity.Same(name, address, postalCode, s.Name, s.Address, s.PostalCode))
            ?? throw new KeyNotFoundException("No shop with that name and address was found.");

        if ((await _takeovers.OpenForExistingShopAsync(existing.Id)).Any(t => t.NewShopId == callerShopId))
            throw new InvalidOperationException("You have already asked to take this shop over. Wait for the owner's answer or cancel the request.");

        var takeover = new ShopTakeover
        {
            ExistingShopId = existing.Id,
            NewShopId = callerShopId,
            NewOwnerUserId = callerUserId,
            OldOwnerUserId = existing.OwnerUserId?.ToString(),
        };
        await _takeovers.AddAsync(takeover);

        var requester = await _users.GetByIdAsync(callerUserId);
        var text = $"{requester?.GetFullName() ?? "Someone"} ({EmailService.Mask(requester?.Email)}) says the shop \"{existing.Name}\" is theirs now and asks to take it over.";
        if (existing.OwnerUserId != null)
            await _notifications.NotifyShopOwnersAsync(existing.Id, "TakeoverRequested", "Someone asks to take over your shop", text, "/takeover");
        else
            await _notifications.NotifyAdminsAsync("TakeoverRequested", "A shop without an owner has a takeover request", text, "/takeover");

        return await ToDtoAsync(takeover, "Outgoing", new ActorView(callerUserId, callerShopId, false));
    }

    public async Task<List<ShopTakeoverDto>> ListAsync(string callerUserId, Guid? callerShopId, bool isAdmin)
    {
        var items = isAdmin
            ? await _takeovers.ListAllAsync()
            : callerShopId == null ? new List<ShopTakeover>() : await _takeovers.ListForShopAsync(callerShopId.Value);

        var view = new ActorView(callerUserId, callerShopId, isAdmin);
        var result = new List<ShopTakeoverDto>();
        foreach (var t in items)
        {
            var direction = isAdmin ? "Admin" : t.ExistingShopId == callerShopId ? "Incoming" : "Outgoing";
            result.Add(await ToDtoAsync(t, direction, view));
        }
        return result;
    }

    // ----- answering

    /// <summary>The owner of the existing shop agrees, re-enters their password and says whether the associates stay.</summary>
    public async Task<ShopTakeoverDto> ConfirmAsync(string callerUserId, Guid callerShopId, Guid id, TakeoverAssociates? decision, string? password)
    {
        var takeover = await LoadAsync(id);
        var existing = await _shops.GetByIdAsync(takeover.ExistingShopId) ?? throw new KeyNotFoundException("The shop was not found.");
        if (existing.Id != callerShopId || existing.OwnerUserId == null || !SameId(existing.OwnerUserId.Value.ToString(), callerUserId))
            throw new UnauthorizedAccessException("Only the owner of this shop can answer.");
        if (decision == null) throw new ArgumentException("Choose what happens to the associates.");

        var caller = await _users.GetByIdAsync(callerUserId) ?? throw new KeyNotFoundException("User not found.");
        if (caller.IsLockedOut) throw new InvalidOperationException("This account is locked. Ask the administrator to unlock it.");

        // handing a shop over is as serious as handing over the owner role: the password counts as a sign-in attempt
        if (string.IsNullOrEmpty(password) || !_passwords.VerifyPassword(password, caller.PasswordHash))
        {
            caller.RecordLoginAttempt();
            await _users.UpdateAsync(caller);
            throw new ArgumentException("Your password is not right.");
        }
        caller.ResetLoginAttempts();
        await _users.UpdateAsync(caller);

        await CompleteAsync(takeover, decision.Value, caller.Id, caller.GetFullName(), byAdmin: false, note: null);
        return await ToDtoAsync(takeover, "Incoming", new ActorView(callerUserId, callerShopId, false));
    }

    /// <summary>An administrator answers for an owner who cannot be reached (or when the shop has no owner).</summary>
    public async Task<ShopTakeoverDto> ConfirmAsAdminAsync(string adminUserId, string adminName, Guid id, TakeoverAssociates? decision, string? note)
    {
        var takeover = await LoadAsync(id);
        if (decision == null) throw new ArgumentException("Choose what happens to the associates.");
        if (string.IsNullOrWhiteSpace(note)) throw new ArgumentException("Say why you are answering for the owner (for example: the owner cannot be reached and sent proof).");

        await CompleteAsync(takeover, decision.Value, adminUserId, adminName, byAdmin: true, note: Clip(note));
        return await ToDtoAsync(takeover, "Admin", new ActorView(adminUserId, null, true));
    }

    public async Task<ShopTakeoverDto> DeclineAsync(string callerUserId, Guid callerShopId, Guid id, string? note, bool isAdmin, string callerName)
    {
        var takeover = await LoadAsync(id);
        var existing = await _shops.GetByIdAsync(takeover.ExistingShopId) ?? throw new KeyNotFoundException("The shop was not found.");
        var isOwner = existing.Id == callerShopId && existing.OwnerUserId != null && SameId(existing.OwnerUserId.Value.ToString(), callerUserId);
        if (!isOwner && !isAdmin) throw new UnauthorizedAccessException("Only the owner of this shop can answer.");

        takeover.Status = ShopTakeoverStatus.Declined;
        takeover.DecidedAt = DateTime.UtcNow;
        takeover.DecidedByUserId = callerUserId;
        takeover.DecidedByName = callerName;
        takeover.DecidedByAdmin = !isOwner;
        takeover.Note = Clip(note);
        await _takeovers.SaveAsync();

        await _notifications.NotifyUsersAsync(new[] { takeover.NewOwnerUserId }, "TakeoverDeclined", "Your takeover request was declined",
            $"The request to take over \"{existing.Name}\" was declined" + (takeover.Note == null ? "." : $": {takeover.Note}"), "/takeover");
        return await ToDtoAsync(takeover, isAdmin && !isOwner ? "Admin" : "Incoming", new ActorView(callerUserId, callerShopId, isAdmin));
    }

    public async Task<ShopTakeoverDto> CancelAsync(string callerUserId, Guid callerShopId, Guid id)
    {
        var takeover = await LoadAsync(id);
        if (takeover.NewShopId != callerShopId || !SameId(takeover.NewOwnerUserId, callerUserId))
            throw new UnauthorizedAccessException("Only the person who asked can cancel the request.");

        takeover.Status = ShopTakeoverStatus.Cancelled;
        takeover.DecidedAt = DateTime.UtcNow;
        await _takeovers.SaveAsync();
        return await ToDtoAsync(takeover, "Outgoing", new ActorView(callerUserId, callerShopId, false));
    }

    // ----- doing it

    private async Task CompleteAsync(ShopTakeover takeover, TakeoverAssociates decision, string byUserId, string byName, bool byAdmin, string? note)
    {
        var existing = await _shops.GetByIdAsync(takeover.ExistingShopId) ?? throw new KeyNotFoundException("The shop was not found.");
        var fresh = await _shops.GetByIdAsync(takeover.NewShopId) ?? throw new KeyNotFoundException("The new owner's shop was not found.");
        var newOwner = await _users.GetByIdAsync(takeover.NewOwnerUserId) ?? throw new KeyNotFoundException("The new owner was not found.");

        if (existing.Status != ShopStatus.Active) throw new InvalidOperationException("The shop is no longer active.");
        if (fresh.Status != ShopStatus.Active) throw new InvalidOperationException("The new owner's shop is no longer active.");
        if (!newOwner.IsActive || fresh.OwnerUserId == null || !SameId(fresh.OwnerUserId.Value.ToString(), newOwner.Id))
            throw new InvalidOperationException("The person who asked is no longer the owner of the shop they asked from.");

        var oldMembers = await _users.GetByShopIdAsync(existing.Id.ToString());
        var oldOwner = existing.OwnerUserId == null ? null : oldMembers.FirstOrDefault(u => SameId(u.Id, existing.OwnerUserId.Value.ToString()));

        if (decision == TakeoverAssociates.Keep)
        {
            // the old owner leaves; the new owner moves into the existing shop and becomes its owner
            if (oldOwner != null) await SwitchOffAsync(oldOwner);
            newOwner.ShopId = existing.Id.ToString();
            newOwner.UpdatedAt = DateTime.UtcNow;
            await _users.UpdateAsync(newOwner);
            await RevokeSessionsAsync(newOwner.Id);

            existing.AssignOwner(Guid.Parse(newOwner.Id));
            existing.RemoveApprover(newOwner.Id);
            existing.ApprovalReviewRequired = true; // the new owner confirms who approves before anything else is approved
            await _shops.UpdateAsync(existing);

            // the old owner's payment method does not carry over
            await ClearBillingAsync(existing.Id);

            fresh.RemoveOwner();
            fresh.SetStatus(ShopStatus.Archived);
            await _shops.UpdateAsync(fresh);
        }
        else
        {
            // the old shop closes: nobody of it can sign in, its subscription stops renewing
            existing.SetStatus(ShopStatus.Inactive);
            await _shops.UpdateAsync(existing);
            foreach (var member in oldMembers) await SwitchOffAsync(member);
            await ClearBillingAsync(existing.Id);

            // the new owner's shop takes the place: same address and place, new people
            fresh.UpdateLocation(existing.Address, existing.City, existing.CountryCode, existing.StateId, existing.DistrictId, existing.PostalCode, existing.Latitude, existing.Longitude);
            fresh.CityId = existing.CityId;
            fresh.GroupId = existing.GroupId;
            fresh.TimeZoneId ??= existing.TimeZoneId;
            await _shops.UpdateAsync(fresh);
            await RevokeSessionsAsync(newOwner.Id);
        }

        takeover.Status = ShopTakeoverStatus.Completed;
        takeover.Associates = decision;
        takeover.DecidedAt = DateTime.UtcNow;
        takeover.DecidedByUserId = byUserId;
        takeover.DecidedByName = byName;
        takeover.DecidedByAdmin = byAdmin;
        takeover.Note = note;
        await _takeovers.SaveAsync();

        // anyone else who asked for the same shop is told it went to someone else
        foreach (var other in (await _takeovers.OpenForExistingShopAsync(existing.Id)).Where(o => o.Id != takeover.Id))
        {
            other.Status = ShopTakeoverStatus.Declined;
            other.DecidedAt = DateTime.UtcNow;
            other.Note = "The shop was taken over by someone else.";
            await _notifications.NotifyUsersAsync(new[] { other.NewOwnerUserId }, "TakeoverDeclined", "Your takeover request was closed", other.Note, "/takeover");
        }
        await _takeovers.SaveAsync();

        var what = decision == TakeoverAssociates.Keep
            ? "You are now the owner. Open Team and confirm who approves banners before anything can be approved."
            : "The old shop is closed and your shop has its address. Add your associates under Team.";
        await _notifications.NotifyUsersAsync(new[] { takeover.NewOwnerUserId }, "TakeoverCompleted", "The shop is yours", $"The takeover of \"{existing.Name}\" is done. {what} Sign in again to see it.", "/takeover");
        await _notifications.NotifyAdminsAsync("TakeoverCompleted", "A shop changed hands", $"\"{existing.Name}\" was taken over ({(decision == TakeoverAssociates.Keep ? "owner only" : "owner and associates")})" + (byAdmin ? $" on the owner's behalf by {byName}." : "."), "/takeover");
    }

    private async Task SwitchOffAsync(User user)
    {
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        await RevokeSessionsAsync(user.Id);
    }

    private async Task ClearBillingAsync(Guid shopId)
    {
        var subscription = await _subscriptions.GetByShopIdAsync(shopId);
        if (subscription == null) return;
        subscription.AutoRenew = false;
        subscription.PaymentMethodId = null;
        await _subscriptions.UpdateAsync(subscription);
    }

    private async Task RevokeSessionsAsync(string userId)
    {
        foreach (var token in await _refreshTokens.GetActiveByUserIdAsync(userId))
        {
            token.Revoke();
            await _refreshTokens.UpdateAsync(token);
        }
    }

    // ----- helpers

    private sealed record ActorView(string UserId, Guid? ShopId, bool IsAdmin);

    private async Task<ShopTakeover> LoadAsync(Guid id)
    {
        var takeover = await _takeovers.GetByIdAsync(id) ?? throw new KeyNotFoundException("Request not found.");
        if (takeover.Status != ShopTakeoverStatus.Requested) throw new InvalidOperationException("This request has already been answered.");
        return takeover;
    }

    private async Task<ShopTakeoverDto> ToDtoAsync(ShopTakeover t, string direction, ActorView actor)
    {
        var existing = await _shops.GetByIdAsync(t.ExistingShopId);
        var fresh = await _shops.GetByIdAsync(t.NewShopId);
        var requester = await _users.GetByIdAsync(t.NewOwnerUserId);
        var oldOwner = t.OldOwnerUserId == null ? null : await _users.GetByIdAsync(t.OldOwnerUserId);

        var can = new List<string>();
        if (t.Status == ShopTakeoverStatus.Requested)
        {
            var isOldOwner = existing?.OwnerUserId != null && SameId(existing.OwnerUserId.Value.ToString(), actor.UserId) && actor.ShopId == existing.Id;
            if (isOldOwner) { can.Add("confirm"); can.Add("decline"); }
            if (actor.IsAdmin) { can.Add("confirm-as-admin"); can.Add("decline"); }
            if (SameId(t.NewOwnerUserId, actor.UserId) && actor.ShopId == t.NewShopId) can.Add("cancel");
        }

        return new ShopTakeoverDto
        {
            Id = t.Id,
            Direction = direction,
            ExistingShopName = existing?.Name ?? string.Empty,
            NewShopName = fresh?.Name ?? string.Empty,
            RequesterName = requester?.GetFullName() ?? string.Empty,
            RequesterEmail = EmailService.Mask(requester?.Email),
            ExistingOwnerName = oldOwner?.GetFullName() ?? string.Empty,
            Status = t.Status.ToString(),
            Associates = t.Associates?.ToString(),
            DecidedByName = t.DecidedByName,
            DecidedByAdmin = t.DecidedByAdmin,
            Note = t.Note,
            CreatedAt = DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc),
            DecidedAt = t.DecidedAt == null ? null : DateTime.SpecifyKind(t.DecidedAt.Value, DateTimeKind.Utc),
            Can = can,
        };
    }

    private static bool SameId(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string? Clip(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim().Length > 500 ? text.Trim()[..500] : text.Trim();
}

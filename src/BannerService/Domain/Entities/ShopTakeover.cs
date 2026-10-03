namespace BannerService.Domain.Entities;

public enum ShopTakeoverStatus
{
    /// <summary>The new owner has asked; the owner of the existing shop has not answered.</summary>
    Requested = 1,
    Declined = 2,
    Cancelled = 3,
    Completed = 4,
}

/// <summary>What happens to the people who work in the shop when it changes hands.</summary>
public enum TakeoverAssociates
{
    /// <summary>Only the owner changes. The shop, its logins, banners and subscription stay.</summary>
    Keep = 1,

    /// <summary>The old shop is closed and the new owner's shop takes its place; the new owner adds new associates.</summary>
    Replace = 2,
}

/// <summary>
/// A shop that changes hands. Two shops may share a name when their addresses differ, but one name at one address is one shop: the new owner
/// asks, and the owner of the existing shop (or an administrator, when that owner cannot be reached) confirms, choosing whether the associates stay.
/// </summary>
public class ShopTakeover
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The shop that is being taken over.</summary>
    public Guid ExistingShopId { get; set; }

    /// <summary>The shop the new owner signed up with. It takes the place of the old one (Replace) or is closed (Keep).</summary>
    public Guid NewShopId { get; set; }
    public string NewOwnerUserId { get; set; } = string.Empty;

    /// <summary>The owner of the existing shop when the request was made, if it had one.</summary>
    public string? OldOwnerUserId { get; set; }

    public ShopTakeoverStatus Status { get; set; } = ShopTakeoverStatus.Requested;
    public TakeoverAssociates? Associates { get; set; }

    /// <summary>Who answered: the old owner, or an administrator on their behalf.</summary>
    public string? DecidedByUserId { get; set; }
    public string? DecidedByName { get; set; }
    public bool DecidedByAdmin { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}

namespace BannerService.Domain.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ValueObjects;

public class BannerVersion
{
    public Guid Id { get; set; }
    public Guid BannerId { get; set; }
    public Guid ShopId { get; set; }
    public int VersionNumber { get; set; }

    public string SnapshotJson { get; set; } = string.Empty;

    [NotMapped]
    public BannerSnapshot Snapshot
    {
        get => string.IsNullOrEmpty(SnapshotJson)
            ? new BannerSnapshot()
            : JsonSerializer.Deserialize<BannerSnapshot>(SnapshotJson) ?? new BannerSnapshot();
        set => SnapshotJson = JsonSerializer.Serialize(value);
    }
    public string? ChangeDescription { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public bool IsActive { get; set; }

    public BannerVersion() { }

    public BannerVersion(Guid bannerId, Guid shopId, int versionNumber,
        BannerSnapshot snapshot, Guid createdBy, string? changeDescription = null)
    {
        Id = Guid.NewGuid();
        BannerId = bannerId;
        ShopId = shopId;
        VersionNumber = versionNumber;
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        CreatedBy = createdBy;
        ChangeDescription = changeDescription;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;

        if (!string.IsNullOrEmpty(changeDescription) && changeDescription.Length > 500)
            throw new ArgumentException("Change description must be <= 500 characters");
    }
}

# DDD-02: Technical Design — Version Control Service

**Status**: Stage 2 of 5 (Technical Design)  
**Bolt**: 004-version-control-service  
**Created**: 2026-09-26  

---

## Architecture Overview

Version Control Service is a **domain service** integrated into Banner Service. It does not have its own microservice boundary yet — it shares the Banner Service database and API.

```
┌─────────────────────────────────────────────────────┐
│           Presentation Layer                         │
│  (Controllers, DTOs, API Endpoints)                 │
└──────────┬──────────────────────────────────────────┘
           │
┌──────────▼──────────────────────────────────────────┐
│           Application Layer                          │
│  (Services, Dto Mapping, Orchestration)             │
│  - BannerService (existing)                         │
│  - VersionControlService (NEW)                      │
└──────────┬──────────────────────────────────────────┘
           │
┌──────────▼──────────────────────────────────────────┐
│           Domain Layer                               │
│  (Entities, Value Objects, Domain Services)         │
│  - Banner, Component (existing)                     │
│  - BannerVersion, BannerSnapshot (NEW)              │
│  - VersionControlService logic (NEW)                │
└──────────┬──────────────────────────────────────────┘
           │
┌──────────▼──────────────────────────────────────────┐
│           Infrastructure Layer                       │
│  (Database, Repositories, Migrations)               │
│  - BannerRepository (existing)                      │
│  - BannerVersionRepository (NEW)                    │
│  - EF Core DbContext (update)                       │
└─────────────────────────────────────────────────────┘
```

---

## Database Schema

### New Table: BannerVersions

```sql
CREATE TABLE [dbo].[BannerVersions] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [BannerId] [uniqueidentifier] NOT NULL,
    [ShopId] [uniqueidentifier] NOT NULL,
    [VersionNumber] [int] NOT NULL,
    [SnapshotJson] [nvarchar](max) NOT NULL,  -- Serialized BannerSnapshot
    [ChangeDescription] [nvarchar](500) NULL,
    [CreatedAt] [datetime2] NOT NULL,
    [CreatedBy] [uniqueidentifier] NOT NULL,
    [IsActive] [bit] NOT NULL DEFAULT 1,
    
    CONSTRAINT [FK_BannerVersions_Banners] 
        FOREIGN KEY ([BannerId]) REFERENCES [dbo].[Banners]([Id]),
    
    CONSTRAINT [UQ_BannerVersions_Number] 
        UNIQUE ([BannerId], [VersionNumber]),
);

CREATE INDEX [IX_BannerVersions_Query] 
    ON [dbo].[BannerVersions] ([BannerId], [ShopId], [VersionNumber] DESC)
    WHERE [IsActive] = 1;
```

**Rationale**:
- `SnapshotJson` stores complete banner state (name, description, width, height, components)
- Unique constraint on (BannerId, VersionNumber) enforces sequential numbering
- No composite primary key; Id is the identity
- Index optimized for "get all versions for banner" queries
- IsActive flag allows future optimization (soft delete pattern)

### Migration File

```csharp
// Migrations/202609261000_AddBannerVersions.cs

public partial class AddBannerVersions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BannerVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                BannerId = table.Column<Guid>(nullable: false),
                ShopId = table.Column<Guid>(nullable: false),
                VersionNumber = table.Column<int>(nullable: false),
                SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ChangeDescription = table.Column<string>(maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                CreatedBy = table.Column<Guid>(nullable: false),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BannerVersions", x => x.Id);
                table.ForeignKey("FK_BannerVersions_Banners", 
                    x => x.BannerId, "Banners", "Id", 
                    onDelete: ReferentialAction.Cascade);
                table.UniqueConstraint("UQ_BannerVersions_Number", 
                    x => new { x.BannerId, x.VersionNumber });
            });

        migrationBuilder.CreateIndex(
            name: "IX_BannerVersions_Query",
            table: "BannerVersions",
            columns: new[] { "BannerId", "ShopId", "VersionNumber" },
            descending: new[] { false, false, true });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BannerVersions");
    }
}
```

---

## Entity Model (C# Code Structure)

### Domain Layer Entities

```csharp
// Domain/Entities/BannerVersion.cs
public class BannerVersion
{
    public Guid Id { get; set; }
    public Guid BannerId { get; set; }
    public Guid ShopId { get; set; }
    public int VersionNumber { get; set; }
    
    public BannerSnapshot Snapshot { get; private set; } = null!;
    public string? ChangeDescription { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public bool IsActive { get; set; }
    
    // Constructor for creation
    public BannerVersion(Guid bannerId, Guid shopId, int versionNumber, 
        BannerSnapshot snapshot, Guid createdBy, string? changeDescription = null)
    {
        Id = Guid.NewGuid();
        BannerId = bannerId;
        ShopId = shopId;
        VersionNumber = versionNumber;
        Snapshot = snapshot;
        CreatedBy = createdBy;
        ChangeDescription = changeDescription;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
}

// Domain/ValueObjects/BannerSnapshot.cs
public class BannerSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public List<ComponentSnapshot> Components { get; set; } = new();
    public DateTime CapturedAt { get; set; }
    
    public BannerSnapshot(Banner banner)
    {
        Name = banner.Name;
        Description = banner.Description;
        Width = banner.Width;
        Height = banner.Height;
        CapturedAt = DateTime.UtcNow;
        Components = banner.Components
            .Select(c => new ComponentSnapshot(c))
            .ToList();
    }
}

// Domain/ValueObjects/ComponentSnapshot.cs
public class ComponentSnapshot
{
    public Guid ComponentId { get; set; }
    public int ComponentType { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int SizeWidth { get; set; }
    public int SizeHeight { get; set; }
    public int ZIndex { get; set; }
    public string PropertiesJson { get; set; } = string.Empty;
    
    public ComponentSnapshot(Component component)
    {
        ComponentId = component.Id;
        ComponentType = (int)component.ComponentType;
        PositionX = component.PositionX;
        PositionY = component.PositionY;
        SizeWidth = component.SizeWidth;
        SizeHeight = component.SizeHeight;
        ZIndex = component.ZIndex;
        PropertiesJson = component.PropertiesJson;
    }
}
```

### Data Layer

```csharp
// Infrastructure/Data/ApplicationDbContext.cs (UPDATED)
public DbSet<BannerVersion> BannerVersions { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations ...
    
    modelBuilder.Entity<BannerVersion>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.SnapshotJson).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.CreatedBy).IsRequired();
        
        entity.HasIndex(e => new { e.BannerId, e.ShopId, e.VersionNumber })
            .HasDatabaseName("IX_BannerVersions_Query");
        
        entity.HasIndex(e => new { e.BannerId, e.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UQ_BannerVersions_Number");
        
        entity.HasOne<Banner>()
            .WithMany()
            .HasForeignKey(e => e.BannerId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}

// Infrastructure/Repositories/BannerVersionRepository.cs (NEW)
public interface IBannerVersionRepository
{
    Task<BannerVersion> SaveAsync(BannerVersion version);
    Task<List<BannerVersion>> GetVersionsAsync(Guid bannerId, Guid shopId);
    Task<BannerVersion?> GetVersionAsync(Guid bannerId, int versionNumber, Guid shopId);
    Task<int> GetNextVersionNumberAsync(Guid bannerId, Guid shopId);
}

public class BannerVersionRepository : IBannerVersionRepository
{
    private readonly ApplicationDbContext _context;
    
    public BannerVersionRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<BannerVersion> SaveAsync(BannerVersion version)
    {
        _context.BannerVersions.Add(version);
        await _context.SaveChangesAsync();
        return version;
    }
    
    public async Task<List<BannerVersion>> GetVersionsAsync(Guid bannerId, Guid shopId)
    {
        return await _context.BannerVersions
            .Where(v => v.BannerId == bannerId && v.ShopId == shopId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }
    
    public async Task<BannerVersion?> GetVersionAsync(Guid bannerId, int versionNumber, Guid shopId)
    {
        return await _context.BannerVersions
            .FirstOrDefaultAsync(v => v.BannerId == bannerId && 
                v.VersionNumber == versionNumber && v.ShopId == shopId);
    }
    
    public async Task<int> GetNextVersionNumberAsync(Guid bannerId, Guid shopId)
    {
        var maxVersion = await _context.BannerVersions
            .Where(v => v.BannerId == bannerId && v.ShopId == shopId)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0;
        return maxVersion + 1;
    }
}
```

---

## Application Layer

```csharp
// Application/Services/VersionControlService.cs (NEW)
public interface IVersionControlService
{
    Task<BannerVersion> CreateSnapshotAsync(Banner banner, string? changeDescription, Guid userId);
    Task<List<BannerVersionDto>> ListVersionsAsync(Guid bannerId, Guid shopId);
    Task<Banner> RestoreVersionAsync(Guid bannerId, int versionNumber, Guid shopId, Guid userId);
}

public class VersionControlService : IVersionControlService
{
    private readonly IBannerVersionRepository _versionRepository;
    private readonly IBannerRepository _bannerRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public VersionControlService(
        IBannerVersionRepository versionRepository,
        IBannerRepository bannerRepository,
        IUnitOfWork unitOfWork)
    {
        _versionRepository = versionRepository;
        _bannerRepository = bannerRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<BannerVersion> CreateSnapshotAsync(Banner banner, string? changeDescription, Guid userId)
    {
        if (banner == null) throw new ArgumentNullException(nameof(banner));
        if (!string.IsNullOrEmpty(changeDescription) && changeDescription.Length > 500)
            throw new ArgumentException("Change description must be <= 500 chars");
        
        var versionNumber = await _versionRepository.GetNextVersionNumberAsync(banner.Id, banner.ShopId);
        var snapshot = new BannerSnapshot(banner);
        var version = new BannerVersion(banner.Id, banner.ShopId, versionNumber, snapshot, userId, changeDescription);
        
        return await _versionRepository.SaveAsync(version);
    }
    
    public async Task<List<BannerVersionDto>> ListVersionsAsync(Guid bannerId, Guid shopId)
    {
        var versions = await _versionRepository.GetVersionsAsync(bannerId, shopId);
        return versions.Select(v => new BannerVersionDto
        {
            VersionNumber = v.VersionNumber,
            CreatedAt = v.CreatedAt,
            CreatedBy = v.CreatedBy,
            ChangeDescription = v.ChangeDescription,
            IsActive = v.IsActive,
            ComponentCount = v.Snapshot.Components.Count,
            Width = v.Snapshot.Width,
            Height = v.Snapshot.Height
        }).ToList();
    }
    
    public async Task<Banner> RestoreVersionAsync(Guid bannerId, int versionNumber, Guid shopId, Guid userId)
    {
        if (versionNumber <= 0) throw new ArgumentException("Version number must be > 0");
        
        var version = await _versionRepository.GetVersionAsync(bannerId, versionNumber, shopId);
        if (version == null) throw new KeyNotFoundException($"Version {versionNumber} not found");
        
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null) throw new KeyNotFoundException("Banner not found");
        
        // Clear components and restore from snapshot
        foreach (var component in banner.Components.ToList())
            banner.RemoveComponent(component.Id);
        
        foreach (var componentSnapshot in version.Snapshot.Components)
        {
            var component = new Component(
                banner.Id,
                (ComponentType)componentSnapshot.ComponentType,
                new Position(componentSnapshot.PositionX, componentSnapshot.PositionY),
                new Size(componentSnapshot.SizeWidth, componentSnapshot.SizeHeight),
                componentSnapshot.ZIndex,
                componentSnapshot.PropertiesJson);
            banner.AddComponent(component);
        }
        
        banner.UpdateName(version.Snapshot.Name);
        banner.UpdateDescription(version.Snapshot.Description);
        
        // Create restoration snapshot
        var snapshot = new BannerSnapshot(banner);
        var restorationVersion = new BannerVersion(
            bannerId, shopId, 
            await _versionRepository.GetNextVersionNumberAsync(bannerId, shopId),
            snapshot, userId, 
            $"Restored to version {versionNumber}");
        
        await _bannerRepository.UpdateAsync(banner);
        await _versionRepository.SaveAsync(restorationVersion);
        await _unitOfWork.CommitAsync();
        
        return banner;
    }
}
```

---

## Presentation Layer (API)

### DTOs

```csharp
// Application/Dto/BannerVersionDto.cs (NEW)
public class BannerVersionDto
{
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? ChangeDescription { get; set; }
    public bool IsActive { get; set; }
    public int ComponentCount { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class BannerVersionDetailDto
{
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? ChangeDescription { get; set; }
    public string BannerName { get; set; } = string.Empty;
    public string BannerDescription { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public List<PreviewComponentDto> Components { get; set; } = new();
}

public class RestoreVersionRequestDto
{
    public int VersionNumber { get; set; }
}
```

### API Endpoints

```csharp
// Presentation/Controllers/VersionControlController.cs (NEW)
[ApiController]
[Route("api/banners/{bannerId}/versions")]
[Authorize]
public class VersionControlController : ControllerBase
{
    private readonly IVersionControlService _versionControlService;
    private readonly IBannerService _bannerService;
    private readonly ILogger<VersionControlController> _logger;
    
    [HttpGet]
    [ProducesResponseType(typeof(List<BannerVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListVersions(Guid bannerId)
    {
        var shopId = GetShopId();
        var versions = await _versionControlService.ListVersionsAsync(bannerId, shopId);
        return Ok(versions);
    }
    
    [HttpGet("{versionNumber}")]
    [ProducesResponseType(typeof(BannerVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(Guid bannerId, int versionNumber)
    {
        var shopId = GetShopId();
        var version = await _versionControlService.GetVersionDetailAsync(bannerId, versionNumber, shopId);
        if (version == null) return NotFound();
        return Ok(version);
    }
    
    [HttpPost("{versionNumber}/restore")]
    [ProducesResponseType(typeof(BannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreVersion(Guid bannerId, int versionNumber)
    {
        var shopId = GetShopId();
        var userId = GetUserId();
        
        var banner = await _versionControlService.RestoreVersionAsync(bannerId, versionNumber, shopId, userId);
        return Ok(BannerDto.FromBanner(banner));
    }
    
    private Guid GetShopId() => Guid.Parse(User.FindFirst("shop_id")?.Value ?? "");
    private Guid GetUserId() => Guid.Parse(User.FindFirst("sub")?.Value ?? "");
}
```

---

## Integration Points

### Modification to BannerService

When banner is saved, automatically create version snapshot:

```csharp
// Application/Services/BannerService.cs (UPDATED)
public async Task UpdateBannerAsync(Guid bannerId, UpdateBannerDto dto, Guid shopId, Guid userId)
{
    var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
    // ... update banner ...
    await _bannerRepository.UpdateAsync(banner);
    
    // NEW: Create version snapshot
    await _versionControlService.CreateSnapshotAsync(banner, dto.ChangeDescription, userId);
    
    await _unitOfWork.CommitAsync();
}
```

---

## Deployment Considerations

1. **Database Migration**: Apply migration before deployment
2. **No Breaking Changes**: Existing Banner Service endpoints unchanged
3. **Backward Compatibility**: Version Control features optional
4. **Multi-tenant Safety**: All queries filter by ShopId

---

## Next: ADR Analysis (Stage 3)

Key architectural decisions to document:
1. JSON snapshot vs relational normalization
2. Automatic vs triggered versioning
3. Immutable history design
4. Version numbering strategy

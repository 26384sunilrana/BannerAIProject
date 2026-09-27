namespace BannerService.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.ValueObjects;

public class ApplicationDbContext : DbContext
{
    // Authentication entities
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    // Shop management
    public DbSet<Shop> Shops { get; set; } = null!;

    // Subscription management
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;

    // Admin dashboard
    public DbSet<AdminDashboard> AdminDashboards { get; set; } = null!;
    public DbSet<DashboardMetricSnapshot> DashboardMetricSnapshots { get; set; } = null!;
    public DbSet<AdminDashboardAlert> AdminDashboardAlerts { get; set; } = null!;

    // Shop owner dashboard
    public DbSet<ShopOwnerDashboard> ShopOwnerDashboards { get; set; } = null!;
    public DbSet<ShopDashboardMetricSnapshot> ShopDashboardMetricSnapshots { get; set; } = null!;
    public DbSet<ShopDashboardAlert> ShopDashboardAlerts { get; set; } = null!;

    // Publish workflow
    public DbSet<PublishWorkflow> PublishWorkflows { get; set; } = null!;
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; } = null!;

    // Advertisement management
    public DbSet<Advertisement> Advertisements { get; set; } = null!;
    public DbSet<AdMetricsHistory> AdMetricsHistory { get; set; } = null!;

    // Analytics and reporting
    public DbSet<DashboardReport> DashboardReports { get; set; } = null!;
    public DbSet<AnalyticsEvent> AnalyticsEvents { get; set; } = null!;

    // Banner entities
    public DbSet<Banner> Banners { get; set; } = null!;
    public DbSet<Component> Components { get; set; } = null!;
    public DbSet<BannerVersion> BannerVersions { get; set; } = null!;
    public DbSet<Effect> Effects { get; set; } = null!;
    public DbSet<MediaFile> MediaFiles { get; set; } = null!;
    public DbSet<UploadChunk> UploadChunks { get; set; } = null!;
    public DbSet<Carousel> Carousels { get; set; } = null!;
    public DbSet<CarouselComponent> CarouselComponents { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Shop configuration
        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Address).HasMaxLength(256);
            entity.Property(e => e.City).HasMaxLength(128);
            entity.Property(e => e.State).HasMaxLength(128);
            entity.Property(e => e.Country).HasMaxLength(128);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Website).HasMaxLength(256);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.CreatedByUserId).IsRequired();

            // Hierarchy configuration
            entity.HasOne(e => e.ParentShop)
                .WithMany(e => e.ChildShops)
                .HasForeignKey(e => e.ParentShopId)
                .OnDelete(DeleteBehavior.SetNull);

            // Owner configuration
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            entity.HasIndex(e => e.ParentShopId).HasName("IX_Shops_ParentShopId");
            entity.HasIndex(e => e.City).HasName("IX_Shops_City");
            entity.HasIndex(e => e.Status).HasName("IX_Shops_Status");
            entity.HasIndex(e => e.OwnerUserId).HasName("IX_Shops_OwnerUserId");
        });

        // Banner configuration
        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShopId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.Width).IsRequired();
            entity.Property(e => e.Height).IsRequired();
            entity.Property(e => e.IsPublished).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Indexes for multi-tenant isolation and performance
            entity.HasIndex(e => new { e.ShopId, e.Id }).HasName("IX_Banners_ShopId_Id");
            entity.HasIndex(e => new { e.ShopId, e.CreatedAt }).HasName("IX_Banners_ShopId_CreatedAt").IsDescending(false, true);

            // Navigation to components
            entity.HasMany<Component>().WithOne().HasForeignKey(c => c.BannerId).OnDelete(DeleteBehavior.Cascade);
        });

        // Component configuration
        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BannerId).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.PositionX).IsRequired();
            entity.Property(e => e.PositionY).IsRequired();
            entity.Property(e => e.SizeWidth).IsRequired();
            entity.Property(e => e.SizeHeight).IsRequired();
            entity.Property(e => e.ZIndex).IsRequired();
            entity.Property(e => e.PropertiesJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Indexes for component queries
            entity.HasIndex(e => new { e.BannerId, e.ZIndex }).HasName("IX_Components_BannerId_ZIndex");
            entity.HasIndex(e => e.Type).HasName("IX_Components_ComponentType");

            // Unique constraint on BannerId + ZIndex
            entity.HasIndex(e => new { e.BannerId, e.ZIndex }).IsUnique();
        });

        // BannerVersion configuration
        modelBuilder.Entity<BannerVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BannerId).IsRequired();
            entity.Property(e => e.ShopId).IsRequired();
            entity.Property(e => e.VersionNumber).IsRequired();
            entity.Property(e => e.SnapshotJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
            entity.Property(e => e.ChangeDescription).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // Unique constraint on BannerId + VersionNumber
            entity.HasIndex(e => new { e.BannerId, e.VersionNumber })
                .IsUnique()
                .HasName("UQ_BannerVersions_Number");

            // Query index for listing versions
            entity.HasIndex(e => new { e.BannerId, e.ShopId, e.VersionNumber })
                .HasName("IX_BannerVersions_Query")
                .IsDescending(false, false, true);

            // Foreign key to Banners
            entity.HasOne<Banner>()
                .WithMany()
                .HasForeignKey(e => e.BannerId)
                .OnDelete(DeleteBehavior.Cascade);

        });

        // Effect configuration
        modelBuilder.Entity<Effect>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EffectType).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);

            // Index for component effects query
            entity.HasIndex(e => e.ComponentId).HasName("IX_Effects_ComponentId");

            // Foreign key to Component
            entity.HasOne<Component>()
                .WithMany(c => c.Effects)
                .HasForeignKey(e => e.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Parameters as owned collection
            entity.OwnsOne(e => e.Parameters ?? new Dictionary<string, object>());
        });

        // MediaFile configuration
        modelBuilder.Entity<MediaFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShopId).IsRequired();
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Status).IsRequired();

            entity.HasIndex(e => new { e.ShopId, e.Status, e.CreatedAt })
                .HasName("IX_MediaFiles_Query")
                .IsDescending(false, false, true);

            entity.HasIndex(e => e.StoragePath)
                .IsUnique()
                .HasName("UQ_MediaFiles_StoragePath");
        });

        // UploadChunk configuration
        modelBuilder.Entity<UploadChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MediaFileId).IsRequired();
            entity.Property(e => e.ChecksumMD5).HasMaxLength(32).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).IsRequired();

            entity.HasIndex(e => new { e.MediaFileId, e.ChunkNumber })
                .IsUnique()
                .HasName("UQ_UploadChunks_Unique");

            entity.HasIndex(e => new { e.MediaFileId, e.ChunkNumber })
                .HasName("IX_UploadChunks_Query");

            entity.HasOne<MediaFile>()
                .WithMany()
                .HasForeignKey(e => e.MediaFileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Carousel configuration
        modelBuilder.Entity<Carousel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BannerId).IsRequired();
            entity.Property(e => e.IntervalMs).IsRequired();
            entity.Property(e => e.TransitionDuration).IsRequired();
            entity.Property(e => e.TransitionType).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.BannerId).HasName("IX_Carousels_BannerId");

            entity.HasOne<Banner>()
                .WithMany()
                .HasForeignKey(e => e.BannerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CarouselComponent configuration
        modelBuilder.Entity<CarouselComponent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CarouselId).IsRequired();
            entity.Property(e => e.ComponentId).IsRequired();
            entity.Property(e => e.Order).IsRequired();

            entity.HasIndex(e => new { e.CarouselId, e.Order })
                .HasName("IX_CarouselComponents_Query");

            entity.HasIndex(e => new { e.CarouselId, e.ComponentId })
                .IsUnique()
                .HasName("UQ_CarouselComponents_Unique");

            entity.HasOne<Carousel>()
                .WithMany()
                .HasForeignKey(e => e.CarouselId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Component>()
                .WithMany()
                .HasForeignKey(e => e.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

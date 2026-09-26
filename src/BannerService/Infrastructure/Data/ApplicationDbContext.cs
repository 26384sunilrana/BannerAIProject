namespace BannerService.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.ValueObjects;

public class ApplicationDbContext : DbContext
{
    public DbSet<Banner> Banners { get; set; } = null!;
    public DbSet<Component> Components { get; set; } = null!;
    public DbSet<BannerVersion> BannerVersions { get; set; } = null!;
    public DbSet<Effect> Effects { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

            // Configure value object navigation
            entity.OwnsOne(e => e.Snapshot, snapshot =>
            {
                snapshot.Property(s => s.Name).HasMaxLength(100);
                snapshot.Property(s => s.Description).HasMaxLength(500);
                snapshot.OwnsMany(s => s.Components, component =>
                {
                    component.Property(c => c.PropertiesJson).HasColumnType("NVARCHAR(MAX)");
                });
            });
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
    }
}

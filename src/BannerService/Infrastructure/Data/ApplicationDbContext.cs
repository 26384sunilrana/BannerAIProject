namespace BannerService.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.ValueObjects;

public class ApplicationDbContext : DbContext
{
    public DbSet<Banner> Banners { get; set; } = null!;
    public DbSet<Component> Components { get; set; } = null!;

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
    }
}

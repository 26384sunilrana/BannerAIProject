# Database Schema Fix Plan - Full Implementation

**Objective**: Create complete BannerService database with all 19 tables without cascade delete conflicts

**Current Status**: 
- DbContext configured with all 19 entities
- Some missing FK configurations causing errors
- Need to add Subscription, Invoice FK configurations

---

## Issue Analysis

### Critical Issues Identified:

1. **Shops.ParentShopId (Self-Referential FK)**
   - **Current**: SetNull ✅ (This is correct)
   - **Status**: Should work

2. **Missing Subscription Entity Configuration**
   - **Issue**: Entity registered in DbSet but NO configuration in OnModelCreating
   - **FKs Needed**: Subscription → SubscriptionPlan, Subscription → User, Subscription → Shop

3. **Missing Invoice Entity Configuration**
   - **Issue**: Entity registered but NOT configured
   - **FKs Needed**: Invoice → Subscription, Invoice → User, Invoice → Shop

4. **Potential Cascade Cycles**
   - Banner → Component (Cascade) → Effect (Cascade) is OK (linear)
   - Banner → Carousel (Cascade) → CarouselComponent (Restrict) is OK
   - Component → Effect (Cascade) OK, but Component used in multiple places

---

## Solution Strategy

### Phase 1: Add Missing Entity Configurations

**1. Subscription Configuration**
```csharp
modelBuilder.Entity<Subscription>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.SubscriptionPlanId).IsRequired();
    entity.Property(e => e.UserId).IsRequired();
    entity.Property(e => e.ShopId).IsRequired();
    entity.Property(e => e.StartDate).IsRequired();
    entity.Property(e => e.EndDate).IsRequired();
    entity.Property(e => e.Status).IsRequired();
    entity.Property(e => e.CurrentPrice).HasColumnType("decimal(18,2)");
    entity.Property(e => e.BillingCycle).IsRequired();
    entity.Property(e => e.CreatedAt).IsRequired();
    entity.Property(e => e.UpdatedAt).IsRequired();

    // FKs - All SetNull (subscriptions can exist but parent can be deleted)
    entity.HasOne<SubscriptionPlan>()
        .WithMany()
        .HasForeignKey(e => e.SubscriptionPlanId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne<User>()
        .WithMany()
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne<Shop>()
        .WithMany()
        .HasForeignKey(e => e.ShopId)
        .OnDelete(DeleteBehavior.SetNull);

    // Indexes
    entity.HasIndex(e => new { e.ShopId, e.Status }).HasName("IX_Subscriptions_Shop_Status");
    entity.HasIndex(e => new { e.UserId, e.StartDate }).HasName("IX_Subscriptions_User_StartDate");
});
```

**2. Invoice Configuration**
```csharp
modelBuilder.Entity<Invoice>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.SubscriptionId).IsRequired();
    entity.Property(e => e.UserId).IsRequired();
    entity.Property(e => e.ShopId).IsRequired();
    entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
    entity.Property(e => e.DueDate).IsRequired();
    entity.Property(e => e.Status).IsRequired();
    entity.Property(e => e.CreatedAt).IsRequired();
    entity.Property(e => e.UpdatedAt).IsRequired();

    // FKs - All SetNull (invoices preserved even if parent deleted)
    entity.HasOne<Subscription>()
        .WithMany()
        .HasForeignKey(e => e.SubscriptionId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne<User>()
        .WithMany()
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne<Shop>()
        .WithMany()
        .HasForeignKey(e => e.ShopId)
        .OnDelete(DeleteBehavior.SetNull);

    // Indexes
    entity.HasIndex(e => new { e.ShopId, e.DueDate }).HasName("IX_Invoices_Shop_DueDate");
    entity.HasIndex(e => new { e.UserId, e.Status }).HasName("IX_Invoices_User_Status");
});
```

### Phase 2: Verify All FK Strategies

| Entity | FK | Current | Issue? | Fix |
|--------|-----|---------|--------|-----|
| Component | Banner | Cascade | ✅ | OK - single parent |
| Effect | Component | Cascade | ✅ | OK - linear chain |
| UploadChunk | MediaFile | Cascade | ✅ | OK - chunk cleanup |
| BannerVersion | Banner | Cascade | ✅ | OK - version cleanup |
| Carousel | Banner | Cascade | ✅ | OK - single parent |
| CarouselComponent | Carousel | Restrict | ✅ | OK - prevents orphan |
| CarouselComponent | Component | Restrict | ✅ | OK - prevents orphan |
| State | Country | Restrict | ✅ | OK - master data |
| District | State | Restrict | ✅ | OK - master data |
| Shop | ParentShop | SetNull | ✅ | OK - hierarchy |
| Shop | Country/State/District | SetNull | ✅ | OK - master data |
| Subscription | Plan/User/Shop | SetNull | ⚠️ | NEEDS ADD |
| Invoice | Subscription/User/Shop | SetNull | ⚠️ | NEEDS ADD |
| UserRole | User/Role | Cascade | ✅ | OK - join cleanup |

---

## Implementation Steps

### Step 1: Update ApplicationDbContext
Add Subscription and Invoice configurations to `OnModelCreating()` method

### Step 2: Verify No Shadow Properties
Resolve warnings about `BannerId1`, `UserId1`, `RoleId1` shadow properties by ensuring FKs are properly configured

### Step 3: Create New Migration
```bash
dotnet ef migrations remove  (remove bad migration)
dotnet ef migrations add FixFullSchema -o Infrastructure/Data/Migrations
```

### Step 4: Test Database Update
```bash
dotnet ef database drop --force
dotnet ef database update
```

### Step 5: Verify All Tables Created
```sql
SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'
-- Expected: 20 (19 tables + __EFMigrationsHistory)

SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME
```

---

## Expected Result

**19 Tables Created:**
1. ✅ __EFMigrationsHistory
2. ✅ AnalyticsEvents
3. ✅ Banners
4. ✅ BannerVersions
5. ✅ Carousels
6. ✅ CarouselComponents
7. ✅ Components
8. ✅ Countries
9. ✅ Districts
10. ✅ Effects
11. ✅ Invoices (needs FK config)
12. ✅ MediaFiles
13. ✅ Roles
14. ✅ States
15. ✅ Shops
16. ✅ Subscriptions (needs FK config)
17. ✅ SubscriptionPlans (with owned SubscriptionFeatures)
18. ✅ UploadChunks
19. ✅ UserRoles
20. ✅ Users
21. ✅ RefreshTokens

---

## Cascade Delete Strategy Summary

**SAFE CASCADES** (used):
- Banner → Component → Effect (cleanup chain)
- Banner → BannerVersion (version cleanup)
- Banner → Carousel (carousel cleanup)
- MediaFile → UploadChunk (chunk cleanup)
- User → UserRole (role cleanup)

**SAFE RESTRICT** (used):
- Country ← State (prevents orphan states)
- State ← District (prevents orphan districts)
- Carousel ← CarouselComponent (prevents orphan components)
- Component ← CarouselComponent (prevents orphan components)

**SAFE SET_NULL** (proposed):
- Shop → ParentShop (self-referential, keeps child shops)
- Shop ← Country/State/District (keeps shops if master deleted)
- Subscription ← Plan/User/Shop (keeps subscription records)
- Invoice ← Subscription/User/Shop (keeps financial records)

---

## Testing Checklist

- [ ] All 20 tables created
- [ ] No FK constraint errors
- [ ] No cascade delete cycles
- [ ] Test data can be inserted
- [ ] Foreign key constraints enforced
- [ ] Indexes created successfully
- [ ] Unique constraints working

---

**Next Action**: Implement Step 1 (add Subscription and Invoice configurations)

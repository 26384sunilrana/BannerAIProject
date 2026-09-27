# Bolt 10: Subscription Management - Implementation Plan
## Tiers, Renewal, Auto-Billing, and Feature Entitlements

**Status**: 🚀 IN PROGRESS  
**Date Started**: 2026-09-27  
**Estimated Duration**: 8-10 hours (3 phases)

---

## Overview

Bolt 10 implements **Subscription Management Service** with:
- ✅ 4 Subscription Tiers (Basic, Silver, Gold, Platinum)
- ✅ Feature entitlements per tier
- ✅ Auto-renewal with configurable intervals
- ✅ Billing management (invoices, payment tracking)
- ✅ Plan upgrades/downgrades mid-cycle
- ✅ Grace periods and cancellation
- ✅ Subscription status tracking
- ✅ Proration calculations for mid-cycle changes
- ✅ Renewal reminders and payment failures
- ✅ Subscription analytics per shop

---

## Business Requirements

### Subscription Tiers

| Tier | Monthly Cost | Annual Cost | Features |
|------|-------------|------------|----------|
| **Basic** | $29 | $290 | 5 Banners, 1 Shop, Basic Support |
| **Silver** | $79 | $790 | 25 Banners, 5 Shops, Email Support |
| **Gold** | $199 | $1,990 | Unlimited Banners, 25 Shops, Priority Support |
| **Platinum** | $499 | $4,990 | Everything + API Access, Dedicated Support |

### Subscription Lifecycle

```
New Account
    ↓
[Free Trial] (14 days)
    ↓
Select Tier & Billing Period
    ↓
[Active] ← [Renewal Scheduled]
    ↓
[Renewal Pending] (7 days before)
    ↓
[Payment Processing]
    ├─ Success → [Active]
    └─ Failure → [Payment Failed] → [Suspended] → [Cancelled]
    ↓
[Grace Period] (7 days after expiry)
    ↓
[Expired/Cancelled]
```

### Feature Entitlements

```
Feature | Basic | Silver | Gold | Platinum |
---------|-------|--------|------|----------|
Max Banners | 5 | 25 | Unlimited | Unlimited |
Max Shops | 1 | 5 | 25 | Unlimited |
Max Users | 1 | 3 | 10 | Unlimited |
Max Storage (GB) | 1 | 10 | 100 | 1000 |
API Access | No | No | No | Yes |
Custom Domain | No | No | Yes | Yes |
Advanced Analytics | No | No | Yes | Yes |
Dedicated Support | No | No | No | Yes |
SLA % | 99.0% | 99.5% | 99.9% | 99.99% |
Priority Queue | No | No | Yes | Yes |
```

---

## Architecture & Domain Design

### Domain Model

```csharp
// Subscription Plans
public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } // Basic, Silver, Gold, Platinum
    public string Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int DisplayOrder { get; set; }
    public SubscriptionFeatures Features { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Customer Subscription
public class Subscription
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; } // FK to Shop
    public Guid PlanId { get; set; } // FK to SubscriptionPlan
    public SubscriptionStatus Status { get; set; }
    public BillingPeriod BillingPeriod { get; set; } // Monthly, Annual
    
    // Dates
    public DateTime StartDate { get; set; }
    public DateTime RenewalDate { get; set; }
    public DateTime? CancellationDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    
    // Payment tracking
    public decimal CurrentPrice { get; set; }
    public int? PaymentFailureCount { get; set; }
    public DateTime? LastPaymentAttempt { get; set; }
    public string? PaymentMethodId { get; set; } // Stripe/PayPal ID
    
    // Plan changes
    public Guid? PreviousPlanId { get; set; }
    public DateTime? PlanChangedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Billing Invoice
public class Invoice
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid ShopId { get; set; }
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; }
    public string InvoiceNumber { get; set; } // INV-2026-001
    
    // Dates
    public DateTime IssuedDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    
    // Details
    public string Description { get; set; }
    public string? PaymentReference { get; set; }
    public string? StripeInvoiceId { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

// Enums
public enum SubscriptionStatus
{
    Trial = 1,
    Active = 2,
    RenewalPending = 3,
    PaymentFailed = 4,
    Suspended = 5,
    GracePeriod = 6,
    Expired = 7,
    Cancelled = 8
}

public enum BillingPeriod
{
    Monthly = 1,
    Annual = 2
}

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Overdue = 3,
    Paid = 4,
    Cancelled = 5,
    Refunded = 6
}

public class SubscriptionFeatures
{
    public int MaxBanners { get; set; }
    public int MaxShops { get; set; }
    public int MaxUsers { get; set; }
    public int MaxStorageGB { get; set; }
    public bool ApiAccess { get; set; }
    public bool CustomDomain { get; set; }
    public bool AdvancedAnalytics { get; set; }
    public bool DedicatedSupport { get; set; }
    public decimal SlaPercentage { get; set; }
    public bool PriorityQueue { get; set; }
}
```

---

## Implementation Phases

### Phase 1: Domain Layer (4 hours)
- [ ] Create SubscriptionPlan entity
- [ ] Create Subscription entity
- [ ] Create Invoice entity
- [ ] Define SubscriptionStatus enum
- [ ] Define BillingPeriod enum
- [ ] Define SubscriptionFeatures value object
- [ ] Create ISubscriptionPlanRepository interface
- [ ] Create ISubscriptionRepository interface
- [ ] Create IInvoiceRepository interface
- [ ] Create ISubscriptionService interface
- [ ] Create DTOs for subscription operations
- [ ] Create validators for subscription data
- [ ] Unit tests for domain logic

**Deliverables**: 800+ lines
- Subscription.cs (150 lines)
- SubscriptionPlan.cs (80 lines)
- Invoice.cs (80 lines)
- Enums (50 lines)
- SubscriptionFeatures (60 lines)
- 3 Repository interfaces (100 lines)
- ISubscriptionService interface (50 lines)
- DTOs (150 lines)
- Validators (150 lines)

### Phase 2: Infrastructure & Services (3-4 hours)
- [ ] Implement SubscriptionPlanRepository
- [ ] Implement SubscriptionRepository
- [ ] Implement InvoiceRepository
- [ ] Create database migrations (3 tables)
- [ ] Implement SubscriptionService with business logic
- [ ] Implement RenewalService (renewal scheduling)
- [ ] Implement BillingService (invoice generation)
- [ ] Integration tests for repositories

**Deliverables**: 800+ lines
- 3 Repository implementations (250 lines)
- SubscriptionService (250 lines)
- RenewalService (150 lines)
- BillingService (150 lines)
- 3 Database migrations (150 lines)
- Integration tests (50 lines)

### Phase 3: API & Testing (2-3 hours)
- [ ] Create SubscriptionsController with 8 endpoints
- [ ] Create SubscriptionPlansController with 3 endpoints
- [ ] Create InvoicesController with 4 endpoints
- [ ] Wire up in Program.cs
- [ ] Create Postman collection
- [ ] Write API testing guide

**Deliverables**: 600+ lines
- 2 Controllers (400 lines)
- Postman collections (200 lines)
- Testing guide

---

## Database Schema

### SubscriptionPlans Table

```sql
CREATE TABLE SubscriptionPlans (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(500),
    MonthlyPrice DECIMAL(10, 2) NOT NULL,
    AnnualPrice DECIMAL(10, 2) NOT NULL,
    DisplayOrder INT NOT NULL,
    
    -- Features (JSON column)
    Features NVARCHAR(MAX) NOT NULL,
    
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    INDEX IX_DisplayOrder (DisplayOrder),
    INDEX IX_IsActive (IsActive)
);
```

### Subscriptions Table

```sql
CREATE TABLE Subscriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ShopId UNIQUEIDENTIFIER NOT NULL FK,
    PlanId UNIQUEIDENTIFIER NOT NULL FK,
    Status INT NOT NULL,
    BillingPeriod INT NOT NULL,
    
    StartDate DATETIME2 NOT NULL,
    RenewalDate DATETIME2 NOT NULL,
    CancellationDate DATETIME2,
    TrialEndDate DATETIME2,
    
    CurrentPrice DECIMAL(10, 2) NOT NULL,
    PaymentFailureCount INT DEFAULT 0,
    LastPaymentAttempt DATETIME2,
    PaymentMethodId NVARCHAR(256),
    
    PreviousPlanId UNIQUEIDENTIFIER,
    PlanChangedAt DATETIME2,
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    FK (ShopId) REFERENCES Shops(Id),
    FK (PlanId) REFERENCES SubscriptionPlans(Id),
    
    INDEX IX_ShopId (ShopId),
    INDEX IX_Status (Status),
    INDEX IX_RenewalDate (RenewalDate),
    INDEX IX_ExpiryDate (RenewalDate)
);
```

### Invoices Table

```sql
CREATE TABLE Invoices (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    SubscriptionId UNIQUEIDENTIFIER NOT NULL FK,
    ShopId UNIQUEIDENTIFIER NOT NULL FK,
    Amount DECIMAL(10, 2) NOT NULL,
    Status INT NOT NULL,
    InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
    
    IssuedDate DATETIME2 NOT NULL,
    DueDate DATETIME2 NOT NULL,
    PaidDate DATETIME2,
    
    Description NVARCHAR(500),
    PaymentReference NVARCHAR(256),
    StripeInvoiceId NVARCHAR(256),
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    FK (SubscriptionId) REFERENCES Subscriptions(Id),
    FK (ShopId) REFERENCES Shops(Id),
    
    INDEX IX_ShopId (ShopId),
    INDEX IX_Status (Status),
    INDEX IX_InvoiceNumber (InvoiceNumber),
    INDEX IX_DueDate (DueDate)
);
```

---

## 15 API Endpoints

### SubscriptionPlans Controller (3 endpoints)

1. **GET** `/api/subscription-plans` - List all plans
2. **GET** `/api/subscription-plans/{planId}` - Get plan details
3. **GET** `/api/subscription-plans/{planId}/features` - Get plan features

### Subscriptions Controller (8 endpoints)

1. **POST** `/api/subscriptions/select-plan` - Create/upgrade subscription
2. **GET** `/api/subscriptions/current` - Get current shop subscription
3. **GET** `/api/subscriptions/{subscriptionId}` - Get subscription details
4. **PUT** `/api/subscriptions/{subscriptionId}/billing-period` - Change billing period
5. **PUT** `/api/subscriptions/{subscriptionId}/upgrade` - Upgrade plan
6. **PUT** `/api/subscriptions/{subscriptionId}/downgrade` - Downgrade plan
7. **DELETE** `/api/subscriptions/{subscriptionId}/cancel` - Cancel subscription
8. **GET** `/api/subscriptions/{subscriptionId}/renewal-date` - Check renewal date

### Invoices Controller (4 endpoints)

1. **GET** `/api/invoices` - List invoices for shop
2. **GET** `/api/invoices/{invoiceId}` - Get invoice details
3. **GET** `/api/invoices/{invoiceId}/download` - Download invoice PDF
4. **POST** `/api/invoices/{invoiceId}/retry-payment` - Retry payment

---

## Business Logic

### Subscription Creation
```
1. Validate plan exists
2. Check shop doesn't have active subscription
3. Calculate start/renewal dates
4. Determine if free trial applies (first subscription)
5. Create subscription record
6. Generate first invoice if no trial
7. Send confirmation email
```

### Plan Upgrade
```
1. Validate new plan is higher tier
2. Calculate prorated amount owed/credited
3. Create upgrade record with proration
4. Generate invoice for prorated amount
5. Update subscription plan and price
6. Keep original renewal date
```

### Plan Downgrade
```
1. Validate new plan is lower tier
2. Calculate credit for next cycle
3. Create downgrade record
4. Apply credit to next invoice
5. Effective on next renewal date
6. Keep renewal date unchanged
```

### Auto-Renewal
```
Daily Job (runs at midnight UTC):
1. Find subscriptions renewing tomorrow
2. For each subscription:
   a. Validate payment method exists
   b. Generate invoice
   c. Process payment (Stripe/PayPal)
   d. If success: Update status to Active, schedule next renewal
   e. If failure: Mark as PaymentFailed, send alert
3. Retry failed payments (3 attempts over 7 days)
4. Move to suspended/cancelled if too many failures
```

### Grace Period
```
On Subscription Expiry:
1. Move to GracePeriod status
2. Send cancellation warning email
3. Disable non-basic features
4. If payment succeeds during grace: Return to Active
5. After 7 days: Move to Expired, permanently disable
```

---

## DTOs

### CreateSubscriptionDto
```csharp
{
    PlanId: Guid,
    BillingPeriod: BillingPeriod (1=Monthly, 2=Annual),
    PaymentMethodId?: string,
    TrialDays?: int
}
```

### SubscriptionDto
```csharp
{
    Id: Guid,
    ShopId: Guid,
    PlanName: string,
    Status: SubscriptionStatus,
    BillingPeriod: BillingPeriod,
    CurrentPrice: decimal,
    StartDate: DateTime,
    RenewalDate: DateTime,
    TrialEndDate?: DateTime,
    CancellationDate?: DateTime,
    Features: SubscriptionFeatures
}
```

### SubscriptionPlanDto
```csharp
{
    Id: Guid,
    Name: string,
    Description: string,
    MonthlyPrice: decimal,
    AnnualPrice: decimal,
    Features: SubscriptionFeatures,
    IsPopular: bool
}
```

### InvoiceDto
```csharp
{
    Id: Guid,
    InvoiceNumber: string,
    Amount: decimal,
    Status: InvoiceStatus,
    IssuedDate: DateTime,
    DueDate: DateTime,
    PaidDate?: DateTime,
    Description: string,
    SubscriptionPlanName: string
}
```

---

## Testing Strategy

### Unit Tests (30+ tests)
- Subscription creation validation
- Plan upgrade/downgrade logic
- Proration calculations
- Renewal date calculations
- Grace period logic
- Feature entitlements per plan
- Status transitions
- Billing calculations

### Integration Tests (20+ tests)
- Repository CRUD operations
- Subscription lifecycle
- Invoice generation
- Auto-renewal workflow
- Plan change scenarios
- Payment failure handling

### API Tests (15+ tests)
- Create subscription
- List plans
- Upgrade/downgrade
- Cancel subscription
- Invoice retrieval
- Status transitions
- Authorization checks

### Coverage Target: 90%+

---

## Authorization Matrix

| Operation | Admin | ShopOwner | SalesExec |
|-----------|-------|-----------|-----------|
| View Plans | ✅ All | ✅ All | ✅ All |
| Create Subscription | ✅ All | ✅ Own | ❌ |
| Change Plan | ✅ All | ✅ Own | ❌ |
| Cancel Subscription | ✅ All | ✅ Own | ❌ |
| View Invoices | ✅ All | ✅ Own | ❌ |
| Download Invoice | ✅ All | ✅ Own | ❌ |

---

## Implementation Checklist

### Phase 1: Domain
- [ ] Create SubscriptionPlan entity
- [ ] Create Subscription entity  
- [ ] Create Invoice entity
- [ ] Define all enums
- [ ] Define SubscriptionFeatures
- [ ] Create 3 repository interfaces
- [ ] Create service interfaces
- [ ] Create DTOs
- [ ] Create validators
- [ ] Unit tests (30+)

### Phase 2: Infrastructure
- [ ] Implement 3 repositories
- [ ] Create 3 database migrations
- [ ] Implement SubscriptionService
- [ ] Implement RenewalService
- [ ] Implement BillingService
- [ ] Integration tests (20+)

### Phase 3: API & Testing
- [ ] Create 2 controllers (15 endpoints)
- [ ] Wire up DI in Program.cs
- [ ] Create Postman collections
- [ ] API tests (15+)
- [ ] Testing guide

---

## Key Features

✅ **4 Subscription Tiers**: Basic, Silver, Gold, Platinum  
✅ **Auto-Renewal**: Configurable monthly/annual billing  
✅ **Plan Changes**: Mid-cycle upgrades/downgrades with proration  
✅ **Billing Management**: Invoices, payment tracking, retry logic  
✅ **Grace Periods**: 7 days after expiry with feature degradation  
✅ **Feature Entitlements**: Per-plan limits and features  
✅ **Trial Support**: 14-day free trial for new customers  
✅ **Payment Methods**: Stripe/PayPal integration ready  
✅ **Analytics**: Subscription metrics per shop  
✅ **Audit Trail**: All changes logged with timestamps  

---

## Success Criteria

✅ All 15 endpoints functional and tested  
✅ Subscription lifecycle working end-to-end  
✅ Proration calculations accurate  
✅ Auto-renewal workflow operational  
✅ 90%+ test coverage (65+ tests)  
✅ Database migrations optimized  
✅ Postman collection complete  
✅ Authorization enforced correctly  
✅ Ready for payment integration  
✅ Complete testing and deployment guide  

---

## Estimated Timeline

| Phase | Task | Duration | Complete By |
|-------|------|----------|------------|
| 1 | Domain Layer | 4 hrs | Today +4h |
| 2 | Infrastructure | 3-4 hrs | Today +7-8h |
| 3 | API & Testing | 2-3 hrs | Today +9-11h |
| **Total** | **Subscription Management** | **9-11 hrs** | **Today +9-11h** |

---

## Dependencies

- ✅ Bolt 8: Authentication (User, RBAC)
- ✅ Bolt 9: Shop Management (Shop entity)
- ✅ EF Core with SQL Server
- ✅ xUnit for testing
- ✅ Moq for mocking

---

## Future Enhancements

- [ ] Stripe integration for payment processing
- [ ] PayPal integration
- [ ] Email notifications for renewals/failures
- [ ] Usage-based billing (overages)
- [ ] Bulk subscription management
- [ ] Subscription analytics dashboard
- [ ] Custom billing periods (quarterly, etc)
- [ ] Volume discounts
- [ ] White-label support
- [ ] Multi-currency support

---


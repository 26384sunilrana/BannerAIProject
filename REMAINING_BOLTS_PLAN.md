# BannerAIProject - Remaining Bolts & Features Plan

**Status**: Bolts 1-7 Complete ✅ | Bolts 8-14 Planned

---

## 📋 Original Requirements Coverage

### ✅ Completed (Bolts 1-7)
- Banner CRUD operations
- 4 component types (text, image, video, graphics)
- Z-index management
- Version control (10 versions)
- Effects engine
- Media upload with metadata
- Carousel/rotation
- Editor UI with drag-drop

### ❌ NOT YET IMPLEMENTED (Bolts 8-14)
- User Management (Admin)
- Shop Management & Grouping
- Subscription Management
- Authentication & Authorization (JWT, Roles)
- Admin Dashboard
- Shop Owner Dashboard
- Publish/Approval Workflow
- Advertisement Management
- Reporting & Analytics
- Multi-tenant Architecture
- Local Default Banner
- Multi-login Management

---

## 🔧 Architecture Update Required

### Current Architecture (Bolts 1-7)
```
BannerService (Core)
├── Banner operations
├── Component management
├── Version control
├── Effects
├── Media upload
└── Carousel
```

### Required Additional Services (Bolts 8-14)
```
BannerService (Core)
├── Existing (Bolts 1-7)
└── User/Shop management

AuthenticationService (NEW - Bolt 8)
├── User registration
├── Login/JWT tokens
├── Role management
└── Authorization

SubscriptionService (NEW - Bolt 9)
├── Subscription plans (Basic, Silver, Gold, Platinum)
├── Renewal management
├── Auto-renewal logic
└── Pricing by size/location

AdminService (NEW - Bolt 10)
├── User management
├── Shop management
├── Subscription management
├── Approval workflows
└── Reporting

AnalyticsService (NEW - Bolt 11)
├── Banner usage metrics
├── User analytics
├── Advertisement analytics
└── HIPAA/PHI compliance

AdvertisementService (NEW - Bolt 12)
├── Advertisement management
├── Minor ads (component-based)
├── Major ads (location-based)
├── Popup ads
└── Pricing management

PublishService (NEW - Bolt 13)
├── Approval workflows
├── Publishing logic
├── Scheduled publishing
└── Version management
```

---

## 📊 Remaining Bolts Breakdown

### Bolt 8 - Authentication & Authorization Service
**Duration**: 3-4 days
**Features**:
- User registration
- User login with JWT
- Role-based access control (Admin, Shop Owner, Sales Executive)
- Token refresh
- Email verification
- Password reset
- API endpoint protection

**Database Tables**:
- Users (email, password_hash, role, shop_id, is_active)
- Roles (Admin, ShopOwner, SalesExecutive)
- UserRoles (junction table)
- RefreshTokens

**API Endpoints**:
```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh-token
POST   /api/auth/logout
GET    /api/auth/me
POST   /api/auth/verify-email
POST   /api/auth/reset-password
```

---

### Bolt 9 - Shop Management Service
**Duration**: 3-4 days
**Features**:
- Create/Update/Delete shops
- Shop grouping (Location, City, State, Country hierarchy)
- Multi-shop assignment to users
- Shop profile management
- Default banner storage

**Database Tables**:
- Shops (name, location, city, state, country, banner_size)
- ShopGroups (shop_id, group_id)
- Locations (location_id, country, state, city, area)
- ShopOwners (user_id, shop_id)

**API Endpoints**:
```
POST   /api/shops
GET    /api/shops/{shopId}
PUT    /api/shops/{shopId}
DELETE /api/shops/{shopId}
GET    /api/shops?country=...&state=...&city=...
POST   /api/shops/{shopId}/groups
GET    /api/locations/hierarchy
```

---

### Bolt 10 - Subscription Management Service
**Duration**: 3-4 days
**Features**:
- Subscription tier management (Basic, Silver, Gold, Platinum)
- Pricing based on banner size + location
- Auto-renewal management
- Renewal reminders (SMS/Email)
- Subscription upgrade/downgrade
- Grace period handling (1 week after expiry)
- Login disable on expired

**Database Tables**:
- SubscriptionPlans (name, features, base_price)
- Subscriptions (shop_id, plan_id, start_date, end_date, is_active, auto_renew)
- SubscriptionPricing (plan_id, size_category, location, price)
- RenewalReminders (subscription_id, reminder_date, sent)

**API Endpoints**:
```
GET    /api/subscriptions/plans
POST   /api/shops/{shopId}/subscriptions
GET    /api/shops/{shopId}/subscriptions
PUT    /api/subscriptions/{subscriptionId}
POST   /api/subscriptions/{subscriptionId}/upgrade
POST   /api/subscriptions/{subscriptionId}/renew
GET    /api/subscriptions/{subscriptionId}/status
```

---

### Bolt 11 - Admin Dashboard & User Management
**Duration**: 4-5 days
**Frontend Components**:
- User management page (create, edit, delete users)
- Shop management page (CRUD with hierarchy)
- Subscription management page
- Approval workflow management
- Reporting dashboard

**Backend Features**:
- User CRUD operations
- Role assignment
- Shop assignment to users
- Login limits (max 2 active per shop)
- Approval request management
- Audit logging

**Admin Pages**:
```
/admin
├── Dashboard (overview, stats)
├── Users (management, roles, shops)
├── Shops (management, grouping, locations)
├── Subscriptions (plans, pricing, renewals)
├── Approvals (pending banners, rejections)
├── Reports (usage, revenue, analytics)
└── Settings (system configuration)
```

---

### Bolt 12 - Shop Owner Dashboard
**Duration**: 3-4 days
**Features**:
- View own shops
- Create/manage sales executive logins (max 2 active)
- Manage approval workflow (self or assign to others)
- View subscription status
- View banner analytics
- Manage default banner

**Pages**:
```
/dashboard
├── Overview (stats, current banners)
├── My Shops (list, details)
├── Logins (create sales exec login, manage)
├── Subscriptions (current plan, renewal)
├── Banners (list, create, edit, version history)
├── Analytics (views, engagement)
└── Settings (profile, preferences)
```

---

### Bolt 13 - Publish & Approval Workflow
**Duration**: 3-4 days
**Features**:
- Approval request submission
- Approval by designated users
- Approval rejection with comments
- Schedule publishing
- Auto-publish on scheduled date
- Change approval (publish date change, banner updates)
- Validation (no overlapping hours on same day)

**Database Tables**:
- ApprovalRequests (banner_id, requester_id, status, requested_date)
- ApprovalHistory (request_id, approver_id, decision, comments, timestamp)
- PublishedBanners (banner_id, shop_id, start_date, end_date, version)

**API Endpoints**:
```
POST   /api/banners/{bannerId}/submit-for-approval
GET    /api/approvals/pending
PUT    /api/approvals/{approvalId}/approve
PUT    /api/approvals/{approvalId}/reject
POST   /api/banners/{bannerId}/publish
GET    /api/banners/{bannerId}/publish-schedule
```

---

### Bolt 14 - Advertisement Management Service
**Duration**: 4-5 days
**Features**:
- Minor advertisements (component-based, shop owner charges)
- Major advertisements (location-based, admin manages)
- Popup advertisements
- Advertisement pricing by location/size
- Advertisement schedule management
- Notification system for available ad spaces
- Flexible pricing management

**Database Tables**:
- Advertisements (ad_id, type, content, schedule)
- AdvertisementPrices (location, size, price)
- AdvertisementAssignments (shop_id, ad_id, schedule, price)
- AdvertisementCharges (shop_id, amount, paid, due_date)

**Admin Features**:
```
/admin/advertisements
├── Manage ad campaigns
├── Set pricing by location
├── View ad performance
├── Send notifications
└── Manage charges
```

---

### Bolt 15 - Analytics & Reporting Service
**Duration**: 4-5 days
**Features**:
- Banner impression tracking
- User engagement analytics
- Advertisement ROI tracking
- Revenue reporting
- HIPAA/PHI compliance
- Export reports (PDF, Excel)
- Real-time dashboards

**Reporting Modules**:
```
User Level:
- Banner views
- Component engagement
- Peak hours
- Audience demographics

Admin Level:
- Revenue by shop/location
- Subscription metrics
- Ad performance
- System health

Compliance:
- HIPAA audit logs
- Data encryption
- Access logs
- Retention policies
```

---

## 📈 Complete Project Timeline

| Bolt | Feature | Days | Status |
|------|---------|------|--------|
| 1-3 | Banner Core | ✅ Done | ✅ |
| 4 | Version Control | ✅ Done | ✅ |
| 5-6 | Effects & Media | ✅ Done | ✅ |
| 7 | Editor UI | ✅ Done | ✅ |
| **8** | **Authentication** | **3-4** | ⏳ |
| **9** | **Shop Management** | **3-4** | ⏳ |
| **10** | **Subscription Mgmt** | **3-4** | ⏳ |
| **11** | **Admin Dashboard** | **4-5** | ⏳ |
| **12** | **Shop Owner Dashboard** | **3-4** | ⏳ |
| **13** | **Publish Workflow** | **3-4** | ⏳ |
| **14** | **Advertisement Mgmt** | **4-5** | ⏳ |
| **15** | **Analytics & Reports** | **4-5** | ⏳ |
| **Total Remaining** | | **32-39 days** | |

---

## 🏗️ Data Model Updates Required

### New Tables for Bolts 8-15
```sql
-- Authentication
Users (user_id, email, password_hash, role, shop_id, is_active)
Roles (role_id, name)
UserRoles (user_id, role_id)
RefreshTokens (token_id, user_id, expires_at)

-- Shop Management
Shops (shop_id, name, location, city, state, country, size)
Locations (location_id, country, state, city, area)
ShopOwners (owner_id, user_id, shop_id)

-- Subscriptions
SubscriptionPlans (plan_id, name, tier, features)
Subscriptions (subscription_id, shop_id, plan_id, start_date, end_date, auto_renew)
SubscriptionPricing (pricing_id, plan_id, size, location, price)
RenewalReminders (reminder_id, subscription_id, date_sent)

-- Publishing & Approval
ApprovalRequests (request_id, banner_id, requester_id, status)
ApprovalHistory (history_id, request_id, approver_id, decision)
PublishedBanners (published_id, banner_id, shop_id, start_date, end_date)

-- Advertisements
Advertisements (ad_id, type, content, start_date, end_date)
AdvertisementPrices (price_id, location, size, price_per_month)
AdvertisementAssignments (assignment_id, shop_id, ad_id)
AdvertisementCharges (charge_id, shop_id, amount, due_date)

-- Analytics
BannerImpressions (impression_id, banner_id, timestamp, viewer_id)
UserActivity (activity_id, user_id, action, timestamp)
AdPerformance (performance_id, ad_id, impressions, clicks, conversions)
```

---

## 🎯 Priority Order

### Phase 1 (Critical - Weeks 1-2)
1. **Bolt 8** - Authentication (user login, JWT)
2. **Bolt 9** - Shop Management (CRUD, hierarchy)
3. **Bolt 10** - Subscription (plans, renewal)

### Phase 2 (Important - Weeks 3-4)
4. **Bolt 11** - Admin Dashboard (user mgmt, approvals)
5. **Bolt 12** - Shop Owner Dashboard (banners, logins)

### Phase 3 (Essential - Weeks 5-6)
6. **Bolt 13** - Publish Workflow (approval, scheduling)
7. **Bolt 14** - Advertisement Management (pricing, assignment)

### Phase 4 (Nice to Have - Week 7+)
8. **Bolt 15** - Analytics & Reporting (dashboards, compliance)

---

## 🔐 Security Considerations

### Multi-tenant Isolation
- Filter all queries by shop_id at repository level
- Ensure user can only access their own shop's data
- API authorization on every endpoint

### Authentication
- JWT tokens with short expiry (15 min)
- Refresh tokens with longer expiry (7 days)
- Password hashing (bcrypt)
- Email verification required

### Data Protection
- Encrypt sensitive data in database
- HIPAA/PHI compliance for analytics
- Audit logging for all changes
- Secure password reset flow

### Role-Based Access
```
Admin
├── All system access
├── User management
├── Subscription management
└── Advertisement management

ShopOwner
├── Own shop management
├── User creation (max 2 active)
├── Banner creation & approval
└── Analytics for own shop

SalesExecutive
├── Banner creation
├── Can't approve (needs owner approval)
└── View analytics
```

---

## 📝 Implementation Strategy

### For Each Bolt:
1. **Database Design** - Create migrations
2. **API Services** - Implement business logic
3. **Controllers** - Create endpoints
4. **Frontend Pages** - Build UI components
5. **Tests** - Unit & integration tests
6. **Documentation** - API docs & guides

### Testing Requirements:
- Unit tests for services
- Integration tests for workflows
- E2E tests for critical flows
- Load testing for analytics

### Deployment:
- Docker compose for local testing
- Kubernetes manifests for Azure
- CI/CD pipeline updates
- Database backup strategy

---

## ✅ Next Immediate Steps

1. **Start with Bolt 8** - Authentication Service
   - Create auth database tables
   - Implement JWT token generation
   - Create registration/login endpoints
   - Build login UI page

2. **Continue with Bolt 9** - Shop Management
   - Create shop tables with hierarchy
   - Implement shop CRUD endpoints
   - Build location management

3. **Then Bolt 10** - Subscription Management
   - Create subscription tables
   - Implement renewal logic
   - Build subscription management API

4. **Parallel: Build Admin UI** (Bolts 11-12)
   - Admin dashboard layout
   - Shop owner dashboard
   - User management pages

---

## 🎯 Would You Like Me To Start?

**Choose your priority:**

**Option A**: Start with **Bolt 8 (Authentication)**
- User registration/login system
- JWT token management
- Role-based access control
- ~3-4 days to complete

**Option B**: Start with **Bolts 8+9+10 together** (Full foundation)
- Authentication + Shop + Subscription
- Everything needed for shop owner access
- ~10 days to complete

**Option C**: Create **comprehensive plan first**
- Design all data models
- Plan API architecture
- Review with requirements
- Then start implementation

**Which would you prefer?**

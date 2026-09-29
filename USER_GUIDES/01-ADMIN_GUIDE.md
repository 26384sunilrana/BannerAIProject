# Administrator Guide
## System Management & Monitoring

**Role Level**: System Administrator  
**Access Level**: Full system access  
**Last Updated**: September 29, 2026

---

## Table of Contents
1. [Getting Started](#getting-started)
2. [Admin Dashboard](#admin-dashboard)
3. [User Management](#user-management)
4. [Shop Management](#shop-management)
5. [Subscription & Billing](#subscription--billing)
6. [System Monitoring](#system-monitoring)
7. [Tips & Best Practices](#tips--best-practices)
8. [Troubleshooting](#troubleshooting)

---

## Getting Started

### 1️⃣ Login to Admin Panel

**Steps**:
1. Navigate to: `https://app.bannereditor.app/admin`
2. Enter your **email** and **password**
3. Click **Login**
4. Complete **two-factor authentication** (if enabled)
5. You're redirected to the **Admin Dashboard**

### 2️⃣ First-Time Setup

**Tasks to Complete**:
- [ ] Review system configuration
- [ ] Set up email notifications
- [ ] Configure backup settings
- [ ] Create initial subscription plans
- [ ] Set up admin team members

**Time Needed**: ~30 minutes

---

## Admin Dashboard

### Dashboard Overview

The Admin Dashboard provides a complete view of your system:

```
┌─────────────────────────────────────────────────┐
│           Admin Dashboard                       │
├─────────────────────────────────────────────────┤
│  📊 Key Metrics                                 │
│  • Total Users: 1,234                           │
│  • Active Shops: 156                            │
│  • Banners Created: 5,678                       │
│  • Monthly Revenue: $45,230                     │
│                                                 │
│  🚨 System Alerts (4)                           │
│  ⚠️  High server load (95%)                     │
│  ⚠️  Backup failed yesterday                    │
│  ✓  All systems operational                     │
│                                                 │
│  📈 Recent Activity                             │
│  • 123 new users this week                      │
│  • 2,156 banners published                      │
│  • $12,450 revenue this week                    │
└─────────────────────────────────────────────────┘
```

### Dashboard Sections

#### 1. Key Metrics
- **Total Users**: Count of all registered users
- **Active Shops**: Number of active shops
- **Banners Created**: Total banners in system
- **Monthly Revenue**: Total subscription revenue
- **Active Subscriptions**: Count of active subscriptions

#### 2. System Alerts
**Alert Severity Levels**:
- 🔴 **Critical**: Immediate action required (server down, database error)
- 🟠 **Warning**: Action needed soon (high CPU, low disk space)
- 🔵 **Info**: Informational alerts (backups completed, updates available)

**How to Handle Alerts**:
1. Click on alert to view details
2. Take appropriate action
3. Mark as resolved when complete

#### 3. Recent Activity
- User registrations
- Banner publications
- Subscription changes
- Payment transactions

---

## User Management

### View All Users

**Steps**:
1. Click **Users** in left sidebar
2. See list of all registered users
3. Filter by: Status, Shop, Role, Registration Date

**User List Columns**:
| Column | Description |
|--------|-------------|
| Email | User's email address |
| Name | Full name |
| Role | Admin / Shop Owner / Sales Exec / User |
| Shop | Assigned shop (if applicable) |
| Status | Active / Inactive / Locked |
| Joined | Registration date |
| Actions | Edit / Lock / Delete |

### Create New User

**Steps**:
1. Click **Users** → **Add New User**
2. Fill in the form:
   - **Email**: User's email address
   - **First Name**: First name
   - **Last Name**: Last name
   - **Role**: Select from dropdown
   - **Shop Assignment**: (for non-admin users)
   - **Phone**: Optional

3. Click **Send Invitation**
   - User receives email with activation link
   - User sets their own password
   - Account automatically activated

### Assign Roles to Users

**Available Roles**:

| Role | Access Level | Use Case |
|------|--------------|----------|
| **Admin** | System-wide | System management, monitoring |
| **Shop Owner** | Multiple shops | Manage multiple shops and teams |
| **Sales Executive** | Single shop | Create and manage banners, view analytics |
| **User** | Limited | Create and edit own banners |

**How to Assign Role**:
1. Click **Users** → Find user
2. Click user's email to open detail
3. Click **Edit** button
4. Select new **Role** from dropdown
5. Click **Save Changes**
6. Notification sent to user

### Lock/Unlock User Accounts

**When to Lock Account**:
- User forgets password multiple times (auto-locked after 5 attempts)
- Suspicious activity detected
- User leaves company
- Temporary access restriction needed

**How to Lock Account**:
1. Click **Users** → Find user
2. Click user's email
3. Click **Lock Account** button
4. Confirm action
5. User receives notification
6. User cannot login until unlocked

**How to Unlock Account**:
1. Follow same steps as Lock
2. Click **Unlock Account** button
3. User can login again

### Delete User

**Important**: User deletion is permanent!

**Steps**:
1. Click **Users** → Find user
2. Click **Delete** button
3. Confirm deletion
4. User's account removed from system

**What Happens to User's Data**:
- Banners stay in system (reassigned to original shop)
- User's profile deleted
- Login access removed
- Email address can be reused

---

## Shop Management

### View All Shops

**Steps**:
1. Click **Shops** in left sidebar
2. View list of all active shops
3. Filter by: Owner, Status, Subscription Plan, City

### Create New Shop

**Steps**:
1. Click **Shops** → **Add New Shop**
2. Fill in shop details:
   - **Shop Name**: Name of the shop
   - **Owner Email**: Email of shop owner (must be existing user)
   - **Phone**: Shop phone number
   - **Address**: Complete address
   - **City**: City name
   - **GST/Tax ID**: (optional)
   - **Subscription Plan**: Select plan

3. Click **Create Shop**
4. Shop owner receives notification
5. Shop is activated

### Assign Subscription Plan

**Steps**:
1. Click **Shops** → Select shop
2. Click **Edit Subscription**
3. Select new plan from dropdown
4. Review pricing and features
5. Click **Confirm Change**
6. Subscription updated immediately

### Monitor Shop Performance

**Steps**:
1. Click **Shops** → Select shop
2. Click **Analytics** tab
3. View metrics:
   - Banners created (this month)
   - Banners published
   - Team members
   - Active campaigns
   - Storage usage
   - Subscription renewal date

---

## Subscription & Billing

### Manage Subscription Plans

**Steps**:
1. Click **Subscription Plans** in left sidebar
2. View all subscription plans

**Plan List Columns**:
| Column | Description |
|--------|-------------|
| Plan Name | Name of subscription plan |
| Price | Monthly/Annual price |
| Features | Count of included features |
| Users | Total users on this plan |
| Status | Active / Inactive |
| Actions | Edit / Deactivate |

### Create New Subscription Plan

**Steps**:
1. Click **Subscription Plans** → **Create New Plan**
2. Fill in plan details:
   - **Plan Name**: Name (e.g., "Professional", "Enterprise")
   - **Price**: Monthly price in USD
   - **Billing Cycle**: Monthly / Annual
   - **Description**: What's included

3. Select **Features**:
   - [ ] Drag-drop banner editor
   - [ ] Video component support
   - [ ] Media upload (up to X GB)
   - [ ] Version history (10 versions)
   - [ ] Effects library
   - [ ] Analytics dashboard
   - [ ] Team management (X users)
   - [ ] Custom branding
   - [ ] API access

4. Set **Limits**:
   - Max banners per shop
   - Max media storage
   - Max team members
   - Max API calls/month

5. Click **Create Plan**

### View Invoices

**Steps**:
1. Click **Invoices** in left sidebar
2. See list of all invoices
3. Filter by: Shop, Date, Status, Amount

**Invoice Columns**:
| Column | Description |
|--------|-------------|
| Invoice # | Unique invoice ID |
| Shop | Associated shop |
| Amount | Invoice amount |
| Date | Invoice date |
| Due Date | Payment due date |
| Status | Paid / Pending / Overdue |
| Actions | View / Download PDF / Send |

### Download Invoice

**Steps**:
1. Click **Invoices** → Find invoice
2. Click **Download PDF** icon
3. Invoice saved to computer
4. Or click **Email** to send to customer

---

## System Monitoring

### Monitor Server Health

**Health Check Indicators**:

| Metric | Status | Action |
|--------|--------|--------|
| Server CPU | < 80% ✅ | Monitor if > 80% |
| Memory | < 85% ✅ | Restart if > 90% |
| Database | Connected ✅ | Check if disconnected |
| Disk Space | > 20% ✅ | Archive if < 10% |
| API Response | < 200ms ✅ | Optimize if > 500ms |

**Steps**:
1. Click **System** → **Health Monitor**
2. Review all metrics
3. Click on metric for details
4. Take action if needed

### View System Logs

**Steps**:
1. Click **System** → **Activity Logs**
2. See all system events (logins, errors, changes)
3. Filter by: User, Action, Date, Severity

**Log Levels**:
- 🟢 **Info**: Normal operations
- 🟡 **Warning**: Unexpected but handled
- 🔴 **Error**: Problem occurred
- 🟣 **Debug**: Developer information

### Backup & Recovery

**Automatic Backups**:
- Run daily at 2:00 AM UTC
- Kept for 30 days
- Stored in redundant locations

**Manual Backup**:
1. Click **System** → **Backups**
2. Click **Create Backup Now**
3. Backup starts (takes 5-10 minutes)
4. Notification when complete

**Restore from Backup**:
1. Click **System** → **Backups**
2. Find desired backup date
3. Click **Restore**
4. Confirm restoration
5. System restored to that point
6. **Warning**: This will overwrite current data!

---

## Tips & Best Practices

### 🎯 System Administration

1. **Daily Review**
   - Check alerts first thing in morning
   - Monitor system health
   - Review activity logs

2. **Weekly Tasks**
   - Backup verification
   - User activity review
   - Performance trending

3. **Monthly Tasks**
   - Revenue analysis
   - Plan optimization
   - User growth tracking
   - Security audit

### 👥 User Management

- **Create users in bulk** using CSV import
- **Set up clear password policy** (8+ chars, complexity)
- **Regular access reviews** quarterly
- **Audit super-admin accounts** - keep limited
- **Enable 2FA for all admins** immediately

### 💰 Billing Management

- **Monitor churn rate** - identify cancellation patterns
- **Track MRR (Monthly Recurring Revenue)** - key metric
- **Review failed payments** - coordinate with customers
- **Plan pricing strategy** - based on usage patterns
- **Archive old invoices** annually

### 🔒 Security

- **Change default passwords** immediately
- **Enable audit logging** for all critical actions
- **Regular security reviews** monthly
- **Backup verification** - test restores quarterly
- **Keep system updated** - apply patches promptly

---

## Troubleshooting

### Q: Server responding slowly
**A**: 
1. Check System → Health Monitor
2. If CPU > 90%, consider scaling up
3. Check active user count (Dashboard)
4. May need to optimize database queries

### Q: Backup failed
**A**:
1. Click System → Backups
2. View error details
3. Check disk space (should be > 50GB free)
4. Retry backup manually
5. Contact support if persists

### Q: User locked out
**A**:
1. Go to Users → Find user
2. Click Unlock Account
3. User receives notification
4. User can login again

### Q: Subscription plan not showing
**A**:
1. Check if plan is "Active" status
2. If inactive, click Edit and toggle Active
3. Refresh browser page
4. Plan now available for shop assignment

### Q: Can't delete user
**A**:
1. User may have active subscriptions
2. Cancel subscriptions first
3. Then delete user
4. Or reassign banners and delete user

---

## 📞 Need Help?

- **Email**: admin-support@bannereditor.app
- **Chat**: In-app chat (click ? icon)
- **Phone**: +1-800-BANNER-1
- **Hours**: 24/7 support for critical issues

---

**Next**: Read [Shop Owner Guide](02-SHOP_OWNER_GUIDE.md) to understand shop-level management.

# Admin User Guide - BannerAI Project
**Complete End-to-End Guide for System Administration & Subscription Management**

---

## Table of Contents
1. [Getting Started as Admin](#getting-started-as-admin)
2. [Admin Dashboard Overview](#admin-dashboard-overview)
3. [User Management](#user-management)
4. [Subscription Plan Management](#subscription-plan-management)
5. [System Analytics & Reporting](#system-analytics--reporting)
6. [Common Admin Scenarios](#common-admin-scenarios)
7. [System Settings & Configuration](#system-settings--configuration)
8. [Troubleshooting & Support](#troubleshooting--support)

---

## Getting Started as Admin

### 1. Admin Account Access

**Eligibility**: Only super-admins can access admin panel.

**How to Access**:
1. Log in with your admin credentials
2. Navigate to: `https://bannerai.com/admin`
3. You'll see **Admin Dashboard** (different from user dashboard)
4. Your access level is shown: **Super Admin** 👑

### 2. Admin Navigation Bar

**Top Menu Items**:
- **Dashboard**: System overview and key metrics
- **Users**: User management and account administration
- **Subscriptions**: Plan management and billing oversight
- **Analytics**: System-wide reporting and insights
- **Settings**: System configuration and security
- **Support**: Customer support tickets and issues
- **Reports**: Advanced reporting and data export

### 3. Your Admin Responsibilities

As Admin, you're responsible for:
- ✅ Creating and managing subscription plans
- ✅ Monitoring user accounts and activity
- ✅ Processing billing and payments
- ✅ Responding to support tickets
- ✅ System health and security
- ✅ Generating compliance reports
- ✅ Managing system-wide settings

---

## Admin Dashboard Overview

### Key Metrics Section

**At-a-Glance Numbers** (Updated hourly):
```
Total Users:        1,234
  ├─ Active Users:    987 (80%)
  ├─ Inactive Users:  189 (15%)
  └─ Suspended:        58 (5%)

Total Revenue:      $234,567 (This Month)
  ├─ Monthly Plans:   $178,900 (76%)
  ├─ Annual Plans:    $55,667 (24%)
  └─ Invoices Due:    $8,450

Active Subscriptions: 412
  ├─ Basic Plans:      98
  ├─ Professional:    234
  └─ Enterprise:       80

Support Tickets:     23
  ├─ Urgent:          3 (🔴 RED)
  ├─ Normal:         15 (🟡 YELLOW)
  └─ Low:             5 (🟢 GREEN)
```

### Quick Actions

**Emergency Actions** (Red buttons):
- 🔴 **System Maintenance**: Take system offline (with notice)
- 🔴 **Suspend User**: Immediately suspend a user account
- 🔴 **Force Password Reset**: For security issues

**Common Actions** (Green buttons):
- 🟢 **Create Subscription Plan**: Add new plan tier
- 🟢 **Generate Report**: Export system data
- 🟢 **View Support Queue**: See waiting tickets

### Activity Feed

**Recent Events** (Last 24 hours):
- "User alice@example.com subscribed to Professional plan"
- "Payment received from bob@business.com ($99)"
- "User suspended: suspicious@account.com (fraud detected)"
- "Subscription renewed: charlie@startup.com"
- "Support ticket closed: #5234"

---

## User Management

### Scenario 1: Viewing All Users

**Situation**: You need to get an overview of all registered users.

**Steps**:
1. Go to **Users** section in admin panel
2. You'll see a table with:
   - User Email
   - Full Name
   - Status (Active/Inactive/Suspended)
   - Subscription Plan
   - Created Date
   - Last Login
   - Actions (Edit/Suspend/Delete)

3. **Filtering Options**:
   - Filter by Status: `All / Active / Inactive / Suspended`
   - Filter by Plan: `All / Basic / Professional / Enterprise`
   - Filter by Region: `All / India / USA / Europe / etc`
   - Search by Email or Name

4. **Sorting**:
   - Click column header to sort by:
     - Created date (newest first)
     - Last login (most active first)
     - Plan (by subscription tier)
     - Name (alphabetical)

5. **Viewing Details**:
   - Click on a user row to see full profile:
     - Account information
     - Subscription details
     - Payment history
     - Support tickets
     - Activity log
     - Associated shops and banners

---

### Scenario 2: Handling Account Disputes or Fraud

**Situation**: You receive a support ticket: "Someone hacked my account and created unauthorized shops."

**Steps**:

**Immediate Action**:
1. Go to **Users** → Search for `hacked@example.com`
2. Click on user profile
3. Click **"View Activity Log"**
4. You'll see:
   - Login times and locations
   - Shops created (with timestamps)
   - Banners published
   - Subscription changes
   - IP addresses used

5. **Identify Suspicious Activity**:
   - If login from unusual location → likely hacked
   - If creation times don't match user's timezone → suspicious
   - If multiple rapid actions → automated attack

**Response Actions**:

**Step A: Protect the Account**
1. Click **"Force Password Reset"** button
2. User will receive email: "Your password was reset due to security concerns"
3. User must set new password before accessing account

**Step B: Suspend if Necessary**
1. If very serious: Click **"Suspend Account"**
2. Provide reason: `Account compromised - unauthorized shop creation`
3. Click **"Confirm Suspension"**
4. All activity immediately stops

**Step C: Undo Fraudulent Actions**
1. Go to affected shops
2. Click **"Rollback"** on fraudulent banners
3. Delete any unauthorized shops
4. Send user recovery email with details

**Step D: Follow Up**
1. Reply to support ticket: "We've secured your account and removed unauthorized changes"
2. Advise user to:
   - Use unique, strong password
   - Enable two-factor authentication
   - Review other accounts for similar hacks

---

### Scenario 3: Suspending a User Account

**Situation**: A user is violating terms of service (creating malicious banners).

**Steps**:
1. Go to **Users** → Find the user
2. Click on user profile
3. Click **"More Options"** (three dots)
4. Select **"Suspend Account"**
5. A form appears:
   - **Reason for Suspension**:
     ```
     Select from dropdown:
     - Violation of Terms of Service
     - Non-payment
     - Fraudulent activity
     - Illegal content
     - Other (specify)
     ```
   - **Suspension Duration**:
     - Permanent
     - Temporary (30 days / 60 days / 90 days)
   - **Notification**: Auto-send email to user?
     - Yes (recommended): User gets notice
     - No: Silent suspension (for fraud cases)

6. Click **"Confirm Suspension"**

**What Happens**:
- ✅ User can't log in
- ✅ All active banners are paused
- ✅ Subscriptions are frozen (no charges)
- ✅ All API keys are disabled
- ✅ Support tickets auto-archived

**Reactivating User**:
1. After reason is resolved
2. Go to user profile
3. Click **"Unsuspend Account"**
4. Confirm
5. User can log in again

---

### Scenario 4: Deleting a User Account

**Situation**: User requests full account deletion (GDPR right to be forgotten).

**Steps**:
1. Go to **Users** → Find the user
2. Click on user profile
3. Scroll to **"Danger Zone"** section
4. Click **"Delete Account"**
5. Confirmation dialog:
   ```
   Are you sure you want to delete this account?
   
   This will:
   ✓ Delete all user data
   ✓ Delete all shops and banners
   ✓ Delete payment history (retained for 90 days for compliance)
   ✓ This action cannot be undone
   
   Type email to confirm: [____________________]
   ```
6. Type user's email to confirm
7. Click **"Yes, Delete Account"**

**Data Retention**:
- Personal data: Deleted immediately
- Payment/billing: Retained for 90 days (legal requirement)
- After 90 days: Completely purged from system

---

### Scenario 5: Creating Admin Sub-Accounts

**Situation**: You have too many support tickets and need another admin to help.

**Steps**:
1. Go to **Settings** → **Team**
2. Click **"+ Add Team Member"**
3. Fill in details:
   - Email: `support2@bannerai.com`
   - Full Name: `Sarah Support Manager`
   - Role: Select `Support Admin` (not Super Admin)
   - Permissions: Check boxes for what they can do:
     - ☐ View Users
     - ☐ Suspend Users
     - ☐ View Subscriptions
     - ☐ Create Plans
     - ☐ Handle Support Tickets
     - ☐ Generate Reports
     - ☐ View Analytics

4. Click **"Send Invitation"**
5. Sarah receives email with admin invitation link
6. She clicks link and sets her password
7. She's now a Support Admin with limited permissions

**Permission Levels**:
- **Super Admin**: Full access (you)
- **Support Admin**: Can handle users and support tickets
- **Finance Admin**: Can manage subscriptions and billing
- **Analytics Admin**: Can view and generate reports
- **Viewer**: Read-only access to dashboards

---

## Subscription Plan Management

### Scenario 1: Creating a New Subscription Plan

**Situation**: You want to add a new "Starter" plan below the existing "Basic" plan.

**Steps**:
1. Go to **Subscriptions** → **Plans**
2. Click **"+ Create New Plan"**
3. Fill in Plan Details:

   **Basic Information**:
   - Plan Name: `Starter`
   - Description: `Perfect for small businesses just starting out`
   - Display Order: `1` (appears first in listings)

   **Pricing**:
   - Monthly Price: `$29`
   - Annual Price: `$290` (2.4 months free!)
   - Currency: `USD`
   - Billing Interval: `Monthly / Annual`
   - Trial Period: `14 days` (optional)

   **Capabilities**:
   - Max Shops: `1`
   - Max Banners per Shop: `Unlimited`
   - Max Users per Team: `1`
   - Max Campaigns: `Unlimited`
   - A/B Testing: `No`
   - Advanced Analytics: `No`
   - API Access: `No`
   - Support Level: `Email only`

   **Features Included**:
   - ☑ Create shops and banners
   - ☑ Basic analytics
   - ☑ Email support
   - ☐ Phone support
   - ☐ Priority support
   - ☐ API access

4. Click **"Create Plan"**
5. Plan is immediately available for new subscriptions
6. Existing users see it in their plan comparison

---

### Scenario 2: Updating an Existing Plan

**Situation**: You want to increase the Professional plan's feature limit from 3 to 5 shops.

**Steps**:
1. Go to **Subscriptions** → **Plans**
2. Find "Professional" plan
3. Click **"Edit"**
4. Update the field:
   - Max Shops: `5` (was 3)
5. Click **"Save Changes"**

**What Happens**:
- ✅ New users get 5 shop limit
- ✅ Existing users get upgrade automatically!
- ✅ No email needed (positive change)
- ✅ Change takes effect immediately

**Important**: When you increase features/capacity, always update all existing users (they won't be downgraded).

---

### Scenario 3: Deprecating an Old Plan

**Situation**: You want to retire the "Deluxe" plan (nobody uses it) and move users to "Professional".

**Steps**:
1. Go to **Subscriptions** → **Plans**
2. Find "Deluxe" plan
3. Click **"More Options"** (three dots)
4. Select **"Deprecate Plan"**
5. A dialog appears:
   ```
   Deprecate Plan: Deluxe
   
   Current Subscribers: 2 users
   Recommended Action: Migrate to Professional plan
   
   What should happen:
   ☐ Keep as-is (users keep Deluxe)
   ☑ Auto-migrate to: [Professional ▼]
   ☐ Force downgrade to: [Basic ▼]
   ```
6. Select: "Auto-migrate to Professional"
7. Click **"Send Notification Email"**:
   - "We're upgrading your plan with new features at no extra cost!"
8. Click **"Deprecate"**

**What Happens**:
- ✅ 2 Deluxe users automatically moved to Professional
- ✅ They get extra features (no price change)
- ✅ Deluxe plan removed from plan selection
- ✅ Plan removed from dashboard after 30 days

---

### Scenario 4: Managing Subscription Discounts

**Situation**: You want to run a "50% off" promotion for new users for 3 months.

**Steps**:
1. Go to **Subscriptions** → **Discounts & Promotions**
2. Click **"+ Create Promotional Code"**
3. Fill in details:
   - Promo Code: `LAUNCH50` (uppercase)
   - Discount Type: `Percentage`
   - Discount Amount: `50%`
   - Applicable Plans: 
     - ☑ Basic
     - ☑ Professional
     - ☑ Enterprise
   - Duration: `First 3 months only`
   - Valid From: `October 1, 2024`
   - Valid Until: `December 31, 2024`
   - Max Uses: `Unlimited`
   - Usage Limit per User: `Once`

4. Click **"Create Promo Code"**
5. You'll see:
   - Promo code: `LAUNCH50`
   - Used: `0/Unlimited`
   - Revenue Impact: `-$1,250 estimated per month`

6. **Share the code**:
   - Marketing email: "Use code LAUNCH50 for 50% off"
   - Social media: "Limited time: 50% discount - LAUNCH50"
   - Sales team gets the code to share

7. **Monitor usage**:
   - After 2 weeks: 45 users used it (track in dashboard)
   - Revenue is -$562.50 (50% of normal)

8. **When promotion ends**:
   - Code automatically expires on Dec 31
   - No new users can use it
   - Existing users keep discounted rate for 3 months

---

### Scenario 5: Handling Payment Issues

**Situation**: A customer's subscription failed to renew - payment card was declined.

**Steps**:

**Step 1: Find the User**:
1. Go to **Users** → Search: `customer@email.com`
2. Click on user profile
3. Go to **"Billing & Subscription"** tab

**Step 2: Check Status**:
- Subscription Status: `Failed - Payment Declined`
- Last Renewal Attempt: `Oct 27, 2024 2:15 PM`
- Reason: `Insufficient funds`
- Days Until Suspension: `3 days`

**Step 3: Notify User**:
1. Click **"Send Payment Failed Email"**
   - Email template: "Your subscription payment failed"
   - Includes: Retry link, updated payment method link
   - User gets 3 days to update payment

**Step 4: Manual Processing** (if needed):
1. If user contacts you, you can:
   - Click **"Retry Payment Now"** (charges card again)
   - Or give user more time: Set **"Grace Period: 7 days"**
   - Or **"Apply Manual Override: Credit $99"** (admin discretion)

**Step 5: Resolution**:
- If payment succeeds: Subscription active again
- If payment fails again: Account automatically suspended after grace period
- Send final email: "Your subscription has been suspended due to payment failure"

---

## System Analytics & Reporting

### Scenario 1: Generating Monthly Revenue Report

**Situation**: CEO needs this month's financial report for board meeting.

**Steps**:
1. Go to **Analytics** → **Revenue Reports**
2. Select Date Range: `October 1 - October 31, 2024`
3. Click **"Generate Report"**

**Report Shows**:
```
MONTHLY REVENUE REPORT - October 2024

Total Revenue:                  $234,567

By Plan Type:
  Basic (98 users):             $2,940 (1.3%)
  Professional (234 users):     $23,166 (9.9%)
  Enterprise (80 users):        $208,461 (88.8%)

By Billing Cycle:
  Monthly Billing:              $178,900 (76%)
  Annual Billing:               $55,667 (24%)

By Region:
  India:                        $145,678 (62%)
  USA:                          $67,890 (29%)
  Europe:                       $20,999 (9%)

Invoices:
  Issued:      412
  Paid:        401 (97%)
  Overdue:     8 (2%)
  Disputed:    3 (1%)

Growth Metrics:
  New Subscriptions:    23 (↑ 8% vs last month)
  Cancellations:        3 (↓ 12% vs last month)
  Net MRR Growth:       +$18,450 (↑ 8.5%)
  Churn Rate:           0.7% (↓ 0.3% improvement)
```

4. Click **"Download PDF"** or **"Download Excel"**
5. Share with CEO

---

### Scenario 2: Tracking User Growth

**Situation**: You want to see growth trends over the past year.

**Steps**:
1. Go to **Analytics** → **User Analytics**
2. Select Date Range: `Oct 2023 - Oct 2024` (1 year)
3. View **User Growth Chart**:
   ```
   Users Over Time (Monthly):
   Oct 2023:  234
   Nov 2023:  301 (+28%)
   Dec 2023:  456 (+52%)
   Jan 2024:  589 (+29%)
   ...
   Oct 2024:  1,234 (+12%)
   ```

4. **Key Metrics**:
   - Total Users: 1,234
   - Monthly Growth: 12% (this month)
   - Average Monthly Growth: 18% (last year)
   - Projected Year-End: 1,387 users
   - Doubling Time: 5.6 months

5. **Insights**:
   - Growth is slowing (18% → 12%)
   - Might indicate market saturation or need for marketing boost
   - Recommend: Increase marketing spend in Q1 2025

---

### Scenario 3: Analyzing Support Ticket Trends

**Situation**: You want to understand what customers are struggling with.

**Steps**:
1. Go to **Analytics** → **Support Metrics**
2. View **Ticket Volume by Category**:
   ```
   Total Tickets This Month: 487
   
   By Category:
   Billing & Payments:      178 (37%) - HIGHEST
   Feature Requests:        124 (25%)
   Technical Issues:         89 (18%)
   Account Issues:           62 (13%)
   Other:                    34 (7%)
   ```

3. **Drill Down into Billing Issues**:
   - Most common: "Can't update payment method" (45 tickets)
   - Second: "Unexpected charge" (38 tickets)
   - Action: Improve UI for payment method updates

4. **Drill Down into Technical Issues**:
   - Most common: "Banners not publishing" (32 tickets)
   - Second: "Analytics not loading" (28 tickets)
   - Action: Alert engineering team - possible bug!

5. **Response Time Analysis**:
   - Average response: 2.3 hours
   - First resolution: 4.7 hours
   - Target: <2 hours (you're slightly behind)
   - Action: Hire another support person

---

## Common Admin Scenarios

### Scenario 1: Emergency System Maintenance

**Situation**: You need to apply critical security patch. System must go offline for 1 hour.

**Steps**:
1. Go to **Settings** → **System Maintenance**
2. Click **"Schedule Maintenance"**
3. Fill in details:
   - Title: `Critical Security Patch`
   - Start Time: `2024-10-28 2:00 AM UTC`
   - Estimated Duration: `1 hour`
   - Impact: `Complete system downtime`
   - Auto-notify Users: `Yes`
   - Notification Lead Time: `24 hours`

4. Click **"Schedule"**
5. System automatically sends email to all users:
   - "BannerAI will be under maintenance Oct 28 2-3 AM UTC"
   - "Your subscriptions and data are safe"
   - "Thank you for your patience"

6. **During Maintenance**:
   - Click **"Begin Maintenance"** at scheduled time
   - System goes into maintenance mode
   - Users see: "System under maintenance. Back in ~1 hour"

7. **After Maintenance**:
   - Click **"End Maintenance"**
   - System comes back online
   - Users notified: "System is back online. Thank you for waiting"

---

### Scenario 2: Investigating Suspicious Activity

**Situation**: You notice unusual API usage patterns - looks like DDOS attack.

**Steps**:
1. Go to **Settings** → **Security & Monitoring**
2. Click **"View API Logs"**
3. Filter by:
   - Date: `Last 24 hours`
   - Status: `All`
4. You see:
   - User: `apikey_12345`
   - Requests: 50,000 in last hour (normally 100-200)
   - Status Codes: Mostly `429 Too Many Requests`
   - Source IPs: Multiple IPs from different countries
   - Verdict: **DDOS Attack** 🚨

**Response**:
1. Click **"IP Whitelist"** in Settings
2. Click **"Block Range"**
3. Enter IP range: `1.2.3.0/24`
4. Click **"Blacklist"**
5. Attack traffic from that range is blocked immediately
6. Attack stops! ✅

**Follow-up**:
1. Notify user whose API key was compromised
2. Force them to regenerate API key
3. Review their account for other compromises
4. Send security advisory

---

### Scenario 3: Seasonal Planning - Black Friday Sale

**Situation**: Q4 is coming up. You want to plan for Black Friday.

**Steps**:

**Step 1: Create Black Friday Plan** (30 days before):
1. Go to **Subscriptions** → **Promotional Codes**
2. Create code: `BLACK50` - 50% off everything
3. Valid: Nov 24-28, 2024
4. Max uses: Unlimited
5. Estimate: 100 new signups, $15,000 revenue loss (worth it for growth)

**Step 2: Prepare Marketing Team**:
1. Send them the promo code
2. Share revenue impact estimates
3. Align on marketing channels

**Step 3: Scale Infrastructure** (1 week before):
1. Go to **Settings** → **Infrastructure**
2. Review current capacity
3. Temporarily increase server capacity for peak load
4. Enable CDN caching for fast load times

**Step 4: Prepare Support Team** (launch day):
1. Increase support staff (hire temps if needed)
2. Prepare FAQ document for common questions
3. Set up ticket auto-responses
4. Alert team: "Black Friday rush incoming!"

**Step 5: Monitor During Campaign**:
1. Check **Analytics** → **Real-time Dashboard**
2. Monitor:
   - New signups per hour
   - Server performance
   - Support ticket queue
   - Payment success rate

3. Daily reports:
   - "Day 1: 45 new signups, $2,100 revenue, support queue at 12"
   - "Day 2: 67 new signups, $3,200 revenue, support queue at 28"
   - "Day 3: 89 new signups, $4,100 revenue, HIRE TEMP SUPPORT STAFF"

**Step 6: Post-Campaign Analysis** (Dec 5):
1. Generate full report
2. Analysis:
   - Total new signups: 287 (+340% vs normal month)
   - Promo code used: 245 times
   - Actual revenue: $18,900 (slightly better than expected)
   - Customer acquisition cost: $65
   - Success! 🎉

---

## System Settings & Configuration

### Global Settings Management

**Location**: **Settings** → **Global Configuration**

**Available Settings**:

1. **Email Settings**
   - SMTP Server: `smtp.gmail.com`
   - From Address: `noreply@bannerai.com`
   - Test Email: Click to send test

2. **Payment Processing**
   - Payment Gateway: `Stripe`
   - API Keys: (hidden for security)
   - Supported Currencies: USD, EUR, INR
   - Payment Retry Logic: 3 attempts with 24-hour intervals

3. **Data Retention**
   - User Data: Keep for 90 days after deletion
   - Logs: Keep for 1 year
   - Invoices: Keep permanently (legal requirement)
   - Backup: Daily backups, kept for 30 days

4. **Rate Limiting**
   - API Calls: 1,000 per hour per user
   - Login Attempts: 5 per hour per email
   - File Uploads: 100 MB per file

5. **Integrations**
   - Slack: Connect to receive alerts
   - Google Analytics: Track user behavior
   - Sentry: Monitor errors and crashes

---

## Troubleshooting & Support

### Common Issues for Admins

**Issue 1**: User says they didn't receive password reset email
- **Check**: Go to user profile → Review email address (typo?)
- **Action**: Send manual password reset link from admin panel
- **Verify**: Ask user to check spam folder

**Issue 2**: Payment processing is slow (takes 5 minutes instead of <10 seconds)
- **Check**: **Settings** → **System Health** → Check Stripe status
- **Action**: May be Stripe outage - wait 30 minutes
- **Escalate**: If persists, contact Stripe support

**Issue 3**: High number of support tickets suddenly (100+ queue)
- **Check**: Is there a known bug or outage?
- **Action**: Check recent deployments
- **Response**: Send bulk email: "We're aware of issue X. ETA fix: 2 hours"

**Issue 4**: Database running out of space
- **Alert**: System sends notification when 80% full
- **Action**: Go to **Settings** → **Database** → Click **"Archive Old Data"**
- **Result**: Older data moved to archive storage (saves 40% space typically)

---

## Security Best Practices for Admins

1. **Protect Your Admin Account**:
   - Use very strong password (16+ characters)
   - Enable two-factor authentication (2FA)
   - Use security key if available
   - Never share admin credentials

2. **Regular Audits**:
   - Weekly: Review suspension log
   - Monthly: Review admin activity log
   - Monthly: Check for unusual payment patterns
   - Quarterly: Security audit with team

3. **Data Protection**:
   - Daily backups (automatic)
   - Test restore procedure quarterly
   - Never export user data without encryption
   - Use VPN when accessing admin panel remotely

4. **Incident Response**:
   - Security breach? Contact support immediately
   - Data loss? Restore from backup within 1 hour
   - Malicious user? Suspend immediately, investigate later
   - Payment fraud? Chargeback protection enabled by default

---

## Support & Escalation

### Getting Help
- **Internal Slack**: #admin-help
- **Email**: admin-support@bannerai.com
- **Phone**: +91-9876543210 (24/7 for emergencies)
- **Documentation**: https://admin-docs.bannerai.com

### Escalation Path
- **Level 1**: Your team (support admins)
- **Level 2**: Senior admin (you or colleague)
- **Level 3**: Engineering team (for technical issues)
- **Level 4**: CEO/Board (for policy decisions)

---

**Last Updated**: October 2024
**Version**: 1.0

For the latest admin features and updates, visit: https://admin-docs.bannerai.com

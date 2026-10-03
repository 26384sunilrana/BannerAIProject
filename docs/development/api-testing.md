# Bolt 10: Subscription Management API Testing Guide

## Overview
This guide covers comprehensive testing of the Subscription Management API including subscription management, billing, invoicing, and renewal operations.

---

## Environment Setup

### Prerequisites
- Postman installed or Thunder Client
- API running locally on `http://localhost:5000`
- Valid JWT authentication token for authorized endpoints

### Postman Collection
Import the collections in `postman/` (this folder) into Postman.

### Environment Variables
Configure the following variables in Postman:
```
base_url: http://localhost:5000
access_token: [JWT token from login]
shop_id: [Your shop ID]
subscription_id: [Active subscription ID]
invoice_id: [Invoice ID]
plan_id: [Subscription plan ID]
new_plan_id: [Different plan ID for upgrades]
invoice_number: [Invoice number like INV-2026-000001]
```

---

## API Endpoints

### 1. Subscription Plans

#### Get All Plans
```
GET /api/subscriptions/plans
```
**Response**: List of all active subscription plans with pricing and features

**Test Cases**:
- ✅ Verify response contains at least 4 plans (Basic, Silver, Gold, Platinum)
- ✅ Verify pricing is correctly set
- ✅ Verify features are populated for each plan
- ✅ Verify only active plans are returned

#### Get Plan by ID
```
GET /api/subscriptions/plans/{planId}
```
**Response**: Single plan details

**Test Cases**:
- ✅ Get valid plan returns 200 OK
- ✅ Get invalid plan returns 404 Not Found
- ✅ Verify plan details include all properties

---

### 2. Subscriptions

#### Create Subscription
```
POST /api/subscriptions
Content-Type: application/json

{
  "shopId": "00000000-0000-0000-0000-000000000001",
  "planId": "00000000-0000-0000-0000-000000000001",
  "billingPeriod": 0,
  "paymentMethodId": "pm_123456",
  "trialDays": 14
}
```
**Response**: Created subscription details

**Test Cases**:
- ✅ Create with trial period → Status should be "Trial"
- ✅ Create without trial → Status should be "Active"
- ✅ Verify renewal date is calculated correctly (30 days for monthly, 365 for annual)
- ✅ Verify trial end date is set when trial days provided
- ✅ Shop with existing active subscription → Should return error
- ✅ Invalid plan ID → Should return 404
- ✅ Verify invoice created for non-trial subscriptions
- ✅ Verify initial payment failure count is 0

#### Get Shop Subscription
```
GET /api/subscriptions/{shopId}
Authorization: Bearer {token}
```
**Response**: Current active subscription for shop

**Test Cases**:
- ✅ Get existing subscription returns 200
- ✅ Get non-existent subscription returns 404
- ✅ Verify returned subscription matches shop

#### Upgrade Plan
```
POST /api/subscriptions/{subscriptionId}/upgrade
Content-Type: application/json
Authorization: Bearer {token}

{
  "newPlanId": "00000000-0000-0000-0000-000000000003"
}
```
**Response**: Updated subscription with new plan

**Test Cases**:
- ✅ Upgrade to higher tier → Status remains "Active"
- ✅ Verify proration invoice created
- ✅ Verify previous plan is stored in PreviousPlanId
- ✅ Verify current price updated to new plan price
- ✅ Upgrade to same plan → Should return error
- ✅ Upgrade to lower tier → Should return error (use downgrade instead)
- ✅ Verify PlanChangedAt timestamp updated

#### Downgrade Plan
```
POST /api/subscriptions/{subscriptionId}/downgrade
Content-Type: application/json
Authorization: Bearer {token}

{
  "newPlanId": "00000000-0000-0000-0000-000000000002"
}
```
**Response**: Updated subscription with new plan

**Test Cases**:
- ✅ Downgrade to lower tier → Status remains "Active"
- ✅ Verify price adjusted downward
- ✅ Verify change takes effect on next billing cycle
- ✅ Downgrade to same plan → Should return error
- ✅ Downgrade to higher tier → Should return error (use upgrade instead)

#### Change Billing Period
```
POST /api/subscriptions/{subscriptionId}/change-billing
Content-Type: application/json
Authorization: Bearer {token}

{
  "newPeriod": 1
}
```
**Response**: Updated subscription with new billing period

**Test Cases**:
- ✅ Change from Monthly (0) to Annual (1) → Renewal date extends to 365 days
- ✅ Change from Annual (1) to Monthly (0) → Renewal date reduces to 30 days
- ✅ Verify price adjustment invoice created if price differs
- ✅ Same period → Should succeed (no-op)
- ✅ Verify RenewalDate recalculated from today

#### Cancel Subscription
```
POST /api/subscriptions/{subscriptionId}/cancel
Authorization: Bearer {token}
```
**Response**: Confirmation message

**Test Cases**:
- ✅ Cancel active subscription → Status changes to "Cancelled"
- ✅ Verify CancellationDate is set to current time
- ✅ Cancel already cancelled subscription → Should return error
- ✅ Verify subscription no longer appears in active queries

#### Renew Subscription
```
POST /api/subscriptions/{subscriptionId}/renew
Authorization: Bearer {token}
```
**Response**: Renewed subscription details

**Test Cases**:
- ✅ Renew expiring subscription → Status changes to "Active"
- ✅ Verify RenewalDate extended by billing period
- ✅ Verify PaymentFailureCount reset to 0
- ✅ Verify invoice created
- ✅ Renew already active subscription → Should succeed

#### Get Pending Renewals
```
GET /api/subscriptions/renewals/pending
Authorization: Bearer {token}
```
**Response**: List of subscriptions renewing within 7 days

**Test Cases**:
- ✅ Returns subscriptions with renewal within next 7 days
- ✅ Does not return subscriptions beyond 7 days
- ✅ Does not return cancelled subscriptions
- ✅ Does not return expired subscriptions

---

### 3. Invoices

#### Get Invoice by ID
```
GET /api/invoices/{invoiceId}
Authorization: Bearer {token}
```
**Response**: Invoice details

**Test Cases**:
- ✅ Valid invoice ID returns 200
- ✅ Invalid invoice ID returns 404
- ✅ Verify all invoice properties present

#### Get Invoice by Number
```
GET /api/invoices/number/{invoiceNumber}
Authorization: Bearer {token}
```
**Response**: Invoice details

**Test Cases**:
- ✅ Valid invoice number returns 200
- ✅ Invalid invoice number returns 404
- ✅ Invoice number format: INV-YYYY-000001

#### Get Shop Invoices (Paginated)
```
GET /api/invoices/shop/{shopId}?pageNumber=1&pageSize=10
Authorization: Bearer {token}
```
**Response**: Paginated invoice list

**Test Cases**:
- ✅ Default pagination (page 1, size 10)
- ✅ Valid pagination parameters
- ✅ Invalid pageNumber (0 or negative) returns 400
- ✅ Invalid pageSize (0 or negative) returns 400
- ✅ Out of range pagination returns empty list
- ✅ Verify invoices ordered by IssuedDate descending

#### Get Subscription Invoices
```
GET /api/invoices/subscription/{subscriptionId}
Authorization: Bearer {token}
```
**Response**: All invoices for subscription

**Test Cases**:
- ✅ Returns all invoices for subscription
- ✅ Ordered by IssuedDate descending
- ✅ No invoices for new subscription returns empty list

#### Get Invoices by Status
```
GET /api/invoices/status/{status}
Authorization: Bearer {token}
```
**Response**: Invoices with specified status

**Test Cases**:
- ✅ Status values: Draft, Issued, Overdue, Paid, Cancelled, Refunded
- ✅ Valid status returns invoices
- ✅ Invalid status returns 400
- ✅ Case-insensitive status lookup

#### Get Overdue Invoices
```
GET /api/invoices/overdue
Authorization: Bearer {token}
```
**Response**: Invoices past due date not yet paid

**Test Cases**:
- ✅ Returns invoices with status "Issued" and DueDate < now
- ✅ Does not include paid invoices
- ✅ Empty list if no overdue invoices

#### Get Unpaid Invoices
```
GET /api/invoices/unpaid
Authorization: Bearer {token}
```
**Response**: Invoices awaiting payment

**Test Cases**:
- ✅ Returns invoices with status "Issued" or "Overdue"
- ✅ Does not include paid invoices
- ✅ Ordered by DueDate ascending

#### Create Invoice
```
POST /api/invoices
Content-Type: application/json
Authorization: Bearer {token}

{
  "subscriptionId": "00000000-0000-0000-0000-000000000001",
  "shopId": "00000000-0000-0000-0000-000000000001",
  "amount": 99.99,
  "description": "Monthly subscription renewal"
}
```
**Response**: Invoice created

**Test Cases**:
- ✅ Create invoice with status "Draft"
- ✅ Verify invoice number generated correctly
- ✅ Verify IssuedDate defaults to today
- ✅ Verify DueDate defaults to 30 days from today
- ✅ Invalid amount (negative) returns 400
- ✅ Missing required fields returns 400

#### Issue Invoice
```
POST /api/invoices/{invoiceId}/issue
Authorization: Bearer {token}
```
**Response**: Updated invoice with status "Issued"

**Test Cases**:
- ✅ Issue draft invoice → Status changes to "Issued"
- ✅ Issue already issued invoice → Should return error
- ✅ Verify IssuedDate updated

#### Mark Invoice Paid
```
POST /api/invoices/{invoiceId}/mark-paid
Content-Type: application/json
Authorization: Bearer {token}

{
  "paymentReference": "TXN-123456789"
}
```
**Response**: Updated invoice with status "Paid"

**Test Cases**:
- ✅ Mark issued invoice paid → Status changes to "Paid"
- ✅ Verify PaidDate set to current time
- ✅ Verify PaymentReference stored if provided
- ✅ Already paid invoice → Should return error
- ✅ Verify subscription payment failure count reset
- ✅ Verify subscription status updated to "Active" if in PaymentFailed

#### Cancel Invoice
```
POST /api/invoices/{invoiceId}/cancel
Content-Type: application/json
Authorization: Bearer {token}

{
  "reason": "Customer requested cancellation"
}
```
**Response**: Confirmation message

**Test Cases**:
- ✅ Cancel issued invoice → Status changes to "Cancelled"
- ✅ Cancel overdue invoice → Status changes to "Cancelled"
- ✅ Cannot cancel paid invoice → Returns error
- ✅ Reason appended to description if provided
- ✅ Already cancelled invoice → Returns error

#### Refund Invoice
```
POST /api/invoices/{invoiceId}/refund
Content-Type: application/json
Authorization: Bearer {token}

{
  "amount": 99.99,
  "reason": "Partial refund for service reduction"
}
```
**Response**: Confirmation message

**Test Cases**:
- ✅ Refund paid invoice → Status changes to "Refunded"
- ✅ Amount equals invoice → Full refund
- ✅ Amount less than invoice → Partial refund
- ✅ Amount greater than invoice → Returns error
- ✅ Cannot refund unpaid invoice → Returns error
- ✅ Already refunded invoice → Returns error

#### Retry Payment
```
POST /api/invoices/{invoiceId}/retry-payment
Authorization: Bearer {token}
```
**Response**: Updated invoice with status "Paid"

**Test Cases**:
- ✅ Retry overdue invoice → Status changes to "Paid"
- ✅ Verify PaymentFailureCount reset to 0
- ✅ Verify subscription status updated to "Active"
- ✅ Already paid invoice → Should return error
- ✅ Draft invoice → Should return error

#### Mark Invoice Overdue
```
POST /api/invoices/{invoiceId}/mark-overdue
Authorization: Bearer {token}
```
**Response**: Updated invoice with status "Overdue"

**Test Cases**:
- ✅ Mark issued invoice overdue → Status changes to "Overdue"
- ✅ Already overdue invoice → Returns error
- ✅ Paid invoice → Cannot mark overdue

#### Get Billing Metrics
```
GET /api/invoices/metrics
Authorization: Bearer {token}
```
**Response**: Billing metrics and KPIs

**Test Cases**:
- ✅ Returns total invoice count
- ✅ Returns paid invoice count
- ✅ Returns overdue invoice count
- ✅ Returns draft invoice count
- ✅ Returns monthly and annual revenue
- ✅ Returns MRR and ARR
- ✅ Returns collection rate percentage

---

## Workflow Testing

### Subscription Lifecycle Workflow
1. **Create Subscription** (with 14-day trial)
   - Verify status is "Trial"
   - Verify trial end date is set
   - Verify invoice NOT created for trial

2. **Trial Ends**
   - Verify status changes to "Active" on trial expiration
   - Verify first invoice created

3. **Renew Subscription** (7 days before expiration)
   - Call /renew endpoint
   - Verify status remains "Active"
   - Verify renewal date extended
   - Verify new invoice created

4. **Payment Failure Scenario**
   - Simulate failed payment
   - Verify PaymentFailureCount increments
   - After 3 failures, verify status changes to "GracePeriod"
   - Retry payment via retry-payment endpoint

5. **Plan Upgrade**
   - Upgrade to higher tier
   - Verify proration invoice created
   - Verify price updated
   - Verify new features available

6. **Cancellation**
   - Cancel subscription
   - Verify status is "Cancelled"
   - Verify cancellation date set
   - Verify subscription not returned in active queries

### Billing Period Change Workflow
1. Create subscription with Monthly billing
2. Change to Annual billing
3. Verify renewal date extends to 365 days
4. Verify adjustment invoice created if price differs

### Payment Collection Workflow
1. Create invoice (status: Draft)
2. Issue invoice (status: Issued)
3. Simulate payment
4. Mark as paid (status: Paid)
5. Verify subscription status updated

---

## Error Scenarios

### Validation Errors
- ✅ Missing required fields → 400 Bad Request
- ✅ Invalid enum values → 400 Bad Request
- ✅ Invalid amount (negative) → 400 Bad Request
- ✅ Invalid pagination parameters → 400 Bad Request

### Not Found Errors
- ✅ Non-existent subscription ID → 404 Not Found
- ✅ Non-existent invoice ID → 404 Not Found
- ✅ Non-existent plan ID → 404 Not Found

### State Errors
- ✅ Cancel cancelled subscription → Error
- ✅ Issue issued invoice → Error
- ✅ Refund unpaid invoice → Error
- ✅ Upgrade to same plan → Error
- ✅ Downgrade to higher tier → Error

### Authorization Errors
- ✅ Missing token → 401 Unauthorized
- ✅ Invalid token → 401 Unauthorized
- ✅ Expired token → 401 Unauthorized

---

## Performance Testing

### Load Testing Recommendations
- Get all plans: Should complete in < 100ms
- Get shop invoices: Should complete in < 200ms for 10 items
- Create invoice: Should complete in < 500ms
- Get billing metrics: Should complete in < 1000ms

### Expected Results
- ✅ No n+1 query problems
- ✅ Pagination working efficiently
- ✅ Database indexes being used

---

## Test Data

### Sample Plan IDs
```
Basic: 00000000-0000-0000-0000-000000000001
Silver: 00000000-0000-0000-0000-000000000002
Gold: 00000000-0000-0000-0000-000000000003
Platinum: 00000000-0000-0000-0000-000000000004
```

### Sample Values
- Monthly billing: 0
- Annual billing: 1
- Trial days: 14
- Payment method: pm_123456
- Invoice reason: "Monthly subscription renewal"

---

## Troubleshooting

### Common Issues

**Issue**: Token expired or invalid
- **Solution**: Re-authenticate and get new token from login endpoint

**Issue**: Shop ID not found
- **Solution**: Ensure shop was created first via Shop Management API

**Issue**: Cannot create subscription - shop has active subscription
- **Solution**: Cancel existing subscription first

**Issue**: Invoice payment retry fails
- **Solution**: Verify invoice has not already been paid

---

## Automation Testing

### Test Categories
- **Unit Tests**: 30+ tests for service logic
- **Integration Tests**: 20+ tests for repository operations
- **API Tests**: 50+ tests for endpoint validation

### Coverage Goal
- Target: 90%+ code coverage
- Critical paths: 100% coverage
- Error scenarios: 100% coverage

---

## Conclusion

This testing guide covers all major functionality of the Subscription Management API. Follow these test cases to ensure the system works correctly in development, staging, and production environments.

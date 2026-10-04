# Super Admin Account: Setup and Management

The BannerAI system has a permanent Super Admin account that controls all global administrators. This account belongs to Sunil Rana (26384sunilrana@gmail.com) and cannot be deleted, demoted, or modified except through explicit CLI commands.

## Initial Setup (First Time Only)

### 1. Run Database Migrations
```bash
dotnet ef migrations add AddSuperAdminAndDeletionRequests
dotnet ef database update
```

This:
- Adds the `SuperAdmin` role (ID: "0")
- Creates Sunil Rana's account with a placeholder password
- Creates the `AdminDeletionRequests` table for the approval workflow

### 2. Set Super Admin Password
```bash
dotnet BannerService.dll --create-super-admin YourSecurePassword123
```

**Required**: Password must be at least 10 characters. This is the ONLY way to set/change the Super Admin password.

**Example**:
```bash
dotnet BannerService.dll --create-super-admin Secure@Pass2024 Sunil Rana
```

After this, Sunil can login with email `26384sunilrana@gmail.com` and the password you set.

## How the two roles work

The Super Admin holds **both** `SuperAdmin` and `Admin`. `Admin` is what every administrator screen and endpoint checks, so the owner passes all of them; `SuperAdmin` is the extra role that only the owner has and that guards approving removals. The API start-up (and `--migrate`) puts both roles, the active flag and the System Shop id back in place for 26384sunilrana@gmail.com every time, so the account cannot be left without its rights. After a role change, sign out and in again: roles are read from the sign-in token.

Nobody else can deactivate the owner, request the owner's removal, or reset the owner's two-step sign-in. The owner's password is set only with `--create-super-admin` on the server.

## Admin Hierarchy

### Super Admin (Sunil Rana)
- **Email**: 26384sunilrana@gmail.com
- **Cannot**: Be deactivated, deleted, or have roles changed
- **Can**:
  - Create global administrators
  - Approve/reject deletion requests
  - View all admin accounts and deletion requests
  - All standard admin functions

### Global Admins (Created by Super Admin)
- **Can**:
  - Create other global administrators
  - Request deletion of other admins
  - All standard admin functions
- **Cannot**:
  - Delete the Super Admin
  - Delete themselves
  - Approve their own deletion request

## Creating Global Administrators

### Via CLI
```bash
dotnet BannerService.dll --create-admin admin@example.com SecurePassword123 FirstName LastName
```

### Via Admin Dashboard
1. Login as Super Admin or any Global Admin
2. Go to Admin → Admins
3. Click "Create Admin"
4. Fill in: Email, Password (min 10 chars), First Name, Last Name
5. Click "Create"

The new admin can immediately login with their credentials.

## Admin Deletion Workflow

When you need to remove a global administrator:

### Step 1: Request Deletion
1. Go to Admin → Admins
2. Find the admin to remove
3. Click "Request Deletion"
4. (Optional) Enter a reason
5. Submit

Status: **Pending** - waiting for Super Admin approval

### Step 2: Super Admin Reviews (Sunil Only)
1. Go to Admin → Deletion Requests
2. See all pending requests with:
   - Admin to be deleted
   - Who requested the deletion
   - Reason (if provided)
   - Request date

### Step 3: Super Admin Decides
- **Approve**: Click "Approve" → Admin is marked for deletion
- **Reject**: Click "Reject" → Request is cancelled, admin stays active

### Step 4: What "approve" does
Approving switches the administrator's active flag off and ends all their sessions at once; the record is kept for the audit trail and the request shows as *Completed*. The Super Admin can also use **Remove** on the Admins screen to do this directly, without a request.

**Note**: If a deletion is rejected, you can request again later.

## Protecting Sunil's Account

Several safeguards prevent accidental loss of the Super Admin account:

| Protection | Details |
|---|---|
| Cannot deactivate | Deactivate button is disabled for Super Admin |
| Cannot delete | Deletion request is rejected with "Super Admin cannot be deleted" |
| Cannot change password via UI | Only the CLI command `--create-super-admin` works |
| Cannot remove role | Role is protected in the database |
| Self-protection | Cannot request your own deletion |

## Admin Account Maintenance

### Reset Forgotten Admin Password
Admins cannot reset their own passwords. A Super Admin must create a new account:

```bash
dotnet BannerService.dll --create-admin admin@example.com NewPassword123
```

This creates a new admin with that email (or updates the password if the email already exists).

### Deactivate a Global Admin
1. Go to Admin → Admins
2. Find the admin
3. Click "Deactivate"
4. Confirm

They lose access immediately. Their active login sessions are revoked.

### Reactivate a Deactivated Admin
1. Go to Admin → Admins
2. Show inactive admins
3. Click "Activate"

They can login again.

### Unlock a Locked Admin
If an admin has 5 failed login attempts, their account locks automatically.

1. Go to Admin → Admins
2. Find the admin (shows as "Locked")
3. Click "Unlock"
4. They can try logging in again

### Reset Two-Factor for an Admin
If an admin lost their phone:

1. Go to Admin → Admins
2. Find the admin
3. Click "Reset Two-Step Sign-In"
4. They must set up two-factor again at next login

For the Super Admin specifically:
```bash
dotnet BannerService.dll --reset-two-factor 26384sunilrana@gmail.com
```

## API Endpoints

All endpoints require role: `Admin` (Super Admin or Global Admin). Deletion approval requires Super Admin.

### List All Admins
```
GET /api/admin/admins
```

### Create Global Admin
```
POST /api/admin/users/create-admin
{
  "email": "admin@example.com",
  "password": "SecurePassword123",
  "firstName": "John",
  "lastName": "Doe"
}
```

### Request Admin Deletion
```
POST /api/admin/admins/{adminId}/request-deletion
{
  "reason": "Employee terminated"
}
```

### Get Pending Deletion Requests (Super Admin Only)
```
GET /api/admin/super-admin/deletion-requests
```

### Approve Deletion (Super Admin Only)
```
POST /api/admin/super-admin/deletion-requests/{requestId}/approve
```

### Reject Deletion (Super Admin Only)
```
POST /api/admin/super-admin/deletion-requests/{requestId}/reject
```

## Troubleshooting

### "Super Admin cannot be deleted"
You tried to request deletion of Sunil's account. This is blocked by design. You cannot delete the permanent product owner.

### "There is already a pending deletion request"
An admin can only have one pending deletion request at a time. Wait for the Super Admin to approve or reject it, or reject it yourself.

### "Only the Super Admin can approve admin deletions"
You are a Global Admin trying to approve a deletion. Only Sunil (26384sunilrana@gmail.com) can approve deletions.

### "Password needs at least 10 characters"
The Super Admin password must be strong. Use at least 10 characters with a mix of letters, numbers, and symbols.

### Lost the Super Admin password?
There is no "forgot password" for the Super Admin. You must have access to the server to run:

```bash
dotnet BannerService.dll --create-super-admin NewSecurePassword123
```

If you lost server access, contact your infrastructure team.

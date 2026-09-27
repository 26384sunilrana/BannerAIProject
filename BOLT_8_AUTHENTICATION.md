# Bolt 8 - Authentication & Authorization Service

**Status**: ✅ PHASE 1 COMPLETE  
**Date**: 2026-09-27  
**Estimated Duration**: 3-4 days

---

## 📋 What Was Built

### Domain Layer

#### Entities Created
1. **User.cs** (150 lines)
   - Email, password hash, name, phone
   - Role management with navigation
   - Email verification tokens
   - Password reset tokens
   - Login attempt tracking & lockout logic
   - Methods: LockOut(), UnlockAccount(), VerifyEmail(), SetPasswordResetToken()

2. **Role.cs** (35 lines)
   - Role name and description
   - 3 predefined roles: Admin, ShopOwner, SalesExecutive
   - Default roles list for seeding

3. **UserRole.cs** (15 lines)
   - Junction table for User-Role many-to-many relationship
   - AssignedAt timestamp

4. **RefreshToken.cs** (55 lines)
   - Token generation and tracking
   - Expiration and revocation
   - IP address and user agent logging
   - Isomorphic properties: IsExpired, IsActive

#### Repository Interfaces
1. **IUserRepository.cs**
   - GetByIdAsync, GetByEmailAsync, GetByVerificationTokenAsync
   - GetByPasswordResetTokenAsync, GetByShopIdAsync
   - CreateAsync, UpdateAsync, DeleteAsync
   - ExistsAsync, GetCountByShopAsync, SearchAsync

2. **IRoleRepository.cs**
   - GetByIdAsync, GetByNameAsync, GetAllAsync
   - GetByUserIdAsync, CreateAsync
   - AssignRoleAsync, RemoveRoleAsync, UserHasRoleAsync

#### Services
1. **JwtTokenService.cs** (150 lines)
   - GenerateAccessToken: Creates JWT with claims (Id, Email, Name, ShopId, Roles)
   - GenerateRefreshToken: Creates secure random token
   - GetPrincipalFromExpiredToken: Extracts claims from expired token
   - ValidateToken: Full token validation with signature check
   - Configurable expiration via appsettings

2. **PasswordHashService.cs** (90 lines)
   - HashPassword: PBKDF2 with SHA256, 10,000 iterations
   - VerifyPassword: Constant-time comparison
   - Salt: 128 bits, Key: 256 bits
   - Secure against rainbow tables and timing attacks

### Application Layer

#### Services
**AuthenticationService.cs** (280 lines)

Methods Implemented:
1. **RegisterAsync(RegisterDto)**
   - Validates email/password format
   - Checks for existing account
   - Hashes password
   - Creates user with default role (SalesExecutive)
   - Generates access & refresh tokens
   - Returns AuthTokenDto

2. **LoginAsync(LoginDto)**
   - Validates credentials
   - Checks account status (active, not locked)
   - Verifies password hash
   - Records failed login attempts
   - Locks account after 5 failed attempts
   - Generates JWT & refresh token

3. **RefreshTokenAsync(string, string)**
   - Validates refresh token exists and is active
   - Verifies user exists
   - Revokes old token
   - Generates new access & refresh tokens
   - Preserves IP/UserAgent info

4. **RevokeTokenAsync(string, string)**
   - Marks token as revoked
   - Used for logout

5. **VerifyEmailAsync(string, string)**
   - Validates token hasn't expired
   - Marks email as verified
   - Clears verification token

6. **RequestPasswordResetAsync(string)**
   - Generates reset token (1 hour expiry)
   - Returns success regardless (security best practice)

7. **ResetPasswordAsync(string, string, string)**
   - Validates reset token
   - Hashes new password
   - Clears token

#### DTOs Created
- **RegisterDto**: Email, password, name, shop
- **LoginDto**: Email, password, IP, userAgent
- **RefreshTokenDto**: Refresh token
- **AuthTokenDto**: Access token, refresh token, expiry
- **UserDto**: User info with roles
- **ChangePasswordDto**: Current & new password
- **ResetPasswordDto**: Token & new password
- **VerifyEmailDto**: Verification token
- **RequestPasswordResetDto**: Email

### Presentation Layer

#### AuthenticationController.cs (180 lines)

**Endpoints Implemented**:

```
POST   /api/authentication/register
       - Public endpoint
       - Input: RegisterDto
       - Output: { message, tokens }
       - Status: 400 for validation errors

POST   /api/authentication/login
       - Public endpoint
       - Input: LoginDto
       - Output: { message, tokens }
       - Status: 401 for invalid credentials

POST   /api/authentication/refresh-token
       - Authorized endpoint
       - Input: RefreshTokenDto
       - Output: { message, tokens }
       - Status: 401 for invalid token

POST   /api/authentication/logout
       - Authorized endpoint
       - Input: RefreshTokenDto
       - Revokes refresh token

GET    /api/authentication/me
       - Authorized endpoint
       - Returns current user info with roles

POST   /api/authentication/verify-email
       - Authorized endpoint
       - Input: VerifyEmailDto
       - Verifies email address

POST   /api/authentication/forgot-password
       - Public endpoint
       - Input: RequestPasswordResetDto
       - Sends reset email (TODO: implement email service)

POST   /api/authentication/reset-password
       - Public endpoint
       - Input: ResetPasswordDto
       - Resets password with token

GET    /api/authentication/health
       - Public endpoint
       - Health check
```

---

## 🗄️ Database Schema (To Be Implemented)

### Tables to Create (Migrations)

```sql
-- Users table
CREATE TABLE Users (
    Id NVARCHAR(36) PRIMARY KEY,
    Email NVARCHAR(256) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    FirstName NVARCHAR(256),
    LastName NVARCHAR(256),
    PhoneNumber NVARCHAR(20),
    ShopId NVARCHAR(36),
    IsActive BIT NOT NULL DEFAULT 1,
    EmailVerified BIT NOT NULL DEFAULT 0,
    VerificationToken NVARCHAR(MAX),
    VerificationTokenExpires DATETIME2,
    PasswordResetToken NVARCHAR(MAX),
    PasswordResetExpires DATETIME2,
    LoginAttempts INT DEFAULT 0,
    LastLoginAttempt DATETIME2,
    IsLockedOut BIT DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (ShopId) REFERENCES Shops(Id)
);

-- Roles table
CREATE TABLE Roles (
    Id NVARCHAR(36) PRIMARY KEY,
    Name NVARCHAR(256) UNIQUE NOT NULL,
    Description NVARCHAR(MAX),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL
);

-- UserRoles junction table
CREATE TABLE UserRoles (
    UserId NVARCHAR(36) NOT NULL,
    RoleId NVARCHAR(36) NOT NULL,
    AssignedAt DATETIME2 NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

-- RefreshTokens table
CREATE TABLE RefreshTokens (
    Id NVARCHAR(36) PRIMARY KEY,
    UserId NVARCHAR(36) NOT NULL,
    Token NVARCHAR(MAX) NOT NULL UNIQUE,
    JwtId NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    RevokedAt DATETIME2,
    ReplacedByToken NVARCHAR(MAX),
    ReplacementJwtId NVARCHAR(MAX),
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(MAX),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_RefreshTokens_Token (Token),
    INDEX IX_RefreshTokens_UserId (UserId)
);

-- Seed default roles
INSERT INTO Roles VALUES ('1', 'Admin', 'System administrator', 1, GETUTCDATE());
INSERT INTO Roles VALUES ('2', 'ShopOwner', 'Shop owner', 1, GETUTCDATE());
INSERT INTO Roles VALUES ('3', 'SalesExecutive', 'Sales executive', 1, GETUTCDATE());
```

---

## ⚙️ Configuration Required

### appsettings.json
```json
{
  "Jwt": {
    "Key": "your-secret-key-minimum-32-characters-long-for-security",
    "Issuer": "BannerService",
    "Audience": "BannerServiceUI",
    "ExpiresInMinutes": 15
  },
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "FromAddress": "noreply@bannerservice.com",
    "FromName": "Banner Service"
  }
}
```

### Program.cs Registrations (To Be Added)
```csharp
// Add authentication services
services.AddScoped<IAuthenticationService, AuthenticationService>();
services.AddScoped<IPasswordHashService, PasswordHashService>();
services.AddScoped<IJwtTokenService, JwtTokenService>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IRoleRepository, RoleRepository>();

// Add JWT authentication
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(configuration["Jwt:Key"])
            ),
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });

app.UseAuthentication();
app.UseAuthorization();
```

---

## 🧪 Tests to Create (Next Step)

- [ ] UserRepository unit tests
- [ ] RoleRepository unit tests
- [ ] PasswordHashService tests
- [ ] JwtTokenService tests
- [ ] AuthenticationService tests
  - [ ] Register validation
  - [ ] Login with correct credentials
  - [ ] Login with wrong credentials
  - [ ] Account lockout after 5 attempts
  - [ ] Token refresh with valid token
  - [ ] Token refresh with invalid token
  - [ ] Password reset flow
- [ ] AuthenticationController integration tests

---

## 📋 Still To Implement (Phase 2)

### Infrastructure Layer
- [ ] UserRepository.cs (EF Core implementation)
- [ ] RoleRepository.cs (EF Core implementation)
- [ ] RefreshTokenRepository.cs (EF Core implementation)
- [ ] Database migrations

### Email Service
- [ ] EmailService for sending verification & reset emails
- [ ] Email templates

### Middleware
- [ ] JWT Bearer middleware configuration
- [ ] Authorization middleware for role-based access

### Security
- [ ] Rate limiting for login attempts
- [ ] API key management (if needed)
- [ ] CORS configuration for frontend

---

## 📝 API Usage Examples

### Register
```bash
POST /api/authentication/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe",
  "shopId": "shop-123"
}

Response:
{
  "message": "Registration successful",
  "tokens": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "base64-encoded-token",
    "expiresIn": 900,
    "tokenType": "Bearer"
  }
}
```

### Login
```bash
POST /api/authentication/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!"
}

Response:
{
  "message": "Login successful",
  "tokens": { ... }
}
```

### Use Access Token
```bash
GET /api/authentication/me
Authorization: Bearer eyJhbGc...

Response:
{
  "userId": "user-id",
  "email": "user@example.com",
  "name": "John Doe",
  "shopId": "shop-123",
  "roles": ["SalesExecutive"]
}
```

### Refresh Token
```bash
POST /api/authentication/refresh-token
Authorization: Bearer eyJhbGc... (expired token)
Content-Type: application/json

{
  "refreshToken": "base64-encoded-token"
}

Response:
{
  "message": "Token refreshed successfully",
  "tokens": { ... }
}
```

---

## ✅ Checklist for Completion

Phase 1 (Current):
- [x] Domain entities created
- [x] Repository interfaces designed
- [x] Services implemented
- [x] DTOs created
- [x] Controller endpoints created

Phase 2 (Next):
- [ ] EF Core repositories implemented
- [ ] Database migrations created
- [ ] Tests written (unit & integration)
- [ ] Email service implemented
- [ ] JWT middleware configured
- [ ] Role-based authorization implemented

---

## 🚀 Next Steps

1. **Create EF Core Repositories** (UserRepository, RoleRepository, RefreshTokenRepository)
2. **Create Database Migrations** for users, roles, refresh tokens tables
3. **Write Unit Tests** for all services
4. **Create Email Service** for verification & reset emails
5. **Configure JWT Middleware** in Program.cs
6. **Test All Endpoints** with Postman/Swagger
7. **Integrate with Frontend** (Bolt 7 update)

---

## 📊 Files Created

- Domain/Entities/User.cs (150 lines)
- Domain/Entities/Role.cs (35 lines)
- Domain/Entities/UserRole.cs (15 lines)
- Domain/Entities/RefreshToken.cs (55 lines)
- Domain/Interfaces/IUserRepository.cs
- Domain/Interfaces/IRoleRepository.cs
- Domain/Services/JwtTokenService.cs (150 lines)
- Domain/Services/PasswordHashService.cs (90 lines)
- Application/Services/AuthenticationService.cs (280 lines)
- Application/DTOs/AuthenticationDtos.cs (170 lines)
- Presentation/Controllers/AuthenticationController.cs (180 lines)

**Total: 11 files, 1,120+ lines of code**

---

## 🎯 Status: READY FOR NEXT PHASE

All domain, application, and presentation layer code complete.
Ready to implement EF Core repositories and tests.

Next: **Bolt 8 Phase 2 - Implementation & Testing**

# Coding Standards

## Overview

Comprehensive coding standards for a polyglot full-stack project: TypeScript/Next.js frontend with strict type safety, and C#/ASP.NET Core backend with clean architecture. Standards ensure consistency across AI-generated and human-written code, maintainability, and code quality.

---

## Frontend - Next.js + TypeScript

### Code Formatting

**Tool**: Prettier
**Key Settings**:
- Print width: 80-100 characters
- Tab width: 2 spaces
- Single quotes: true
- Trailing commas: es5
- Semi-colons: true

**Enforcement**: 
- Pre-commit hook (Husky) runs Prettier on staged files
- CI pipeline enforces formatting
- IDE integration: Prettier on save

**Configuration**: `.prettierrc` in project root

---

### Linting

**Tool**: ESLint
**Base Config**: `@typescript-eslint/strict`
**tsconfig**: `strict: true` enabled
**Strictness**: Strict

**Key Rules**:
- `no-any`: error (use `unknown` instead)
- `explicit-function-return-types`: error (clarity on function contracts)
- `no-unused-vars`: error (clean code)
- `@typescript-eslint/explicit-member-accessibility`: error (intentional visibility)
- `no-console`: warn (avoid console.log in production)
- `prefer-const`: error (immutability by default)

**Enforcement**: 
- Pre-commit hook (Husky) runs ESLint on staged files
- CI pipeline blocks merge if linting fails

**Configuration**: `.eslintrc.json` with TypeScript parser

---

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Variables | camelCase | `bannerConfig`, `isPublished` |
| Functions | camelCase | `saveBanner`, `publishContent` |
| Classes | PascalCase | `BannerService`, `AssetManager` |
| React Components | PascalCase | `BannerEditor`, `AssetLibrary` |
| React Hooks | camelCase with `use` | `useBanner`, `useAssets`, `usePublish` |
| Interfaces | PascalCase | `IBannerConfig`, `IAsset` |
| Types | PascalCase | `BannerId`, `PublishEvent` |
| Constants | UPPER_SNAKE_CASE | `MAX_BANNER_SIZE`, `DEFAULT_TIMEOUT` |
| Enums | PascalCase | `PublishStatus`, `AssetType` |

**File Naming**:
- Components: PascalCase (e.g., `BannerEditor.tsx`)
- Utilities: kebab-case (e.g., `banner-utils.ts`)
- Hooks: kebab-case (e.g., `use-banner.ts`)
- Tests: Same as source file + `.test.ts` (e.g., `BannerEditor.test.tsx`)

---

### File Organization

**Pattern**: Feature-Based

**Structure**:
```
src/
  app/                          # Next.js app directory
    api/                        # API routes
    layout.tsx
    page.tsx
  features/
    banner-editor/
      components/
        BannerCanvas.tsx
        ElementToolbox.tsx
      hooks/
        useBannerState.ts
        useCanvasInteraction.ts
      services/
        banner-api.ts
      types.ts
      index.ts
    asset-library/
      components/
        AssetUploader.tsx
        AssetGallery.tsx
      hooks/
        useAssets.ts
      services/
        asset-api.ts
      types.ts
      index.ts
    publishing/
      components/
        PublishDialog.tsx
      hooks/
        usePublish.ts
      services/
        publish-api.ts
      types.ts
      index.ts
  shared/
    components/
      Button.tsx
      Modal.tsx
    hooks/
      useAuth.ts
      useNotification.ts
    utils/
      date-utils.ts
      validation-utils.ts
    types/
      common.ts
      api.ts
  styles/
    globals.css
    variables.css
```

**Conventions**:
- Tests: Co-located with source files (`*.test.tsx` or `*.test.ts`)
- Types: Co-located in `types.ts` within feature folder
- Index files: Re-export public API of feature
- No deep nesting: Max 3 levels

---

### Testing Strategy

**Framework**: Jest (or Vitest for faster iteration)
**Additional**: React Testing Library for component testing
**Coverage Target**: 80% overall, 100% on critical paths

**Test Types**:

| Type | Tool | When to Use |
|------|------|-------------|
| Unit | Jest | Functions, utilities, logic |
| Component | React Testing Library | React components, user interactions |
| E2E | Playwright | Full user flows (banner creation → publish) |
| Integration | Jest + Testing Library | Multiple components working together |

**Conventions**:
- Test naming: `it('should [action] when [condition]')`
- Test structure: Arrange-Act-Assert
- Mock strategy: Mock API calls, keep component logic in tests
- Test data: Use factories for realistic data

**Critical paths (100% coverage)**:
- Banner CRUD operations (create, read, update, delete)
- Publishing and version control
- Asset management (upload, delete, reference)

**Non-critical paths (80% coverage)**:
- UI components and styling
- Error edge cases
- Loading states

**Configuration**: `jest.config.js`, `setupTests.ts` in project root

---

### Error Handling

**Pattern**: Try/Catch with Custom Error Classes

**Custom Errors**:
```typescript
class BannerValidationError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'BannerValidationError';
  }
}

class PublishError extends Error { }
class AssetUploadError extends Error { }
```

**API Error Handling**:
```typescript
try {
  const result = await api.saveBanner(config);
  return result;
} catch (error) {
  if (error instanceof BannerValidationError) {
    showUserNotification('Invalid banner configuration');
  } else {
    showUserNotification('Unexpected error. Please try again.');
    logger.error('saveBanner failed', { error });
  }
  throw error;
}
```

**Rules**:
- Use descriptive error names
- Include error context when logging
- Show user-friendly messages for known errors
- Log full error details for debugging

---

### Logging

**Tool**: Winston or Pino (structured logging)
**Format**: JSON (for searchability)

**Levels**:

| Level | Usage | Example |
|-------|-------|---------|
| error | Unexpected failures | API call failed, validation error |
| warn | Unexpected but handled | Deprecated API, fallback used |
| info | Significant events | Banner created, published, deleted |
| debug | Detailed technical info | State changes, API request details |

**Rules**:
- Always log: Errors, authentication events, publish events
- Never log: Passwords, API keys, PII, auth tokens
- Include context: Feature name, user action, request ID
- Log level: info for business events, error for failures

**Local Development**: Logs to `.log` files in project directory
**Production (Azure)**: Migrate to Azure Application Insights

---

## Backend - ASP.NET Core + C#

### Code Formatting

**Tool**: EditorConfig + `dotnet format`
**Key Settings**:
- Indent size: 4 spaces
- Use spaces: true
- Newline preference: Unix
- End of line: LF
- Trim trailing whitespace: true

**Enforcement**:
- Pre-commit hook runs `dotnet format --verify-no-changes`
- CI pipeline enforces formatting
- IDE integration: Format on save

**Configuration**: `.editorconfig` in project root

---

### Linting & Code Analysis

**Tool**: Roslyn Analyzers + StyleCop Analyzers
**Strictness**: Strict

**Key Rules**:
- **Roslyn**:
  - `CS0162`: Unreachable code detection
  - `CS0219`: Unused variable detection
  - `CA1000`: Do not declare static members on generic types
  
- **StyleCop**:
  - `SA1600`: Elements must be documented (public API)
  - `SA1309`: Field names must not begin with underscore (use backing fields)
  - `SA1101`: this prefix must be used for all internal references
  - `SA1028`: Code should not contain trailing whitespace

**Enforcement**:
- Pre-commit hook runs `dotnet build` with analyzers
- CI pipeline treats analyzer warnings as errors
- Build fails if code analysis finds violations

**Configuration**: `.editorconfig` and project file (`PropertyGroup` settings)

---

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Public classes | PascalCase | `BannerService`, `PublishProcessor` |
| Public methods | PascalCase | `SaveBanner()`, `GetBannerById()` |
| Public properties | PascalCase | `BannerId`, `IsPublished` |
| Private/internal members | camelCase with leading underscore | `_bannerRepository`, `_logger` |
| Private methods | camelCase | `validateBanner()` |
| Constants | PascalCase | `MaxBannerSize` |
| Interfaces | PascalCase with `I` prefix | `IBannerRepository`, `IPublishService` |
| Parameters | camelCase | `bannerId`, `publishConfig` |
| Local variables | camelCase | `bannerConfig`, `isValid` |
| Enums | PascalCase | `PublishStatus`, `AssetType` |
| Enum values | PascalCase | `Active`, `Pending` |

**File Naming**:
- Classes: PascalCase matching class name (e.g., `BannerService.cs`)
- Interfaces: PascalCase with `I` prefix (e.g., `IBannerRepository.cs`)
- Tests: Source name + `.Tests.cs` (e.g., `BannerServiceTests.cs`)

---

### File Organization

**Pattern**: Clean Architecture (Layered)

**Structure per Microservice**:
```
BannerService/
  src/
    Presentation/
      Controllers/
        BannersController.cs
      Middleware/
        GlobalExceptionHandlingMiddleware.cs
    Application/
      Services/
        BannerService.cs
      DTOs/
        CreateBannerDto.cs
        BannerResponseDto.cs
      Validators/
        CreateBannerValidator.cs
      Interfaces/
        IBannerService.cs
    Domain/
      Entities/
        Banner.cs
        Asset.cs
      ValueObjects/
        BannerId.cs
      Enums/
        PublishStatus.cs
      Interfaces/
        IBannerRepository.cs
    Infrastructure/
      Data/
        ApplicationDbContext.cs
        Configurations/
          BannerEntityConfiguration.cs
      Repositories/
        BannerRepository.cs
      UnitOfWork.cs
  tests/
    Application.Tests/
      BannerServiceTests.cs
    Infrastructure.Tests/
      BannerRepositoryTests.cs
```

**Conventions**:
- Clear separation of concerns (Presentation, Application, Domain, Infrastructure)
- Domain entities in Domain layer
- Business logic in Application layer
- Data access in Infrastructure layer
- Controllers thin, delegating to services
- Tests mirror source structure

---

### Testing Strategy

**Framework**: xUnit
**Mocking**: Moq
**Coverage Target**: 80% overall, 100% on critical business logic

**Test Types**:

| Type | Tool | When to Use |
|------|------|-------------|
| Unit | xUnit + Moq | Services, repositories, business logic |
| Integration | xUnit + real DB (SQLite) | Repository behavior, data access |
| E2E | Playwright | Full API flow (create → publish → version) |

**Conventions**:
- Test naming: `[MethodName]_[Scenario]_[ExpectedResult]`
  - Example: `SaveBanner_WithValidConfig_ReturnsSuccess`
- Test structure: Arrange-Act-Assert
- Mock repositories in service tests
- Use in-memory SQLite for integration tests
- Test data: Use builders/fixtures for complex objects

**Critical paths (100% coverage)**:
- BannerService CRUD methods
- PublishService publish logic
- VersionControl tracking logic
- Asset management

**Configuration**: `xunit.runner.json` in test project root

---

### Error Handling

**Pattern**: Custom Exceptions + Global Exception Handler Middleware

**Custom Exceptions**:
```csharp
public class BannerValidationException : Exception
{
    public BannerValidationException(string message) : base(message) { }
}

public class BannerNotFoundException : Exception
{
    public BannerNotFoundException(string message) : base(message) { }
}

public class PublishException : Exception
{
    public PublishException(string message) : base(message) { }
}
```

**Global Exception Handler Middleware**:
```csharp
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

Middleware catches exceptions, logs them, and returns standardized error response:
```json
{
  "error": {
    "code": "BANNER_VALIDATION_ERROR",
    "message": "Invalid banner configuration",
    "details": { }
  }
}
```

**Rules**:
- Throw domain-specific exceptions in services
- Middleware translates exceptions to HTTP responses
- Always log full exception details
- Return user-friendly error messages
- Include error codes for API clients

---

### Logging

**Tool**: Serilog (structured logging)
**Format**: JSON (structured)

**Levels**:

| Level | Usage | Example |
|-------|-------|---------|
| Error | Failures that need attention | Exception, validation failed |
| Warning | Unexpected but handled | Deprecated method, fallback used |
| Information | Significant business events | Banner created, published, deleted |
| Debug | Technical details | Method entry/exit, state changes |

**Configuration**:
```csharp
builder.Host.UseSerilog((context, config) =>
    config
        .Enrich.WithProperty("ServiceName", "BannerService")
        .WriteTo.File("logs/banner-.log", rollingInterval: RollingInterval.Day)
        .WriteTo.Console()
);
```

**Rules**:
- Always log: Exceptions, business events (create, publish, delete)
- Never log: Passwords, API keys, personal data, auth tokens
- Include context: UserId, BannerId, operation name
- Structured properties for filtering/searching

**Local Development**: Logs to `logs/` directory (daily files)
**Production (Azure)**: Migrate to Azure Application Insights

---

## Cross-Cutting Standards

### Git & Version Control
- Feature branches: `feature/{feature-name}`
- Bug fixes: `fix/{bug-name}`
- Commit messages: Descriptive, present tense (e.g., "Add banner versioning feature")
- Code review: All PRs require review before merge

### API Communication
- Frontend → Backend: HTTP REST with JSON
- Authentication: Bearer tokens (JWT)
- Error format: Standardized error responses (see error handling)

### Shared Patterns
- Both frontend and backend log to Azure Application Insights in production
- Both use structured, searchable logging formats
- Both maintain 80% code coverage minimum
- Both use strict type checking (TypeScript `strict: true`, C# nullable reference types)

---

## Migration Strategy

### From Local to Azure
- **Logging**: Winston/Pino (frontend) and Serilog (backend) write to files locally → Azure Application Insights in cloud
- **Database**: MSSQL (local) → Azure SQL Server (cloud) via Terraform
- **Containers**: Docker images stored in Azure Container Registry (ACR)
- **Orchestration**: Local Docker Compose → Azure Kubernetes Service (AKS)

### Gradual Adoption
- Start with these standards from day one
- Enforce via pre-commit hooks (prevents non-compliant code)
- Add Azure integrations when deployed to cloud

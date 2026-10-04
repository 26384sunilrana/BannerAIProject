using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using BannerService.Infrastructure.Data;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;
using BannerService.Infrastructure.Repositories;
using BannerService.Infrastructure.Storage;
using BannerService.Application.Services;
using BannerService.Presentation.Middleware;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.WithProperty("ServiceName", "BannerService")
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine("logs", "banner-.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
// No connection string ships in the repository: it comes from appsettings.Development.json locally, and from
// ConnectionStrings__DefaultConnection (a secret) everywhere else
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not set. Provide it as the ConnectionStrings__DefaultConnection environment variable or in user secrets.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IShopContextAccessor, ShopContextAccessor>();

// Authentication repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAdminDeletionRequestRepository, AdminDeletionRequestRepository>();

// Address master data repositories
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<ILocationService, LocationService>();

// Shop management repositories & services
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<IShopService, ShopService>();

// Authentication services
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IJwtTokenService>(sp =>
{
    return new JwtTokenService(builder.Configuration);
});
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Subscription services
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<RenewalService>();
builder.Services.AddScoped<BillingService>();

// Admin dashboard services
builder.Services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Shop owner dashboard services
builder.Services.AddScoped<IShopOwnerDashboardRepository, ShopOwnerDashboardRepository>();
builder.Services.AddScoped<IShopOwnerDashboardService, ShopOwnerDashboardService>();

// Publish workflow services
builder.Services.AddScoped<IPublishWorkflowRepository, PublishWorkflowRepository>();
builder.Services.AddScoped<IPublishWorkflowService, PublishWorkflowService>();

// Advertisement services
builder.Services.AddScoped<IAdvertisementRepository, AdvertisementRepository>();
builder.Services.AddScoped<IAdvertisementService, AdvertisementService>();

// Analytics services
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Banner repositories
builder.Services.AddScoped<IBannerRepository, BannerRepository>();
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();
builder.Services.AddScoped<IBannerVersionRepository, BannerVersionRepository>();
builder.Services.AddScoped<IMediaFileRepository, MediaFileRepository>();
builder.Services.AddScoped<IUploadChunkRepository, UploadChunkRepository>();
builder.Services.AddScoped<ICarouselRepository, CarouselRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Banner services
builder.Services.AddScoped<ComponentValidationService>();
builder.Services.AddScoped<BannerService.Domain.Services.EffectValidator>();
builder.Services.AddScoped<BannerService.Domain.Services.LayerManagementService>();
builder.Services.AddScoped<BannerService.Domain.Services.BannerScheduleService>();
builder.Services.AddScoped<BannerService.Application.Services.BannerScheduleAppService>();
builder.Services.AddScoped<BannerService.Application.Services.ShopTeamService>();
builder.Services.AddScoped<BannerService.Application.Services.UserAdminService>();
builder.Services.AddScoped<BannerService.Application.Services.ShopMembershipService>();
builder.Services.AddScoped<BannerService.Application.Services.AccountService>();
builder.Services.AddScoped<BannerService.Application.Services.ShopSettingsService>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<ISubscriptionNotificationRepository, SubscriptionNotificationRepository>();
builder.Services.AddScoped<ISmsSender, BannerService.Infrastructure.Services.LoggingSmsSender>();
builder.Services.AddScoped<IPaymentGateway, BannerService.Infrastructure.Services.NoPaymentGateway>();
builder.Services.AddScoped<BannerService.Application.Services.SubscriptionLifecycleService>();
builder.Services.AddHostedService<BannerService.Infrastructure.Background.SubscriptionLifecycleWorker>();
builder.Services.AddScoped<IVersionControlService, VersionControlService>();
builder.Services.AddScoped<IEffectService, EffectService>();
builder.Services.AddScoped<IMediaUploadService, MediaUploadService>();
builder.Services.AddScoped<MediaLibraryService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<TwoFactorService>();
builder.Services.AddScoped<IScreenRepository, ScreenRepository>();
builder.Services.AddScoped<ScreenService>();
builder.Services.AddScoped<IShopAdRepository, ShopAdRepository>();
builder.Services.AddScoped<IShopTakeoverRepository, ShopTakeoverRepository>();
builder.Services.AddScoped<ShopTakeoverService>();
builder.Services.AddScoped<IAdRateRepository, AdRateRepository>();
builder.Services.AddScoped<AdRateService>();
builder.Services.AddScoped<AdStatementService>();
builder.Services.AddScoped<AdReportService>();
builder.Services.AddScoped<ShopAdService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<MediaCleanupService>();
builder.Services.AddHostedService<BannerService.Infrastructure.Background.MediaCleanupWorker>();
builder.Services.AddScoped<ICarouselService, CarouselService>();
builder.Services.AddScoped<MetadataExtractionService>();
builder.Services.AddScoped<IStorageProvider, LocalStorageProvider>();
builder.Services.AddSingleton<BannerService.Infrastructure.Security.IRefreshCookie, BannerService.Infrastructure.Security.RefreshCookie>();
builder.Services.AddSingleton<BannerService.Infrastructure.Security.IMediaUrlSigner, BannerService.Infrastructure.Security.MediaUrlSigner>();
builder.Services.AddScoped<IBannerService, BannerService.Application.Services.BannerService>();

builder.Services.AddControllers(options => options.Filters.Add<BannerService.Presentation.Security.TenantAccessFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        // Cors:AllowedOrigins lists the web app origins. When it is empty, development and test allow the local
        // machine (localhost, any port); production allows none (same-origin through the ingress).
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        // The refresh cookie travels with these requests, so a named origin (never any origin) and AllowCredentials are required.
        if (origins.Length > 0)
            policyBuilder.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        else if (!builder.Environment.IsProduction())
            policyBuilder
                .SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && (uri.IsLoopback || uri.Host == "localhost"))
                .AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});

// Authentication (JWT bearer token validation)
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey))
    throw new InvalidOperationException("Jwt:SecretKey is not set. Provide it as the Jwt__SecretKey environment variable or in user secrets (32 or more characters).");
// The shipped placeholder must never sign production tokens: supply Jwt__SecretKey from a secret store
if (builder.Environment.IsProduction() && (jwtSecretKey.Length < 32 || jwtSecretKey.Contains("change-this") || jwtSecretKey.Contains("change-in-production")))
    throw new InvalidOperationException("Jwt:SecretKey must be set to a strong secret (at least 32 characters) in production");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BannerAIProject";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BannerAIProjectUsers";

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Limits on how fast one caller can use the API (see RateLimiting:* in configuration)
builder.Services.AddAppRateLimiting(builder.Configuration);

// Health: /health/live (the process is up), /health/ready (database, media folder, background jobs); metrics at /metrics; the watchdog tells administrators about trouble
builder.Services.AddHealthChecks()
    .AddCheck<BannerService.Infrastructure.Monitoring.DatabaseHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<BannerService.Infrastructure.Monitoring.StorageHealthCheck>("storage", tags: new[] { "ready" })
    .AddCheck<BannerService.Infrastructure.Monitoring.JobsHealthCheck>("jobs", tags: new[] { "ready" });
builder.Services.AddHostedService<BannerService.Infrastructure.Monitoring.OpsWatchdog>();

// Keys for field-level encryption. Production should keep them in a protected store (Azure Blob + Key Vault);
// see Security:KeyDirectory and Security:EncryptionEnabled in configuration.
if (builder.Configuration.GetValue("Security:EncryptionEnabled", true))
{
    var keyDirectory = builder.Configuration["Security:KeyDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "keys");
    builder.Services.AddDataProtection()
        .SetApplicationName("BannerAIProject")
        .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
}

// Administrators must use two-step sign-in in production unless this is set otherwise (Security:RequireTwoFactorForAdmins)
if (builder.Configuration["Security:RequireTwoFactorForAdmins"] == null)
    builder.Configuration["Security:RequireTwoFactorForAdmins"] = builder.Environment.IsProduction() ? "true" : "false";

var app = builder.Build();

if (builder.Configuration.GetValue("Security:EncryptionEnabled", true))
    BannerService.Infrastructure.Security.FieldEncryption.Configure(
        app.Services.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>());

// Apply database migrations and seed data.
//   dotnet BannerService.dll --migrate     applies them and exits (run as a Kubernetes Job / deployment step)
//   Database:MigrateOnStartup=false        skips them at start-up (when a Job owns migrations)
var migrateOnly = args.Contains("--migrate");
if (migrateOnly || app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.Migrate();
        Log.Information("Database migrations applied successfully");

        // Seed master data
        await BannerService.Infrastructure.Data.DatabaseSeeder.SeedAsync(dbContext);
        Log.Information("Database seeded with master data successfully");

        // countries, states and subscribed shops that predate the unique identifiers get theirs
        var backfilled = await scope.ServiceProvider.GetRequiredService<BannerService.Domain.Interfaces.ILocationService>().BackfillUniqueIdsAsync();
        if (backfilled > 0)
            Log.Information("Gave unique identifiers to {Count} existing rows", backfilled);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error applying database migrations or seeding");

        // Outside development a half-migrated API must not start serving: crash so the orchestrator restarts and alerts
        if (migrateOnly || app.Environment.IsProduction())
            throw;
    }
}

if (migrateOnly)
{
    Log.CloseAndFlush();
    return;
}

// dotnet BannerService.dll --create-admin <email> <password> [first name] [last name]
//   makes (or repairs) the first administrator login: sign-up only makes shop owners, so a new installation has no administrator until this is run.
//   Run again with a new password to reset an administrator's password.
var adminIndex = Array.IndexOf(args, "--create-admin");
if (adminIndex >= 0)
{
    using var scope = app.Services.CreateScope();
    var message = await BannerService.Infrastructure.Data.AdminBootstrap.RunAsync(
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
        scope.ServiceProvider.GetRequiredService<BannerService.Domain.Services.IPasswordHashService>(),
        args.Skip(adminIndex + 1).TakeWhile(a => !a.StartsWith("--")).ToArray());
    Log.Information("{Message}", message);
    Log.CloseAndFlush();
    return;
}

// dotnet BannerService.dll --create-super-admin <password> [first name] [last name]
//   creates or updates the permanent Super Admin account (Sunil Rana). Auto-seeded during migration with a placeholder password.
//   Run this to set a real password.
var superAdminIndex = Array.IndexOf(args, "--create-super-admin");
if (superAdminIndex >= 0)
{
    using var scope = app.Services.CreateScope();
    var message = await BannerService.Infrastructure.Data.SuperAdminBootstrap.RunAsync(
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
        scope.ServiceProvider.GetRequiredService<BannerService.Domain.Services.IPasswordHashService>(),
        args.Skip(superAdminIndex + 1).TakeWhile(a => !a.StartsWith("--")).ToArray());
    Log.Information("{Message}", message);
    Log.CloseAndFlush();
    return;
}

// dotnet BannerService.dll --reset-two-factor <email>
//   switches two-step sign-in off for someone who lost their phone and recovery codes (for the last administrator, when no other administrator can do it)
var resetIndex = Array.IndexOf(args, "--reset-two-factor");
if (resetIndex >= 0)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var target = (args.Length > resetIndex + 1 ? args[resetIndex + 1] : string.Empty).Trim().ToLowerInvariant();
    var person = await context.Users.FirstOrDefaultAsync(u => u.Email == target);
    if (person == null)
        Log.Error("No login with that e-mail address.");
    else
    {
        person.TwoFactorEnabled = false;
        person.TwoFactorSecret = null;
        person.TwoFactorRecoveryHashes = null;
        person.TwoFactorLastStep = 0;
        await context.SaveChangesAsync();
        Log.Information("Two-step sign-in was switched off for {Email}. They set it up again at the next sign-in.", target);
    }
    Log.CloseAndFlush();
    return;
}

// dotnet BannerService.dll --encrypt-existing   encrypts personal values saved before field encryption was on, then exits
if (args.Contains("--encrypt-existing"))
{
    using var scope = app.Services.CreateScope();
    var count = await new BannerService.Infrastructure.Security.EncryptionBackfill(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()).RunAsync();
    Log.Information("Encrypted {Count} stored values", count);
    Log.CloseAndFlush();
    return;
}

// Middleware pipeline

// Behind an ingress or load balancer the caller's address arrives in X-Forwarded-For. Trust it only when told to (Proxy:TrustForwardedHeaders=true),
// because anyone could otherwise send a made-up header and slip past the limits and the audit log.
if (app.Configuration.GetValue("Proxy:TrustForwardedHeaders", false))
{
    var forwarded = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
    };
    forwarded.KnownNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
}

// Headers that make browsers treat the API's answers carefully: no guessing of content types, no framing, no referrer, and nothing
// from an API call kept in a shared cache (media downloads are signed links and set their own caching)
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        if (context.Request.Path.StartsWithSegments("/api") && !headers.ContainsKey("Cache-Control") && !context.Request.Path.Value!.Contains("/download"))
            headers["Cache-Control"] = "no-store";
        return Task.CompletedTask;
    });
    await next();
});
if (app.Environment.IsProduction())
    app.UseHsts();
if (!app.Environment.IsProduction() || app.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Behind an ingress that terminates TLS, set Security:RequireHttpsRedirect=false
if (app.Configuration.GetValue("Security:RequireHttpsRedirect", true))
    app.UseHttpsRedirection();
app.UseCors();
app.UseHttpMetrics(); // request counts and durations for /metrics

// Custom middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();

// Authentication must run before ShopContextMiddleware, which reads the validated token's claims
app.UseAuthentication();
app.UseMiddleware<ScreenRestrictionMiddleware>(); // a screen token may only read what a screen shows
app.UseRateLimiter(); // after authentication, so a signed-in caller is limited by user and not by address
app.UseMiddleware<TwoFactorSetupMiddleware>();
app.UseMiddleware<ShopContextMiddleware>();
app.UseAuthorization();

app.MapControllers();

// Liveness and readiness for the orchestrator. Ready answers 503 when the database or the media folder fails; a late background job only degrades.
static Task WriteHealth(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
    {
        status = report.Status.ToString().ToLowerInvariant(),
        checks = report.Entries.ToDictionary(e => e.Key, e => new { status = e.Value.Status.ToString().ToLowerInvariant(), detail = e.Value.Description }),
    }));
}
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteHealth });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = WriteHealth });

// /metrics: on by default outside production; in production only when Metrics:Token is set (then it must be sent as "Authorization: Bearer <token>")
BannerService.Infrastructure.Monitoring.BusinessMetrics.Register(app.Services.GetRequiredService<IServiceScopeFactory>());
var metricsToken = app.Configuration["Metrics:Token"];
var metricsOn = app.Configuration.GetValue("Metrics:Enabled", !app.Environment.IsProduction() || !string.IsNullOrEmpty(metricsToken));
if (metricsOn)
{
    app.MapMetrics("/metrics").AddEndpointFilter(async (invocation, next) =>
    {
        if (string.IsNullOrEmpty(metricsToken)) return await next(invocation);
        var header = invocation.HttpContext.Request.Headers.Authorization.ToString();
        var sent = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..] : string.Empty;
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(sent), System.Text.Encoding.UTF8.GetBytes(metricsToken))
            ? await next(invocation)
            : Results.Unauthorized();
    });
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Enable WebApplicationFactory<Program> for integration testing
public partial class Program { }

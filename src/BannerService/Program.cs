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
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IShopContextAccessor, ShopContextAccessor>();

// Authentication repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Authentication services
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IJwtTokenService>(sp =>
{
    var jwtSettings = new JwtTokenService.JwtSettings
    {
        SecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-secret-key-change-in-production",
        Issuer = builder.Configuration["Jwt:Issuer"] ?? "BannerAIProject",
        Audience = builder.Configuration["Jwt:Audience"] ?? "BannerAIProjectUsers",
        AccessTokenExpirationMinutes = int.Parse(builder.Configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15"),
        RefreshTokenExpirationDays = int.Parse(builder.Configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")
    };
    return new JwtTokenService(jwtSettings);
});
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

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
builder.Services.AddScoped<IVersionControlService, VersionControlService>();
builder.Services.AddScoped<IEffectService, EffectService>();
builder.Services.AddScoped<IMediaUploadService, MediaUploadService>();
builder.Services.AddScoped<ICarouselService, CarouselService>();
builder.Services.AddScoped<MetadataExtractionService>();
builder.Services.AddScoped<IStorageProvider, LocalStorageProvider>();
builder.Services.AddScoped<IBannerService, BannerService.Application.Services.BannerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Authentication (JWT bearer token validation)
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-secret-key-change-in-production";
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

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.Migrate();
        Log.Information("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error applying database migrations");
    }
}

// Middleware pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors();

// Custom middleware
app.UseMiddleware<ShopContextMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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

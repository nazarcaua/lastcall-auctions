using LastCallMotorAuctions.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LastCallMotorAuctions.API.Middleware;
using LastCallMotorAuctions.API.Hubs;
using LastCallMotorAuctions.API.Services;
using Microsoft.AspNetCore.Identity;
using LastCallMotorAuctions.API.Models;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Database
// =======================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =======================
// MVC + Views (REQUIRED)
// =======================
builder.Services.AddControllersWithViews();

// =======================
// OpenAPI / Swagger
// =======================
builder.Services.AddOpenApi();

// =======================
// CORS
// =======================
var allowedOrigins = builder.Configuration
    .GetSection("CORS:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// =======================
// Identity (User + Roles)
// =======================
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    // Password rules
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // User rules
    options.User.RequireUniqueEmail = true;

    // Lockout (optional)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();


// =======================
// JWT + Cookie Authentication
// =======================
var jwtKey = builder.Configuration["JWT:Key"]
    ?? throw new InvalidOperationException("JWT Key is not configured");

var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "LastCallMotorAuctions";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "LastCallMotorAuctions";

builder.Services.AddAuthentication(options =>
{
    // Use cookies as default for MVC views
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        ),
        ClockSkew = TimeSpan.Zero
    };
});

// =======================
// Authorization Policies
// =======================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BuyerOnly", policy => policy.RequireRole("Buyer"));
    options.AddPolicy("SellerOnly", policy => policy.RequireRole("Seller"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("BuyerOrSeller", policy => policy.RequireRole("Buyer", "Seller"));
});

// =======================
// SignalR
// =======================
builder.Services.AddSignalR();

// =======================
// Services
// =======================
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IAuctionService, AuctionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IListingService, ListingService>();

// =======================
// Health Checks
// =======================
builder.Services.AddHealthChecks()
    .AddCheck("self", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is healthy"))
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
        name: "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "sqlserver" }
    );

var app = builder.Build();

// =======================
// Development tools
// =======================
if (app.Environment.IsDevelopment())
{
    // Show detailed exception page during development to aid debugging
    app.UseDeveloperExceptionPage();

    app.MapOpenApi();

    // Move OpenAPI redirect off the root URL so MVC Home page can load
    app.MapGet("/docs", () => Results.Redirect("/openapi/v1.json"))
        .ExcludeFromDescription();
}


app.UseHttpsRedirection();

// =======================
// Global Error Handling
// =======================
app.UseMiddleware<ErrorHandlingMiddleware>();

// =======================
// Static Files (CSS / JS)
// =======================
app.UseStaticFiles();

app.UseRouting();

// =======================
// CORS
// =======================
app.UseCors("AllowFrontend");

// =======================
// Auth
// =======================
app.UseAuthentication();
app.UseAuthorization();

// =======================
// MVC ROUTING (IMPORTANT)
// =======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// API controllers (attribute routing)
app.MapControllers();

// =======================
// SignalR
// =======================
app.MapHub<BiddingHub>("/hubs/bidding");

// =======================
// Health Endpoints
// =======================
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("ready")
});

// =======================
// Seed Vehicle Data (only if empty)
// =======================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await NhtsaVehicleDataSeeder.SeedFromNhtsaAsync(context, logger);
    
    // Seed identity roles
    await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);

    // Seed default admin account
    await AdminSeeder.SeedAdminAsync(scope.ServiceProvider);
}

app.Run();

using LastCallMotorAuctions.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LastCallMotorAuctions.API.Middleware;
using LastCallMotorAuctions.API.Hubs;
using LastCallMotorAuctions.API.Services;

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
// JWT Authentication
// =======================
var jwtKey = builder.Configuration["JWT:Key"]
    ?? throw new InvalidOperationException("JWT Key is not configured");

var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "LastCallMotorAuctions";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "LastCallMotorAuctions";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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

app.Run();

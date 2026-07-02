using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CentralBackend.Services;
using CentralBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Ingress Security Settings
var securitySettings = builder.Configuration.GetSection("PipelineSecurity");
var signingKey = securitySettings["JwtSigningKey"]
    ?? throw new InvalidOperationException("PipelineSecurity:JwtSigningKey is missing.");

// 2. Setup JWT Bearer Authentication
builder.Services
    .AddAuthentication(options =>
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
            ValidIssuer = securitySettings["Issuer"],
            ValidAudience = securitySettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 3. Register Core Services
builder.Services.AddSingleton<JwtTokenService>();

// PostgreSQL Context Registration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

// MongoDB Logging Service Registration
builder.Services.AddSingleton<MongoLogService>();

// Zoho CRM Service with HttpClient
builder.Services.AddHttpClient<ZohoCrmService>();

var app = builder.Build();

// 4. Ensure Database is Created on Startup (Automatic Migration/Schema creation)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        // EnsureCreated creates the schema in PostgreSQL if not already present
        dbContext.Database.EnsureCreated();
        app.Logger.LogInformation("PostgreSQL Database schema verified/created successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to run PostgreSQL schema verification.");
    }
}

// 5. Middleware Pipeline Configuration
app.UseAuthentication();
app.UseAuthorization();

// Ingress landing page
app.MapGet("/", () => Results.Ok(new
{
    service = "Shopify Zoho Ingress Pipeline API",
    status = "ready",
    endpoints = new[]
    {
        "/api/auth/token (POST - JWT Issue)",
        "/api/orders (POST - Zapier protected ingress)",
        "/api/webhooks/shopify (POST - Shopify HMAC protected webhook)"
    }
})).AllowAnonymous();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.MapControllers();

app.Run();

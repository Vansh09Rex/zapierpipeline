using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecurityGuardAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ════════════════════════════════════════════════════════════════════════════
// SERVICE REGISTRATION
// ════════════════════════════════════════════════════════════════════════════

// ── 1. JWT Token Engine ──────────────────────────────────────────────────────
//    Registered as Singleton: stateless, IConfiguration + ILogger are thread-safe.
builder.Services.AddSingleton<JwtTokenEngine>();

// ── 2. JWT Bearer Authentication ─────────────────────────────────────────────
//    Reads from Jwt configuration section to match your appsettings.json
var jwtSection   = builder.Configuration.GetSection("Jwt");
var rawSigningKey = jwtSection["Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key is missing. Set it via 'dotnet user-secrets' or the Jwt__Key environment variable.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // ── What to validate ──────────────────────────────────────────────
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,

            // ── Expected values (must match what JwtTokenEngine writes) ────────
            ValidIssuer              = jwtSection["Issuer"],
            ValidAudience            = jwtSection["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawSigningKey)),

            // ── Zero clock-skew: reject tokens the instant they expire ─────────
            ClockSkew                = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                log.LogWarning("JWT authentication failed — {Reason}", ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var clientId = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                               ?? ctx.Principal?.FindFirst("sub")?.Value 
                               ?? "unknown";
                log.LogInformation("JWT validated successfully — client: {ClientId}", clientId);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                // Suppress default challenge body so structured responses aren't overwritten
                ctx.HandleResponse();
                ctx.Response.StatusCode  = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync(
                    """{"error":"unauthorized","error_description":"A valid bearer token is required."}""");
            }
        };
    });

// ── 3. Authorization ──────────────────────────────────────────────────────────
builder.Services.AddAuthorization();

// ── 4. Controllers ────────────────────────────────════════════════════════════
builder.Services.AddControllers();


// Memoey Cache----------------
builder.Services.AddMemoryCache();

// ── 5. Native .NET 10 OpenAPI Document Generation ────────────────────────────
builder.Services.AddOpenApi();

// ════════════════════════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE
// ════════════════════════════════════════════════════════════════════════════

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Maps the native OpenAPI specification endpoint to /openapi/v1.json
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// UseAuthentication MUST precede UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

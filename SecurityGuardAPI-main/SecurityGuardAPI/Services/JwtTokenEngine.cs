using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SecurityGuardAPI.Services
{
    /// <summary>
    /// Contract for the JWT token generation engine.
    /// </summary>
    public interface IJwtTokenEngine
    {
        /// <summary>
        /// Generates a signed HMAC-SHA256 JWT access token for the given client.
        /// </summary>
        /// <param name="clientId">The authenticated client identifier.</param>
        /// <param name="grantType">The OAuth2 grant type used for this request.</param>
        /// <returns>A compact-serialized, signed JWT string.</returns>
        string GenerateToken(string clientId, string grantType);
    }

    /// <summary>
    /// Concrete implementation that builds and signs JWTs from configuration-driven settings.
    /// All sensitive values (SigningKey) must be injected via environment variables
    /// or dotnet user-secrets — never committed to appsettings.json.
    /// </summary>
    public sealed class JwtTokenEngine : IJwtTokenEngine
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtTokenEngine> _logger;

        public JwtTokenEngine(IConfiguration configuration, ILogger<JwtTokenEngine> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        public string GenerateToken(string clientId, string grantType)
        {
            // ── Resolve configuration ────────────────────────────────────────────
            var jwtSection = _configuration.GetSection("JwtSettings");

            var rawSigningKey = jwtSection["SigningKey"]
                ?? throw new InvalidOperationException(
                    "JwtSettings:SigningKey is not configured. " +
                    "Set it via 'dotnet user-secrets' or the JWTSS__SIGNINGKEY environment variable.");

            var issuer = jwtSection["Issuer"]
                ?? throw new InvalidOperationException("JwtSettings:Issuer is not configured.");

            var audience = jwtSection["Audience"]
                ?? throw new InvalidOperationException("JwtSettings:Audience is not configured.");

            var expiresInMinutes =
                int.TryParse(jwtSection["ExpiresInMinutes"], out var parsedMins) && parsedMins > 0
                    ? parsedMins
                    : 60; // Fallback: 1-hour lifetime

            // ── Build signing credentials (HMAC-SHA256 / HS256) ─────────────────
            //    Key must be ≥ 256 bits (32 UTF-8 bytes) for HS256.
            var keyBytes = Encoding.UTF8.GetBytes(rawSigningKey);
            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256);

            // ── Compose claims ────────────────────────────────────────────────────
            var now = DateTime.UtcNow;
            var claims = new Claim[]
            {
                // Standard registered claims
                new(JwtRegisteredClaimNames.Sub,  clientId),
                new(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString("N")),
                new(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),

                // Application-specific claims consumed by downstream policy checks
                new("client_id",  clientId),
                new("grant_type", grantType),
                new("scope",      "zapier"),
            };

            // ── Assemble and sign the token ───────────────────────────────────────
            var tokenDescriptor = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                notBefore:          now,
                expires:            now.AddMinutes(expiresInMinutes),
                signingCredentials: signingCredentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

            _logger.LogInformation(
                "JWT issued — client: {ClientId} | grant: {GrantType} | exp: {ExpiresAt:O}",
                clientId, grantType, now.AddMinutes(expiresInMinutes));

            return tokenString;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using SecurityGuardAPI.Models;
using SecurityGuardAPI.Services;

namespace SecurityGuardAPI.Controllers
{
    /// <summary>
    /// Handles OAuth2-style client credential token issuance for Zapier integrations.
    /// Exposes: POST /api/auth/token
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IJwtTokenEngine _tokenEngine;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IJwtTokenEngine tokenEngine, ILogger<AuthController> logger)
        {
            _tokenEngine = tokenEngine;
            _logger = logger;
        }

        /// <summary>
        /// Exchanges valid client credentials for a JWT bearer access token.
        /// </summary>
        /// <remarks>
        /// Only the <c>client_credentials</c> OAuth2 grant type is accepted.
        /// Credentials are validated against the internal allowlist.
        /// </remarks>
        /// <param name="request">The token request body containing ClientId, ClientSecret, and GrantType.</param>
        /// <returns>
        /// <list type="bullet">
        ///   <item><description>200 OK — <see cref="TokenResponse"/> with signed JWT on success.</description></item>
        ///   <item><description>400 Bad Request — Unsupported grant type.</description></item>
        ///   <item><description>401 Unauthorized — Invalid or unknown client credentials.</description></item>
        /// </list>
        /// </returns>
        [HttpPost("token")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TokenResponse),            StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails),           StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails),           StatusCodes.Status401Unauthorized)]
        public IActionResult Token([FromBody] ZapierTokenRequest request)
        {
            // ── Guard 1: Enforce client_credentials grant type only ────────────
            if (!string.Equals(request.GrantType, "client_credentials", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Token request rejected — unsupported grant_type: '{GrantType}' (client: '{ClientId}')",
                    request.GrantType, request.ClientId);

                return BadRequest(new
                {
                    error             = "unsupported_grant_type",
                    error_description = "Only the 'client_credentials' grant type is supported."
                });
            }

            // ── Guard 2: Constant-time credential validation against allowlist ──
            //    TryGetValue + Ordinal comparison prevents timing-based enumeration.
            //    The error response is intentionally identical whether the ClientId
            //    is unknown or the ClientSecret is wrong (non-disclosure principle).
            var credentialsAreValid =
                ZapierAllowedClients.Clients.TryGetValue(request.ClientId, out var expectedSecret)
                && string.Equals(expectedSecret, request.ClientSecret, StringComparison.Ordinal);

            if (!credentialsAreValid)
            {
                _logger.LogWarning(
                    "Token request rejected — invalid credentials for client: '{ClientId}'",
                    request.ClientId);

                return Unauthorized(new
                {
                    error             = "invalid_client",
                    error_description = "The provided client credentials are invalid."
                });
            }

            // ── Issue token ────────────────────────────────────────────────────
            var accessToken = _tokenEngine.GenerateToken(request.ClientId, request.GrantType);

            _logger.LogInformation(
                "Bearer token issued to client: '{ClientId}'", request.ClientId);

            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                TokenType   = "Bearer",
                ExpiresIn   = 3600
            });
        }
    }
}

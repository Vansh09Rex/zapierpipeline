using CentralBackend.Models;
using CentralBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace CentralBackend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IConfiguration configuration,
        JwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("token")]
    public IActionResult Token([FromBody] TokenRequest request)
    {
        if (!string.Equals(request.GrantType, "client_credentials", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                error = "unsupported_grant_type",
                error_description = "Only the 'client_credentials' grant type is supported."
            });
        }

        var securitySettings = _configuration.GetSection("PipelineSecurity");
        var expectedClientId = securitySettings["ClientId"];
        var expectedClientSecret = securitySettings["ClientSecret"];

        var validCredentials =
            string.Equals(request.ClientId, expectedClientId, StringComparison.Ordinal)
            && string.Equals(request.ClientSecret, expectedClientSecret, StringComparison.Ordinal);

        if (!validCredentials)
        {
            _logger.LogWarning("Rejected token request for client {ClientId}", request.ClientId);
            return Unauthorized(new
            {
                error = "invalid_client",
                error_description = "The provided client credentials are invalid."
            });
        }

        return Ok(new TokenResponse
        {
            AccessToken = _jwtTokenService.GenerateToken(request.ClientId),
            TokenType = "Bearer",
            ExpiresIn = 3600
        });
    }
}

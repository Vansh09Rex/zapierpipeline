using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CentralBackend.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string clientId)
    {
        var securitySettings = _configuration.GetSection("PipelineSecurity");
        var signingKey = securitySettings["JwtSigningKey"]
            ?? throw new InvalidOperationException("PipelineSecurity:JwtSigningKey is missing.");
        var issuer = securitySettings["Issuer"]
            ?? throw new InvalidOperationException("PipelineSecurity:Issuer is missing.");
        var audience = securitySettings["Audience"]
            ?? throw new InvalidOperationException("PipelineSecurity:Audience is missing.");
        var expiresInMinutes = int.TryParse(securitySettings["TokenExpiresInMinutes"], out var minutes)
            ? minutes
            : 60;

        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("client_id", clientId),
            new Claim("scope", "zapier:orders"),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

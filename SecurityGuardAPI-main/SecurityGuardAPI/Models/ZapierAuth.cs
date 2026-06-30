namespace SecurityGuardAPI.Models
{
    public class ZapierTokenRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string GrantType { get; set; } = "client_credentials";
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; } = 3600; // 1 hour
    }

    public static class ZapierAllowedClients
    {
        public static readonly Dictionary<string, string> Clients = new()
        {
            { "Zapier_App_01", "SuperSecretZapierSigningKey2026!" }
        };
    }
}

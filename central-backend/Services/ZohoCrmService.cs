using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CentralBackend.Services
{
    public class ZohoCrmService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ZohoCrmService> _logger;
        private string? _cachedAccessToken;
        private DateTime _tokenExpiryTime = DateTime.MinValue;

        public ZohoCrmService(HttpClient httpClient, IConfiguration configuration, ILogger<ZohoCrmService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool Success, string? RecordId, string Message)> SyncOrderToZohoAsync(string orderId, string customerEmail, decimal totalAmount)
        {
            var zohoSettings = _configuration.GetSection("ZohoCrm");
            bool useMock = zohoSettings.GetValue<bool>("UseMock", true);

            if (useMock)
            {
                _logger.LogInformation("ZOHO CRM (MOCK MODE): Simulating order synchronization for OrderId: {OrderId}", orderId);
                await Task.Delay(200); // Simulate API latency
                var mockRecordId = $"ZOHO_MOCK_{Guid.NewGuid().ToString("N")[..12]}";
                _logger.LogInformation("ZOHO CRM (MOCK MODE): Order synced successfully. Mock Record ID: {RecordId}", mockRecordId);
                return (true, mockRecordId, "Mock sync successful");
            }

            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return (false, null, "Failed to retrieve Zoho CRM OAuth access token.");
                }

                var apiBaseUrl = zohoSettings["ApiBaseUrl"] ?? "https://www.zohoapis.com";
                var endpoint = $"{apiBaseUrl.TrimEnd('/')}/crm/v3/Contacts";

                // Format the payload for Zoho CRM v3 Contacts Module
                // In Zoho CRM, "Last_Name" is a mandatory field. We'll extract a portion of email or use a placeholder.
                var namePart = customerEmail.Split('@')[0];
                var payload = new
                {
                    data = new[]
                    {
                        new
                        {
                            Last_Name = namePart,
                            Email = customerEmail,
                            Description = $"Order Ingress Pipeline. OrderId: {orderId}, Total Amount: ${totalAmount}"
                        }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Authorization", $"Zoho-oauthtoken {accessToken}");
                request.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Zoho CRM Contact creation failed. Status: {Status}, Content: {Content}", response.StatusCode, content);
                    return (false, null, $"Zoho API error: {response.StatusCode}");
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array && dataProp.GetArrayLength() > 0)
                {
                    var firstItem = dataProp[0];
                    if (firstItem.TryGetProperty("status", out var statusProp) && statusProp.GetString() == "success")
                    {
                        var recordId = firstItem.GetProperty("details").GetProperty("id").GetString();
                        _logger.LogInformation("Zoho CRM Contact created successfully. Record ID: {RecordId}", recordId);
                        return (true, recordId, "Synced successfully to Zoho CRM");
                    }
                }

                _logger.LogError("Zoho CRM response parsed, but structure did not indicate success. Response: {Response}", content);
                return (false, null, "Zoho API returned unexpected result structure.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during Zoho CRM synchronization.");
                return (false, null, $"Exception: {ex.Message}");
            }
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiryTime)
            {
                return _cachedAccessToken;
            }

            _logger.LogInformation("Refreshing Zoho CRM access token using refresh token...");
            var zohoSettings = _configuration.GetSection("ZohoCrm");
            var accountsBaseUrl = zohoSettings["AccountsBaseUrl"] ?? "https://accounts.zoho.com";
            var tokenUrl = $"{accountsBaseUrl.TrimEnd('/')}/oauth/v2/token";

            var parameters = new Dictionary<string, string>
            {
                { "refresh_token", zohoSettings["RefreshToken"] ?? string.Empty },
                { "client_id", zohoSettings["ClientId"] ?? string.Empty },
                { "client_secret", zohoSettings["ClientSecret"] ?? string.Empty },
                { "grant_type", "refresh_token" }
            };

            var requestContent = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(tokenUrl, requestContent);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zoho CRM token refresh failed. Status: {Status}, Content: {Content}", response.StatusCode, content);
                return null;
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (root.TryGetProperty("access_token", out var accessTokenProp))
            {
                _cachedAccessToken = accessTokenProp.GetString();
                int expiresIn = root.TryGetProperty("expires_in", out var expiresProp) ? expiresProp.GetInt32() : 3600;
                // Buffer the token expiry time by 5 minutes
                _tokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresIn - 300);
                return _cachedAccessToken;
            }

            _logger.LogError("Zoho token response did not contain 'access_token'. Response: {Response}", content);
            return null;
        }
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SecurityGuardAPI.Tests
{
    public class SecurityIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private const string ShopifySecret = "hush_secure_shopify_shared_secret_key_789";

        public SecurityIntegrationTests(WebApplicationFactory<Program> factory)
        {
            // Hardens the test against disk configuration differences by forcing the 
            // exact app secret directly into the test host engine memory.
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Shopify:WebhookSecret", ShopifySecret);
            }).CreateClient();
        }

        // ── ZAPIER GATEWAY TESTS (JWT BEARER SHIELD) ───────────────────────────────

        [Fact]
        public async Task ZapierEndpoint_WithoutToken_Returns401Unauthorized()
        {
            var content = new StringContent("{\"test\":\"data\"}", Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/zapier/receive", content);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ── SHOPIFY WEBHOOK TESTS (HMAC-SHA256 GUARD) ─────────────────────────────

        [Fact]
        public async Task ShopifyEndpoint_WithValidHmacSignature_Returns200Ok()
        {
            // Arrange: Generate a test payload and calculate its true cryptographic signature
            string jsonPayload = "{\"event\":\"order_fulfilled\",\"order_id\":1001,\"total\":150.50}";
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonPayload);
            byte[] keyBytes = Encoding.UTF8.GetBytes(ShopifySecret);
            
            byte[] computedHash = HMACSHA256.HashData(keyBytes, bodyBytes);
            string base64Signature = Convert.ToBase64String(computedHash);

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/shopify/receive")
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            
            request.Headers.Add("X-Shopify-Hmac-SHA256", base64Signature);
            request.Headers.Add("X-Shopify-Topic", "orders/fulfilled");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            string responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("Shopify webhook processed successfully under HMAC guard", responseString);
        }

        [Fact]
        public async Task ShopifyEndpoint_WithInvalidHmacSignature_Returns401Unauthorized()
        {
            // Arrange: Provide an intentionally forged signature
            string jsonPayload = "{\"event\":\"order_fulfilled\",\"order_id\":1001}";
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/shopify/receive")
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            
            request.Headers.Add("X-Shopify-Hmac-SHA256", "completely_fake_and_forged_base64_string==");
            request.Headers.Add("X-Shopify-Topic", "orders/fulfilled");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ShopifyEndpoint_MissingSignatureHeader_Returns401Unauthorized()
        {
            // Arrange: Fire a raw payload without appending security headers entirely
            var content = new StringContent("{\"event\":\"test\"}", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/shopify/receive", content);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}

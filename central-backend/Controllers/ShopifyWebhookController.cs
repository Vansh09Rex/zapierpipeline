using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CentralBackend.Data;
using CentralBackend.Models;
using CentralBackend.Services;
using CentralBackend.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CentralBackend.Controllers
{
    [ApiController]
    [Route("api/webhooks/shopify")]
    public class ShopifyWebhookController : ControllerBase
    {
        private readonly IOrderRepository _repository;
        private readonly MongoLogService _mongoLog;
        private readonly ZohoCrmService _zohoService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ShopifyWebhookController> _logger;

        public ShopifyWebhookController(
            IOrderRepository repository,
            MongoLogService mongoLog,
            ZohoCrmService zohoService,
            IConfiguration configuration,
            ILogger<ShopifyWebhookController> logger)
        {
            _repository = repository;
            _mongoLog = mongoLog;
            _zohoService = zohoService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook()
        {
            _logger.LogInformation("Received Shopify webhook request.");

            // 1. Extract X-Shopify-Hmac-SHA256 header
            if (!Request.Headers.TryGetValue("X-Shopify-Hmac-SHA256", out var hmacHeader))
            {
                _logger.LogWarning("Shopify webhook rejected: Missing X-Shopify-Hmac-SHA256 header.");
                return BadRequest("Missing signature header.");
            }

            // Enable buffering so the request body can be read and then reused if needed
            Request.EnableBuffering();

            // 2. Read the raw body string
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0; // Reset body position
            }

            // 3. Verify Shopify HMAC signature
            var shopifySecret = _configuration["Shopify:WebhookSecret"] ?? string.Empty;
            if (!VerifyHmac(rawBody, shopifySecret, hmacHeader.ToString()))
            {
                _logger.LogWarning("Shopify webhook rejected: Invalid signature verification failed.");
                return Unauthorized("Invalid webhook signature.");
            }

            _logger.LogInformation("Shopify webhook signature verified successfully.");

            // 4. Log raw payload to MongoDB asynchronously
            var headersDict = new Dictionary<string, string>();
            foreach (var header in Request.Headers)
            {
                headersDict[header.Key] = header.Value.ToString();
            }
            // Fire-and-forget or await the log to MongoDB
            _ = _mongoLog.LogPayloadAsync("ShopifyWebhook", rawBody, headersDict);

            // 5. Parse the body to C# OrderPayload
            OrderPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<OrderPayload>(rawBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize Shopify order payload.");
                return BadRequest("Invalid JSON payload.");
            }

            if (payload == null || string.IsNullOrEmpty(payload.OrderId) || string.IsNullOrEmpty(payload.CustomerEmail))
            {
                _logger.LogWarning("Shopify webhook contains invalid order contract. OrderId or CustomerEmail is missing.");
                return BadRequest("Missing required fields (OrderId, CustomerEmail).");
            }

            // 6. Check if order already exists in PostgreSQL
            var existingOrder = await _repository.GetOrderByStringIdAsync(payload.OrderId);
            if (existingOrder != null)
            {
                _logger.LogInformation("Shopify order {OrderId} already processed. Skipping duplicates.", payload.OrderId);
                return Ok(new { success = true, duplicate = true, OrderId = payload.OrderId });
            }

            // 7. Store new order in PostgreSQL
            var newOrder = new Order
            {
                OrderId = payload.OrderId,
                CustomerEmail = payload.CustomerEmail,
                TotalAmount = payload.TotalAmount,
                Source = "Shopify",
                SyncStatus = "Pending"
            };

            await _repository.CreateOrderAsync(newOrder);

            // 8. Outbound synchronization to Zoho CRM
            var (zohoSuccess, zohoRecordId, zohoMessage) = await _zohoService.SyncOrderToZohoAsync(
                payload.OrderId,
                payload.CustomerEmail,
                payload.TotalAmount
            );

            // Update PostgreSQL order with sync results
            newOrder.SyncStatus = zohoSuccess ? "Synced" : "Failed";
            newOrder.ZohoRecordId = zohoRecordId;
            await _repository.UpdateOrderAsync(newOrder);

            return Ok(new
            {
                success = true,
                orderId = payload.OrderId,
                postgresId = newOrder.Id,
                zohoStatus = newOrder.SyncStatus,
                zohoRecordId = newOrder.ZohoRecordId,
                message = zohoMessage
            });
        }

        private bool VerifyHmac(string rawBody, string secret, string hmacHeader)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(hmacHeader))
                return false;

            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(bodyBytes);
                var calculatedHmac = Convert.ToBase64String(hashBytes);

                // Use constant-time byte array comparison to prevent timing attacks
                var calculatedBytes = Encoding.UTF8.GetBytes(calculatedHmac);
                var headerBytes = Encoding.UTF8.GetBytes(hmacHeader);

                return CryptographicOperations.FixedTimeEquals(calculatedBytes, headerBytes);
            }
        }
    }
}

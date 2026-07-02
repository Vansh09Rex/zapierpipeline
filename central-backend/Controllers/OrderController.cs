using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CentralBackend.Data;
using CentralBackend.Models;
using CentralBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly MongoLogService _mongoLog;
        private readonly ZohoCrmService _zohoService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            AppDbContext dbContext,
            MongoLogService mongoLog,
            ZohoCrmService zohoService,
            ILogger<OrderController> logger)
        {
            _dbContext = dbContext;
            _mongoLog = mongoLog;
            _zohoService = zohoService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder()
        {
            _logger.LogInformation("Received protected order creation request from Zapier.");

            // Enable buffering to read raw body for MongoDB logging
            Request.EnableBuffering();

            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0; // Reset position
            }

            // Log raw payload to MongoDB
            var headersDict = new Dictionary<string, string>();
            foreach (var header in Request.Headers)
            {
                headersDict[header.Key] = header.Value.ToString();
            }
            _ = _mongoLog.LogPayloadAsync("ZapierRequest", rawBody, headersDict);

            // Deserialize payload
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
                _logger.LogError(ex, "Failed to deserialize Zapier order payload.");
                return BadRequest("Invalid JSON payload.");
            }

            if (payload == null || string.IsNullOrEmpty(payload.OrderId) || string.IsNullOrEmpty(payload.CustomerEmail))
            {
                _logger.LogWarning("Zapier order payload contains invalid contract. OrderId or CustomerEmail is missing.");
                return BadRequest("Missing required fields (OrderId, CustomerEmail).");
            }

            // Check if order already exists in PostgreSQL
            var existingOrder = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == payload.OrderId);
            if (existingOrder != null)
            {
                _logger.LogInformation("Zapier order {OrderId} already processed. Skipping duplicates.", payload.OrderId);
                return Ok(new { success = true, duplicate = true, OrderId = payload.OrderId });
            }

            // Save to PostgreSQL
            var newOrder = new Order
            {
                OrderId = payload.OrderId,
                CustomerEmail = payload.CustomerEmail,
                TotalAmount = payload.TotalAmount,
                Source = "Zapier",
                SyncStatus = "Pending"
            };

            _dbContext.Orders.Add(newOrder);
            await _dbContext.SaveChangesAsync();

            // Outbound synchronization to Zoho CRM
            var (zohoSuccess, zohoRecordId, zohoMessage) = await _zohoService.SyncOrderToZohoAsync(
                payload.OrderId,
                payload.CustomerEmail,
                payload.TotalAmount
            );

            // Update PostgreSQL order with sync status
            newOrder.SyncStatus = zohoSuccess ? "Synced" : "Failed";
            newOrder.ZohoRecordId = zohoRecordId;
            await _dbContext.SaveChangesAsync();

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
    }
}

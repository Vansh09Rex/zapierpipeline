using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CentralBackend.Data;
using CentralBackend.Models;
using CentralBackend.Services;
using CentralBackend.Repositories;
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
        private readonly IOrderRepository _repository;
        private readonly MongoLogService _mongoLog;
        private readonly ZohoCrmService _zohoService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderRepository repository,
            MongoLogService mongoLog,
            ZohoCrmService zohoService,
            ILogger<OrderController> logger)
        {
            _repository = repository;
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
            var existingOrder = await _repository.GetOrderByStringIdAsync(payload.OrderId);
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

            await _repository.CreateOrderAsync(newOrder);

            // Outbound synchronization to Zoho CRM
            var (zohoSuccess, zohoRecordId, zohoMessage) = await _zohoService.SyncOrderToZohoAsync(
                payload.OrderId,
                payload.CustomerEmail,
                payload.TotalAmount
            );

            // Update PostgreSQL order with sync status
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

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            _logger.LogInformation("Retrieving all orders from database.");
            var orders = await _repository.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            _logger.LogInformation("Retrieving order with ID: {Id}", id);
            var order = await _repository.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Order not found."
                });
            }
            return Ok(order);
        }
    }
}

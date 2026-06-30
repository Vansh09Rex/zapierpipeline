using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SecurityGuardAPI.Filters;
using SecurityGuardAPI.Models; // <-- CHANGE 1: Imported Models namespace

namespace SecurityGuardAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class WebhookController : ControllerBase
    {
        private readonly IMemoryCache _cache;

        public WebhookController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpPost("receive")]
        [ShopifyAuthorize] 
        public IActionResult Receive(
            [FromHeader(Name = "X-Shopify-Webhook-Id")] string? webhookId,
            [FromBody] ShopifyOrder order) // <-- CHANGE 2: Swapped object for ShopifyOrder
        {
            // Guard clause if the header is completely missing
            if (string.IsNullOrEmpty(webhookId))
            {
                return BadRequest(new { error = "Missing X-Shopify-Webhook-Id header." });
            }

            // 3. Idempotency Check: If key exists, it's a duplicate
            if (_cache.TryGetValue(webhookId, out _))
            {
                return Ok(new { message = "Duplicate webhook blocked safely by Idempotency Shield." });
            }

            // 4. Cache Miss: Save the unique ID for 24 hours
            _cache.Set(webhookId, true, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });

            // <-- CHANGE 3: Live printing of parsed data
            Console.WriteLine($"\n[LIVE WEBHOOK] Order #{order.OrderNumber} received! Total: {order.TotalPrice} {order.Currency}");
            foreach (var item in order.LineItems)
            {
                Console.WriteLine($" -> Item: {item.Title} (Qty: {item.Quantity})");
            }

            // 5. Proceed with core webhook business logic safely here
            return Ok(new { message = $"Order {order.OrderNumber} integrated cleanly." });
        }
    }
}

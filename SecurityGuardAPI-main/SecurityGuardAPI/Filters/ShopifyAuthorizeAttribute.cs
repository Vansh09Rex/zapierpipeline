using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SecurityGuardAPI.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ShopifyAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            const string HmacHeader = "X-Shopify-Hmac-Sha256";

            if (!context.HttpContext.Request.Headers.TryGetValue(HmacHeader, out var receivedHmacValues)
                || string.IsNullOrWhiteSpace(receivedHmacValues.FirstOrDefault()))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var receivedHmac = receivedHmacValues.First()!;

            context.HttpContext.Request.EnableBuffering();

            string rawBody;
            using (var reader = new StreamReader(
                context.HttpContext.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            context.HttpContext.Request.Body.Position = 0;

            var configuration = context.HttpContext.RequestServices
                .GetRequiredService<IConfiguration>();

            var webhookSecret = configuration["Shopify:WebhookSecret"]
                ?? throw new InvalidOperationException(
                    "Shopify:WebhookSecret is not configured.");

            var secretBytes  = Encoding.UTF8.GetBytes(webhookSecret);
            var bodyBytes    = Encoding.UTF8.GetBytes(rawBody);

            var computedHash       = HMACSHA256.HashData(secretBytes, bodyBytes);
            var computedHmacBase64 = Convert.ToBase64String(computedHash);

            var receivedBytes = Encoding.UTF8.GetBytes(receivedHmac);
            var computedBytes = Encoding.UTF8.GetBytes(computedHmacBase64);

            if (!CryptographicOperations.FixedTimeEquals(receivedBytes, computedBytes))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }
    }
}

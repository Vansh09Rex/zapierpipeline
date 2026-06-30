namespace SecurityGuardAPI.Models
{
    public class ShopifyWebhookConfig
    {
        public string WebhookSecret { get; set; } = "shp_wss_example_secret_key_abc123";
        public const string ShopifyHmacHeader = "X-Shopify-Hmac-Sha256";
    }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SecurityGuardAPI.Models
{
    // C# 12 Positional Records for memory efficiency and clean syntax
    public record ShopifyOrder(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("order_number")] int OrderNumber,
        [property: JsonPropertyName("total_price")] string TotalPrice,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("line_items")] List<ShopifyLineItem> LineItems
    );

    public record ShopifyLineItem(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("price")] string Price,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("sku")] string? Sku
    );
}

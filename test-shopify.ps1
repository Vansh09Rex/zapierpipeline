# test-shopify.ps1
# PowerShell script to test the Shopify Webhook HMAC signature verification locally

$secret = "shopify-local-secret"
$url = "http://localhost:5080/api/webhooks/shopify"

$payload = '{
  "OrderId": "SHOP-777",
  "CustomerEmail": "shopify-customer@example.com",
  "TotalAmount": 149.99
}'

# Remove extra whitespaces to match clean payload formatting
$cleanPayload = $payload.Trim()

# Calculate HMAC-SHA256 signature (Base64 encoded)
$hmacsha256 = New-Object System.Security.Cryptography.HMACSHA256
$hmacsha256.Key = [Text.Encoding]::UTF8.GetBytes($secret)
$signatureBytes = $hmacsha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($cleanPayload))
$signature = [Convert]::ToBase64String($signatureBytes)

Write-Host "Computed HMAC Signature: $signature"
Write-Host "Sending simulated Shopify Webhook payload to: $url"

$headers = @{
    "X-Shopify-Hmac-SHA256" = $signature
}

try {
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $cleanPayload -ContentType "application/json" -Headers $headers
    Write-Host "`nResponse Received successfully (200 OK):"
    $response | Out-String | Write-Host
} catch {
    Write-Error "Request failed: $_"
    if ($_.Exception.Response) {
        $streamReader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errBody = $streamReader.ReadToEnd()
        Write-Host "Error Response Body: $errBody"
    }
}

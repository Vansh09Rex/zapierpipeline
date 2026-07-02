Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Starting End-to-End Local Pipeline Test..." -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Start the API backend in the background
Write-Host "Step 1: Starting central-backend server..." -ForegroundColor Yellow
$serverProcess = Start-Process dotnet -ArgumentList "bin/Debug/net10.0/CentralBackend.dll --urls http://localhost:5080" -WorkingDirectory "central-backend" -PassThru -NoNewWindow

# Wait for server to boot
Write-Host "Waiting for server to initialize..."
Start-Sleep -Seconds 5

# Check health
try {
    $health = Invoke-RestMethod -Method Get -Uri "http://localhost:5080/health"
    Write-Host "Health Check Status: $($health.status)" -ForegroundColor Green
} catch {
    Write-Error "Failed to connect to the backend server. Exiting."
    Stop-Process -Id $serverProcess.Id -Force
    Exit 1
}

# 2. Run Shopify Webhook HMAC test
Write-Host "`nStep 2: Dispatching Shopify webhook with valid HMAC signature..." -ForegroundColor Yellow
$shopifyResult = .\test-shopify.ps1
Write-Host $shopifyResult

# 3. Request JWT Token for Zapier endpoints
Write-Host "`nStep 3: Requesting JWT token from Auth controller..." -ForegroundColor Yellow
$tokenBody = @{
    clientId = "Zapier_App_01"
    clientSecret = "replace-this-dev-secret-before-ngrok"
    grantType = "client_credentials"
} | ConvertTo-Json

try {
    $tokenResponse = Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/auth/token" -ContentType "application/json" -Body $tokenBody
    $token = $tokenResponse.accessToken
    Write-Host "JWT Token generated successfully: Bearer $($token.Substring(0, 15))..." -ForegroundColor Green
} catch {
    Write-Error "Failed to generate JWT Token. Exiting."
    Stop-Process -Id $serverProcess.Id -Force
    Exit 1
}

# 4. Dispatch simulated Zapier order request
Write-Host "`nStep 4: Dispatching Zapier webhook with JWT Authentication..." -ForegroundColor Yellow
$zapierBody = @{
    OrderId = "ZAP-999"
    CustomerEmail = "zapier-lead@example.com"
    TotalAmount = 450.50
} | ConvertTo-Json

try {
    $zapierResponse = Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/orders" -Headers @{ Authorization = "Bearer $token" } -ContentType "application/json" -Body $zapierBody
    Write-Host "Zapier Response Status: 200 OK" -ForegroundColor Green
    $zapierResponse | ConvertTo-Json | Write-Host
} catch {
    Write-Error "Failed to post order via Zapier endpoint."
}

# 5. Query all orders using the new GET repository endpoint
Write-Host "`nStep 5: Querying database via GET /api/orders endpoint..." -ForegroundColor Yellow
try {
    $orders = Invoke-RestMethod -Method Get -Uri "http://localhost:5080/api/orders" -Headers @{ Authorization = "Bearer $token" }
    Write-Host "Total Orders Stored: $($orders.Count)" -ForegroundColor Green
    $orders | ConvertTo-Json -Depth 5 | Write-Host
} catch {
    Write-Error "Failed to retrieve orders."
}

# 6. Tear down server
Write-Host "`nStep 6: Tearing down backend server..." -ForegroundColor Yellow
Stop-Process -Id $serverProcess.Id -Force
Write-Host "Test complete." -ForegroundColor Green

#  Zapier ASP.NET  Pipeline

Flow:

Shopify Webhook -> Zapier CLI Custom App -> ASP.NET Core Web API -> Zoho CRM

New order contract:

```json
{
  "OrderId": "12345",
  "CustomerEmail": "test@test.com",
  "TotalAmount": 50.00
}
```

## Backend

Install the .NET SDK, then run:

```bash
cd central-backend
dotnet restore
dotnet run --urls http://localhost:5080
```

The backend now uses a client-credentials token gate adapted from `SecurityGuardAPI`.
Zapier first calls:

```text
POST /api/auth/token
```

Then it sends the order to:

```text
POST /api/orders
Authorization: Bearer <accessToken>
```

For local testing, the starter credentials are in `central-backend/appsettings.json`.
Before using ngrok or production, replace these values or override them with environment variables:

```text
PipelineSecurity__ClientId
PipelineSecurity__ClientSecret
PipelineSecurity__JwtSigningKey
PipelineSecurity__Issuer
PipelineSecurity__Audience
```

Local token test:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/auth/token -ContentType 'application/json' -Body '{"clientId":"Zapier_App_01","clientSecret":"replace-this-dev-secret-before-ngrok","grantType":"client_credentials"}'
```

Local protected order test:

```powershell
$token = (Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/auth/token -ContentType 'application/json' -Body '{"clientId":"Zapier_App_01","clientSecret":"replace-this-dev-secret-before-ngrok","grantType":"client_credentials"}').accessToken
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/orders -ContentType 'application/json' -Headers @{ Authorization = "Bearer $token" } -Body '{"OrderId":"12345","CustomerEmail":"test@test.com","TotalAmount":50.00}'
```

## Zapier App

```bash
cd zapier-app
npm install
npm run validate
npm test
```

---

## Live Demo Instructions (Containerized Stack)

To run and showcase the entire secure pipeline locally with PostgreSQL, MongoDB, and Zoho CRM (mock mode) running in Docker, execute the following steps:

### 1. Build and Run the Stack
Ensure your Docker daemon is active, then run:
```powershell
docker-compose up --build -d
```
This builds your Web API container and downloads official PostgreSQL 16 and MongoDB 7 images, linking them in a virtual network.

### 2. Verify API Health
Confirm that the containerized API is running and healthy:
```powershell
Invoke-RestMethod -Method Get -Uri http://localhost:5080/health
```

### 3. Demo the Shopify Webhook Ingress (HMAC Verification)
Run the webhook simulator to test HMAC-SHA256 signature validation:
```powershell
powershell -ExecutionPolicy Bypass -File .\test-shopify.ps1
```
*Calculates signature using the developer secret, attaches it to the header, and verifies securely.*

### 4. Demo the Zapier Ingress (JWT Ingress)
Retrieve a token and post a secure order:
```powershell
# 1. Request JWT token using client credentials
$authResponse = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/auth/token -ContentType 'application/json' -Body '{"clientId":"Zapier_App_01","clientSecret":"replace-this-dev-secret-before-ngrok","grantType":"client_credentials"}'
$token = $authResponse.accessToken

# 2. Call secure orders route
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/orders -ContentType 'application/json' -Headers @{ Authorization = "Bearer $token" } -Body '{"OrderId":"ZAP-888","CustomerEmail":"demo@test.com","TotalAmount":99.99}'
```

### 5. Demo Querying Saved Orders (Merged Feature)
You can fetch all processed orders or inspect a single order via the API (requires JWT Bearer authorization):
```powershell
# Get all orders saved in PostgreSQL
Invoke-RestMethod -Method Get -Uri http://localhost:5080/api/orders -Headers @{ Authorization = "Bearer $token" } | ConvertTo-Json -Depth 5

# Get specific order details by PostgreSQL GUID
Invoke-RestMethod -Method Get -Uri http://localhost:5080/api/orders/<postgres-guid-id> -Headers @{ Authorization = "Bearer $token" } | ConvertTo-Json
```

### 6. Run the Single-Command E2E Test Suite (New)
You can test the entire pipeline (server start, webhook simulation, authentication, database persistence, database query, and teardown) with one command in your PowerShell:
```powershell
powershell -ExecutionPolicy Bypass -File .\run_e2e_tests.ps1
```

### 7. Inspect Databases
View structured order history saved inside PostgreSQL directly:
```powershell
docker exec pipeline_postgres psql -U postgres -d zapier_pipeline -c "SELECT * FROM orders;"
```

View raw JSON audit logs stored in MongoDB:
```powershell
docker exec pipeline_mongodb mongosh zapier_pipeline_logs --eval "db.raw_payload_logs.find().pretty()"
```

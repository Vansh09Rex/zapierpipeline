# Shopify Zapier ASP.NET Zoho Pipeline

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

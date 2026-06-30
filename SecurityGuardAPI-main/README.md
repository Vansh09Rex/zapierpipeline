# SecurityGuardAPI Workspace

An enterprise-grade, hardened gateway built on **.NET 10** designed for machine-to-machine (M2M) communication. It implements an OAuth2-style `client_credentials` grant flow to issue signed JSON Web Tokens (JWT) and exposes fully insulated endpoints intended for secure webhook integrations (such as Zapier).

---

## 🏗️ Workspace Architecture

The repository is structured as a unified multi-project workspace to completely isolate production application binaries from the testing framework, avoiding implicit compilation/file-globbing errors.

```text
SecurityGuardWorkspace/
├── SecurityGuardAPI/               # Production Web Application
│   ├── Controllers/
│   │   ├── AuthController.cs       # Public Token Authority Gate
│   │   └── ZapierController.cs     # Protected Webhook Ingestion Receiver
│   ├── Models/                     # Request/Response Data Transfer Objects (DTOs)
│   ├── Services/                   # JWT Engine and Token Generation Logic
│   └── Program.cs                  # Middleware Pipeline & Dependency Injection
├── SecurityGuardAPI.Tests/         # Automated Quality Assurance Framework
│   ├── SecurityIntegrationTests.cs # In-Memory Integration Security Assertions
│   └── SecurityGuardAPI.Tests.csproj
└── README.md                       # System Manifest & Operator Manual
```

---

## 🛡️ Security Hardening Principles

This gateway is explicitly engineered against common automated exploit vectors:

* **Constant-Time Verification Mechanisms:** Credential checking utilizes direct key lookup algorithms to neutralize microsecond-level timing anomalies, preventing brute-force timing analysis enumeration.
* **Non-Disclosure Error Topography:** Error responses throw perfectly identical payloads and generic HTTP status descriptions whether a client ID is unrecognized or a secret is incorrect, closing user-enumeration disclosure doors.
* **Insulated Runtime Middleware:** The execution pipeline rejects unsigned, mutated, or missing bearer authorizations directly at the protocol layer before routing hits application-level code.

---

## 🚀 Installation & Local Deployment

### System Requirements
* **.NET 10.0 SDK** or newer
* A command-line terminal environment (`bash`, `zsh`, etc.)

### 1. Clone & Environment Initialize
Step into your local deployment workspace and clean-restore code assembly dependencies:
```bash
cd ~/SecurityGuardWorkspace
dotnet restore
```

### 2. Verify Infrastructure Stability
Execute the automated test suite against the built-in mock web host to ensure authorization gates respond correctly under simulated attacks:
```bash
dotnet test SecurityGuardAPI.Tests/SecurityGuardAPI.Tests.csproj
```

### 3. Launch the API Service
Fire up the local engine pipeline:
```bash
dotnet run --project SecurityGuardAPI/SecurityGuardAPI.csproj
```
The application will boot up and bind to standard development ports (typically `http://localhost:5000` or `https://localhost:5001`). Check terminal output logs to confirm binding routes.

---

## 🚦 Endpoint Specifications & Usage Manual

The lifecycle of an ingestion session consists of two stages: **Authentication** followed by **Resource Consumption**.

### Phase 1: Exchanging Credentials for an Access Token
To hit the system, an integration client must pass their provisioned ID and secret to the public auth router.

* **Endpoint:** `POST /api/auth/token`
* **Headers:** `Content-Type: application/json`
* **JSON Payload Matrix:**
```json
{
  "clientId": "your_allowed_client_id",
  "clientSecret": "your_assigned_cryptographic_secret",
  "grantType": "client_credentials"
}
```

#### Verification Execution Example (`curl`):
```bash
curl -X POST http://localhost:5000/api/auth/token \
     -H "Content-Type: application/json" \
     -d '{"clientId":"zapier_integration_node","clientSecret":"super_secure_vault_pass_123","grantType":"client_credentials"}'
```

#### Success Response Pipeline (`200 OK`):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 3600
}
```

---

### Phase 2: Utilizing the Secure Data Gateway
Once you harvest an active token string, assign it to your payload headers to safely transition past the gateway boundary.

* **Endpoint:** `POST /api/zapier/receive`
* **Headers:** * `Authorization: Bearer <YOUR_ACCESS_TOKEN>`
  * `Content-Type: application/json`
* **JSON Payload Matrix:** Any structured business event or transactional package array.

#### Verification Execution Example (`curl`):
```bash
curl -X POST http://localhost:5000/api/zapier/receive \
     -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
     -H "Content-Type: application/json" \
     -d '{"event":"lead_created","email":"prospect@domain.com","status":"active"}'
```

#### Expected Ingestion Handshake (`200 OK`):
```json
{
  "message": "Data integrated safely under JWT shield."
}
```

---

## 🚦 Gateway Rejection Matrix

If incoming operational parameters step outside strict cryptographic criteria, the application forces defensive handling states.

| Condition | HTTP Status | System Response Payload Reason |
| --------- | ----------- | ------------------------------ |
| Missing/Mismatched `grantType` | `400 Bad Request` | `{"error":"unsupported_grant_type","error_description":"..."}` |
| Corrupt or Unknown Credentials | `401 Unauthorized` | `{"error":"invalid_client","error_description":"..."}` |
| Missing or Blank Authorization Header | `401 Unauthorized` | Handled at transport boundary before executing routing modules. |
| Altered/Fake/Malformed JWT | `401 Unauthorized` | Handled via framework level validation middleware. |


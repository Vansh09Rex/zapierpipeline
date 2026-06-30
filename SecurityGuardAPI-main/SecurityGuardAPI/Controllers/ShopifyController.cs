// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║  ShopifyController.cs                                                        ║
// ║  SecurityGuardAPI  —  Shopify Webhook Ingestion & HMAC-SHA256 Guard         ║
// ╚══════════════════════════════════════════════════════════════════════════════╝
//
//  SECURITY MODEL
//  ──────────────
//  Shopify does NOT participate in the JWT / Bearer scheme used by the Zapier
//  gateway.  Instead, Shopify cryptographically signs every webhook payload with
//  HMAC-SHA256 (keyed on a shared secret provisioned in the Partner Dashboard)
//  and places the Base64-encoded digest in the X-Shopify-Hmac-SHA256 header.
//
//  This controller validates that digest before any payload data is examined,
//  using a constant-time byte comparison to eliminate timing-oracle side-channels.
//  Any validation failure emits a generic 401 with no diagnostic body — protecting
//  against oracle-probing and information enumeration by hostile callers.
//
//  REQUIRES (appsettings.json / environment):
//  ─────────────────────────────────────────
//    "Shopify": {
//      "WebhookSecret": "<your-shopify-client-secret>"
//    }
//
//  REQUIRES (Program.cs) — no additional middleware needed.
//  Request buffering is enabled per-request inside the action, which avoids
//  the overhead of buffering all requests globally.
//
//  If a global [Authorize] fallback policy is active (e.g. via
//  RequireAuthorizationPolicy()), the [AllowAnonymous] attribute on the action
//  correctly overrides it so ASP.NET Core's auth middleware does not pre-empt
//  the request before our HMAC guard runs.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecurityGuardAPI.Controllers;

/// <summary>
/// Receives and validates inbound Shopify webhook deliveries at
/// <c>POST /api/shopify/receive</c>.
///
/// <para>
/// A single, schema-agnostic endpoint handles all Shopify webhook topics
/// (orders, inventory, customers, fulfillments, …) by deserialising the
/// validated payload into a <see cref="JsonElement"/> DOM, which downstream
/// services can traverse without requiring topic-specific DTOs.
/// </para>
/// </summary>
[ApiController]
[Route("api/shopify")]
public sealed class ShopifyController(
    IConfiguration configuration,
    ILogger<ShopifyController> logger) : ControllerBase
    {
        // ── Configuration & Header Constants ─────────────────────────────────────────

        /// <summary>
        /// Key path in appsettings.json / environment variables for the Shopify
        /// webhook shared secret (sourced from the Shopify Partner Dashboard or
        /// app configuration page).
        /// </summary>
        private const string WebhookSecretConfigKey = "Shopify:WebhookSecret";

        /// <summary>
        /// The HTTP request header in which Shopify delivers its HMAC-SHA256
        /// signature as a Base64-encoded string.
        /// </summary>
        private const string ShopifyHmacHeader = "X-Shopify-Hmac-SHA256";

        /// <summary>
        /// HTTP header that identifies the webhook event topic, e.g.
        /// <c>orders/create</c>, <c>inventory_levels/update</c>,
        /// <c>customers/data_request</c>.
        /// </summary>
        private const string ShopifyTopicHeader = "X-Shopify-Topic";

        // ── Endpoint ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// <c>POST /api/shopify/receive</c> — the sole inbound webhook ingestion point.
        /// </summary>
        ///
        /// <remarks>
        /// <b>Why [AllowAnonymous]?</b><br/>
        /// Shopify cannot supply a Bearer JWT, so this endpoint must bypass any global
        /// JWT authorization policy that protects the rest of the gateway.
        /// Authentication is performed entirely through the HMAC-SHA256 validation
        /// pipeline below — the absence of a Bearer token is expected and correct.
        ///
        /// <b>Validation pipeline (executed in order):</b>
        /// <list type="number">
        ///   <item>Resolve and verify the shared secret is provisioned.</item>
        ///   <item>Enable seekable request body buffering.</item>
        ///   <item>Drain the body stream into a raw <c>byte[]</c>; rewind the stream.</item>
        ///   <item>Extract and Base64-decode the <c>X-Shopify-Hmac-SHA256</c> header.</item>
        ///   <item>Compute <c>HMAC-SHA256(UTF8(secret), rawBody)</c>.</item>
        ///   <item>Compare signatures using <see cref="CryptographicOperations.FixedTimeEquals"/>.</item>
        ///   <item>Deserialise the validated payload as a <see cref="JsonElement"/>.</item>
        ///   <item>Acknowledge receipt to Shopify with <c>200 OK</c>.</item>
        /// </list>
        /// </remarks>
        [HttpPost("receive")]
        [AllowAnonymous] // HMAC-SHA256 is the auth mechanism here; JWT Bearer does not apply.
        public async Task<IActionResult> ReceiveWebhookAsync(CancellationToken cancellationToken)
        {
            // ── Guard: shared secret must be provisioned ──────────────────────────────
            //
            // Without a secret we cannot validate anything.  Treat this as a fatal
            // server-side misconfiguration (Error-level log) and refuse all requests.
            // Importantly, we still return a generic 401 — the caller must not learn
            // that the rejection is due to a misconfiguration rather than a bad HMAC.
            var webhookSecret = configuration[WebhookSecretConfigKey];

            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                logger.LogError(
                    "Shopify webhook secret is not configured under key '{Key}'. " +
                    "All inbound deliveries are being rejected.", WebhookSecretConfigKey);

                return Unauthorized();
            }

            // ── Step 1: Enable seekable request body buffering ────────────────────────
            //
            // By default, the ASP.NET Core request body is a non-seekable, forward-only
            // stream — once read, the data is gone.  EnableBuffering() replaces it with
            // a FileBufferingReadStream (memory-backed up to a threshold, spilling to a
            // temp file for large payloads) that supports Position = 0.
            //
            // This allows us to:
            //   a) Read the raw bytes once for HMAC computation.
            //   b) Rewind and deserialise the same bytes as JSON without re-reading
            //      from the network.
            //
            // Calling it here (rather than via global middleware) limits the buffering
            // overhead to this endpoint only — no other routes buffer unnecessarily.
            Request.EnableBuffering();

            // ── Step 2: Read the raw request body ────────────────────────────────────
            //
            // Shopify signs the *exact* byte sequence it sends over the wire.  We must
            // hash the same byte sequence — any transcoding, re-serialisation, or
            // whitespace normalisation would produce a different digest and cause every
            // valid request to be rejected.
            byte[] rawBody;
            using (var buffer = new MemoryStream())
            {
                await Request.Body.CopyToAsync(buffer, cancellationToken);
                rawBody = buffer.ToArray();
            }

            // Rewind so downstream deserialisation (Step 6) reads from the beginning.
            Request.Body.Position = 0;

            // ── Step 3: Extract and decode the HMAC header ───────────────────────────
            //
            // The header must be present and must be a syntactically valid Base64
            // string.  We treat both absence and malformation identically — neither
            // condition surfaces in the response body (timing-oracle hardening and
            // information concealment).
            if (!Request.Headers.TryGetValue(ShopifyHmacHeader, out var rawHmacHeader)
                || string.IsNullOrWhiteSpace(rawHmacHeader))
            {
                logger.LogWarning(
                    "Shopify webhook rejected — '{Header}' header is absent or empty.",
                    ShopifyHmacHeader);

                return Unauthorized();
            }

            byte[] receivedSignatureBytes;
            try
            {
                // StringValues.ToString() returns the single header value, or a
                // comma-joined string if multiple values are present (which Shopify
                // never sends; the FormatException below would catch it gracefully).
                receivedSignatureBytes = Convert.FromBase64String(rawHmacHeader.ToString());
            }
            catch (FormatException)
            {
                // Header is present but not valid Base64 → treat as tampered request.
                logger.LogWarning(
                    "Shopify webhook rejected — '{Header}' header is not a valid Base64 string.",
                    ShopifyHmacHeader);

                return Unauthorized();
            }

            // ── Step 4: Compute the expected HMAC-SHA256 digest ──────────────────────
            //
            // HMACSHA256.HashData is a one-shot static API (introduced in .NET 7) that
            // avoids allocating and disposing an HMACSHA256 instance manually.  It
            // accepts the key and data as ReadOnlySpan<byte>, keeping allocations minimal.
            //
            // The key is the UTF-8 encoding of the shared secret string from config.
            var secretKeyBytes = Encoding.UTF8.GetBytes(webhookSecret);
            byte[] computedSignatureBytes = HMACSHA256.HashData(secretKeyBytes, rawBody);

            // ── Step 5: Constant-time signature comparison ────────────────────────────
            //
            // WHY NOT == or SequenceEqual()?
            // Standard equality comparisons short-circuit the moment they find a
            // differing byte.  This means a request with the first byte correct takes
            // microscopically longer to reject than one that fails on byte 0.  A
            // statistical attacker can exploit this timing difference across thousands
            // of requests to reconstruct the correct HMAC one byte at a time — a
            // "timing oracle" attack.
            //
            // CryptographicOperations.FixedTimeEquals(left, right) always iterates
            // through the *entire* span regardless of where the first mismatch occurs,
            // making execution time independent of the position of differing bytes.
            //
            // It also returns false when span lengths differ, eliminating length
            // extension probing without a separate guard.
            var signatureIsValid = CryptographicOperations.FixedTimeEquals(
                computedSignatureBytes,      // what we computed
                receivedSignatureBytes);     // what Shopify sent

            if (!signatureIsValid)
            {
                // Server-side log only — the caller must not learn whether the failure
                // was due to a wrong key, a tampered body, or a replay attempt.
                logger.LogWarning(
                    "Shopify webhook rejected — HMAC-SHA256 signature mismatch. " +
                    "Possible root causes: wrong shared secret, payload tampering, " +
                    "or a replay of a stale delivery.");

                return Unauthorized();
            }

            // ── Step 6: Deserialise the HMAC-validated payload ───────────────────────
            //
            // JsonElement gives us a schema-agnostic, allocation-light, read-only DOM
            // view of the payload.  A single endpoint can therefore handle every
            // Shopify webhook topic (orders/create, fulfillments/update,
            // inventory_levels/connect, customers/data_erasure, …) without a library
            // of topic-specific request DTOs.
            //
            // The body stream is already rewound to Position 0 from Step 2.
            JsonElement payload;
            try
            {
                payload = await JsonSerializer.DeserializeAsync<JsonElement>(
                    Request.Body,
                    cancellationToken: cancellationToken);
            }
            catch (JsonException ex)
            {
                // The payload cleared HMAC validation but is malformed JSON.
                // This should never happen with genuine Shopify deliveries but is
                // worth surfacing as a Warning for diagnostics.
                logger.LogWarning(
                    ex,
                    "Shopify webhook passed HMAC validation but body is not valid JSON. " +
                    "Raw body size: {Bytes} byte(s).", rawBody.Length);

                // Return 400 rather than 500 — the payload content is the problem,
                // not a server-side fault.
                return BadRequest();
            }

            // ── Step 7: Observability & downstream routing ────────────────────────────
            //
            // X-Shopify-Topic (e.g. "orders/create") identifies the event type.
            // Log it for traceability, then hand the validated payload to a domain
            // dispatcher — keeping the controller slim and responsibility-focused.
            var topic = Request.Headers[ShopifyTopicHeader].ToString();

            logger.LogInformation(
                "Shopify webhook received and HMAC-validated. " +
                "Topic: '{Topic}' | Raw body: {Bytes} byte(s) | " +
                "Root properties: {PropCount}.",
                string.IsNullOrWhiteSpace(topic) ? "<not provided>" : topic,
                                  rawBody.Length,
                                  payload.ValueKind is JsonValueKind.Object
                                  ? payload.EnumerateObject().Count()
                                  : 0);

            // TODO: Route the validated payload to the appropriate domain handler, e.g.:
            //
            //   await _webhookDispatcher.DispatchAsync(topic, payload, cancellationToken);
            //
            // Heavy processing (database writes, downstream HTTP calls) must be enqueued
            // as background work (e.g. via IBackgroundTaskQueue / Channel<T> / a message
            // bus) and NOT executed inline here.  Shopify requires a 2xx response within
            // 5 seconds or it will classify the delivery as failed and retry with
            // exponential back-off (up to 19 retries over 48 hours).

            // ── Step 8: Acknowledge receipt ───────────────────────────────────────────
            //
            // Return 200 immediately after validation and dispatch enqueue.
            // The clean JSON body aids observability in gateway logs and Shopify's
            // Partner Dashboard delivery history.
            return Ok(new { message = "Shopify webhook processed successfully under HMAC guard." });
        }
    }

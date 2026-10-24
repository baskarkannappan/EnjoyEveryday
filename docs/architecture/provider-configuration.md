# Provider Configuration Guide

> Document: `docs/architecture/provider-configuration.md`  
> Status: **Stage 4 — Validated Configuration**  
> Covers: Storage & AI Provider configuration parameters, defaults, environment overrides, and security best practices.

---

## 1. Storage Provider Configuration

Storage options are defined under `Providers:Storage` in `appsettings.json` or through environment variables.

### 1.1 Configuration Schema

```json
{
  "Providers": {
    "Storage": {
      "DefaultProvider": "Local",
      "Local": {
        "BasePath": "Storage"
      },
      "AzureBlob": {
        "ConnectionString": "",
        "ContainerName": "enjoyeveryday-media"
      },
      "AmazonS3": {
        "ServiceUrl": "https://s3.amazonaws.com",
        "BucketName": "enjoyeveryday-media-bucket",
        "AccessKeyId": "",
        "SecretAccessKey": "",
        "Region": "us-east-1"
      },
      "CloudflareR2": {
        "ServiceUrl": "https://<account_id>.r2.cloudflarestorage.com",
        "BucketName": "enjoyeveryday-media-r2",
        "AccessKeyId": "",
        "SecretAccessKey": "",
        "Region": "auto"
      }
    }
  }
}
```

### 1.2 Environment Variable Overrides
To avoid committing secrets, use environment variables in deployment environments:
- `Providers__Storage__DefaultProvider=Local` (or `AzureBlob`, `AmazonS3`, `CloudflareR2`)
- `Providers__Storage__AzureBlob__ConnectionString="..."`
- `Providers__Storage__AmazonS3__AccessKeyId="..."`
- `Providers__Storage__AmazonS3__SecretAccessKey="..."`
- `Providers__Storage__CloudflareR2__AccessKeyId="..."`
- `Providers__Storage__CloudflareR2__SecretAccessKey="..."`

---

## 2. AI Intelligence Engine Configuration

The AI subsystem is structured as a tenant-isolated intelligence service (`EnjoyEveryday.AiAgent`).

### 2.1 Configuration Schema

In .NET client configuration (`appsettings.json`):
```json
{
  "Providers": {
    "AI": {
      "BaseUrl": "http://127.0.0.1:8000",
      "TimeoutSeconds": 15
    }
  }
}
```

In the Python AI Engine (`.env` or Environment Variables):
```bash
# Provider selection (GoogleGemini, OpenAI, Anthropic, Ollama)
AI_PROVIDER=GoogleGemini

# Default model name
AI_MODEL=gemini-flash-latest

# Operational bounds & timeout
AI_TIMEOUT_SECONDS=60
AI_MAX_RETRIES=2

# Concurrency & rate limits
MAX_CONCURRENT_GENERATIONS=10
RATE_LIMIT_PER_MINUTE=60

# Optional server-to-server security token
REQUIRE_SERVICE_AUTH=false
AI_SERVICE_SECRET=
```

---

## 3. Tenant AI Key Management

As established in Stage 0:
- **Each daycare tenant provides their own AI API key** configured in Daycare Settings.
- The key is securely encrypted using AES-GCM (`tenants.encrypted_ai_api_key`) and never stored in plain text.
- During AI calls, the key is passed over the local authenticated bridge using the `X-Tenant-Api-Key` header with per-tenant rate-limiting and correlation ID tracking.

---

## 4. Fail-Safe and Fallback Policy

1. **Local Disk Priority**: If cloud credentials are missing or transient connection issues occur, the storage layer automatically falls back to local storage without throwing unhandled exceptions.
2. **Never Silently Downgrade Models**: Model selection is explicit and validated. If an unrecognized provider or model is specified, startup or execution halts with a transparent error.
3. **No Credential Logging**: Neither application logs nor database tables output plain text access keys, connection strings, or signed URLs.

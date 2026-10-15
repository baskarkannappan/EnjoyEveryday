from fastapi import Header, HTTPException, Depends
from typing import Optional
import hashlib
import uuid
from app.config import settings
from app.security.tenant_context import TenantContext
from app.security.rate_limiter import rate_limiter

async def get_tenant_context(
    x_tenant_api_key: Optional[str] = Header(default=None, alias="X-Tenant-Api-Key"),
    x_tenant_id: Optional[str] = Header(default=None, alias="X-Tenant-Id"),
    x_service_token: Optional[str] = Header(default=None, alias="X-Service-Token"),
    x_correlation_id: Optional[str] = Header(default=None, alias="X-Correlation-Id"),
) -> TenantContext:
    # Safe extraction when called directly as function
    svc_token = x_service_token if isinstance(x_service_token, str) else None
    t_key = x_tenant_api_key if isinstance(x_tenant_api_key, str) else None
    t_id = x_tenant_id if isinstance(x_tenant_id, str) else None
    corr_id = x_correlation_id if isinstance(x_correlation_id, str) else None

    # 1. Server-to-server authorization check if configured
    if settings.REQUIRE_SERVICE_AUTH and settings.SERVICE_SECRET:
        if not svc_token or svc_token != settings.SERVICE_SECRET:
            raise HTTPException(
                status_code=401,
                detail="Unauthorized: Missing or invalid X-Service-Token."
            )

    # 2. Gemini API key validation
    if not t_key or not t_key.strip():
        raise HTTPException(
            status_code=401,
            detail="X-Tenant-Api-Key header is missing or empty. Please configure your AI API key in Settings."
        )

    clean_key = t_key.strip()
    if len(clean_key) < 15:
        raise HTTPException(
            status_code=401,
            detail="Invalid X-Tenant-Api-Key format provided."
        )

    # 3. Derive or use Tenant ID
    if t_id and t_id.strip():
        resolved_tenant_id = t_id.strip()
    else:
        # Fallback for legacy callers: deterministic pseudonymized tenant ID from API key
        key_hash = hashlib.sha256(clean_key.encode("utf-8")).hexdigest()[:16]
        resolved_tenant_id = f"tenant-{key_hash}"

    correlation_id = corr_id.strip() if corr_id and corr_id.strip() else str(uuid.uuid4())

    # 4. Check tenant rate limit
    await rate_limiter.check_and_record(resolved_tenant_id)

    return TenantContext(
        tenant_id=resolved_tenant_id,
        gemini_api_key=clean_key,
        caller_service=x_service_token,
        correlation_id=correlation_id
    )

import pytest
from fastapi.testclient import TestClient
from app.main import app
from app.security.rate_limiter import rate_limiter

client = TestClient(app)

def test_missing_credentials_rejected():
    response = client.post("/api/ideas", json={"idea": "Sensory water table"})
    assert response.status_code == 401
    assert "X-Tenant-Api-Key header is missing" in response.json()["detail"]

def test_invalid_short_credentials_rejected():
    response = client.post(
        "/api/ideas",
        headers={"X-Tenant-Api-Key": "short_key"},
        json={"idea": "Sensory water table"}
    )
    assert response.status_code == 401
    assert "Invalid X-Tenant-Api-Key" in response.json()["detail"]

def test_tenant_context_isolation():
    # Test that different API keys produce distinct tenant isolation contexts
    from app.security.auth import get_tenant_context
    import asyncio

    async def check():
        ctx1 = await get_tenant_context(x_tenant_api_key="valid_tenant_key_one_1234567")
        ctx2 = await get_tenant_context(x_tenant_api_key="valid_tenant_key_two_9876543")
        assert ctx1.tenant_id != ctx2.tenant_id
        assert ctx1.gemini_api_key != ctx2.gemini_api_key

    asyncio.run(check())

def test_rate_limiter_rejection():
    # Artificially fill rate limit history for a test tenant
    import asyncio
    from fastapi import HTTPException

    async def trigger_rate_limit():
        test_tenant = "rate-limited-test-tenant"
        for _ in range(60):
            await rate_limiter.check_and_record(test_tenant)
        
        with pytest.raises(HTTPException) as exc_info:
            await rate_limiter.check_and_record(test_tenant)
        assert exc_info.value.status_code == 429

    asyncio.run(trigger_rate_limit())

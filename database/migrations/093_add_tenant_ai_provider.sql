-- ============================================================================
-- Migration 093: Add ai_provider to tenants and add procedures for multi-provider AI
-- ============================================================================

ALTER TABLE tenants 
ADD COLUMN IF NOT EXISTS ai_provider VARCHAR(50) DEFAULT 'GoogleGemini';

CREATE OR REPLACE FUNCTION fn_get_tenant_ai_settings(p_tenant_id uuid)
RETURNS TABLE (
    encrypted_key character varying,
    ai_provider character varying
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        t.encrypted_ai_api_key, 
        COALESCE(t.ai_provider, 'GoogleGemini')::varchar
    FROM tenants t
    WHERE t.id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_set_tenant_ai_settings(
    p_tenant_id uuid,
    p_encrypted_key character varying,
    p_ai_provider character varying
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE tenants 
    SET encrypted_ai_api_key = p_encrypted_key,
        ai_provider = COALESCE(p_ai_provider, 'GoogleGemini'),
        updated_at = NOW()
    WHERE id = p_tenant_id;
END;
$$;

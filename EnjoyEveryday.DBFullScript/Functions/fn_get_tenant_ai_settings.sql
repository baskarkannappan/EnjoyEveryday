-- Function: public.fn_get_tenant_ai_settings
-- Backup generated on 2026-11-11 09:05:33

CREATE OR REPLACE FUNCTION public.fn_get_tenant_ai_settings(p_tenant_id uuid)  RETURNS TABLE(encrypted_key character varying, ai_provider character varying)  LANGUAGE plpgsql  STABLE AS $function$ BEGIN     RETURN QUERY     SELECT          t.encrypted_ai_api_key,          COALESCE(t.ai_provider, 'GoogleGemini')::varchar     FROM tenants t     WHERE t.id = p_tenant_id; END; $function$ ;

-- Function: public.sp_set_tenant_ai_settings
-- Backup generated on 2026-11-11 09:05:58

CREATE OR REPLACE PROCEDURE public.sp_set_tenant_ai_settings(IN p_tenant_id uuid, IN p_encrypted_key character varying, IN p_ai_provider character varying)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE tenants      SET encrypted_ai_api_key = p_encrypted_key,         ai_provider = COALESCE(p_ai_provider, 'GoogleGemini'),         updated_at = NOW()     WHERE id = p_tenant_id; END; $procedure$ ;

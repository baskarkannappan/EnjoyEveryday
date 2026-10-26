-- Function: public.fn_get_tenant_encrypted_ai_api_key
-- Backup generated on 2026-10-25 20:33:43

CREATE OR REPLACE FUNCTION public.fn_get_tenant_encrypted_ai_api_key(p_tenant_id uuid)  RETURNS character varying  LANGUAGE plpgsql AS $function$ DECLARE     v_key VARCHAR; BEGIN     SELECT encrypted_ai_api_key INTO v_key FROM tenants WHERE id = p_tenant_id;     RETURN v_key; END; $function$ ;

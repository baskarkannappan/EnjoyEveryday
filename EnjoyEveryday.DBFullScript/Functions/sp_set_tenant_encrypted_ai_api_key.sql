-- Function: public.sp_set_tenant_encrypted_ai_api_key
-- Backup generated on 2026-10-25 20:34:06

CREATE OR REPLACE PROCEDURE public.sp_set_tenant_encrypted_ai_api_key(IN p_tenant_id uuid, IN p_encrypted_key character varying)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE tenants SET encrypted_ai_api_key = p_encrypted_key WHERE id = p_tenant_id; END; $procedure$ ;

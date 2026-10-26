-- Function: public.fn_get_active_support_grant
-- Backup generated on 2026-10-25 20:32:58

CREATE OR REPLACE FUNCTION public.fn_get_active_support_grant(p_tenant_id uuid, p_user_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, granted_to_user_id uuid, purpose character varying, expires_at timestamp with time zone, read_only boolean)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT sag.id, sag.tenant_id, sag.granted_to_user_id, sag.purpose,            sag.expires_at, sag.read_only     FROM support_access_grants sag     WHERE sag.tenant_id = p_tenant_id AND sag.granted_to_user_id = p_user_id       AND sag.revoked_at IS NULL AND sag.expires_at > NOW()     ORDER BY sag.expires_at DESC LIMIT 1; END; $function$ ;

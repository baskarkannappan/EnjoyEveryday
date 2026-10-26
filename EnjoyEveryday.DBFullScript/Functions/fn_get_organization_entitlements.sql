-- Function: public.fn_get_organization_entitlements
-- Backup generated on 2026-10-25 20:33:33

CREATE OR REPLACE FUNCTION public.fn_get_organization_entitlements(p_organization_id uuid)  RETURNS SETOF organization_entitlements  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM organization_entitlements     WHERE organization_id = p_organization_id AND status = 'Active'       AND (expires_at IS NULL OR expires_at > NOW())     ORDER BY granted_at DESC; $function$ ;

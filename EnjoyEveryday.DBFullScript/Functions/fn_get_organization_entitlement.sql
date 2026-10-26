-- Function: public.fn_get_organization_entitlement
-- Backup generated on 2026-10-25 20:33:33

CREATE OR REPLACE FUNCTION public.fn_get_organization_entitlement(p_organization_id uuid, p_platform_experience_id uuid)  RETURNS SETOF organization_entitlements  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM organization_entitlements     WHERE organization_id = p_organization_id AND platform_experience_id = p_platform_experience_id       AND status = 'Active' AND (expires_at IS NULL OR expires_at > NOW()); $function$ ;

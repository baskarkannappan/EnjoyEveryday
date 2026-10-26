-- Function: public.fn_get_experience_audit_events
-- Backup generated on 2026-10-25 20:33:19

CREATE OR REPLACE FUNCTION public.fn_get_experience_audit_events(p_organization_id uuid DEFAULT NULL::uuid, p_platform_experience_id uuid DEFAULT NULL::uuid)  RETURNS SETOF experience_audit_events  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM experience_audit_events     WHERE (p_organization_id IS NULL OR organization_id = p_organization_id)       AND (p_platform_experience_id IS NULL OR platform_experience_id = p_platform_experience_id)     ORDER BY created_at DESC     LIMIT 200; $function$ ;

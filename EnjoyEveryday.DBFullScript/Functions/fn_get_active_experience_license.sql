-- Function: public.fn_get_active_experience_license
-- Backup generated on 2026-10-25 20:32:57

CREATE OR REPLACE FUNCTION public.fn_get_active_experience_license(p_experience_id uuid)  RETURNS SETOF experience_licenses  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM experience_licenses     WHERE experience_id = p_experience_id AND is_active = TRUE       AND (expires_at IS NULL OR expires_at > NOW())     ORDER BY granted_at DESC     LIMIT 1; $function$ ;

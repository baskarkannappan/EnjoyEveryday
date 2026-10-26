-- Function: public.fn_get_platform_experience_versions
-- Backup generated on 2026-10-25 20:33:38

CREATE OR REPLACE FUNCTION public.fn_get_platform_experience_versions(p_platform_experience_id uuid)  RETURNS SETOF platform_experience_versions  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM platform_experience_versions     WHERE platform_experience_id = p_platform_experience_id     ORDER BY version_number DESC; $function$ ;

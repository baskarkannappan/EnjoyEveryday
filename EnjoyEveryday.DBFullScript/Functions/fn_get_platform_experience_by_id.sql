-- Function: public.fn_get_platform_experience_by_id
-- Backup generated on 2026-10-25 20:33:37

CREATE OR REPLACE FUNCTION public.fn_get_platform_experience_by_id(p_id uuid)  RETURNS SETOF platform_experiences  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM platform_experiences     WHERE id = p_id; $function$ ;

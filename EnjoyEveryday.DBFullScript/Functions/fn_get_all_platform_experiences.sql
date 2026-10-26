-- Function: public.fn_get_all_platform_experiences
-- Backup generated on 2026-10-25 20:33:00

CREATE OR REPLACE FUNCTION public.fn_get_all_platform_experiences()  RETURNS SETOF platform_experiences  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM platform_experiences     ORDER BY created_at DESC; $function$ ;

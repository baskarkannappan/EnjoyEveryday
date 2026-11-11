-- Function: public.fn_get_platform_admin_count
-- Backup generated on 2026-11-11 09:05:27

CREATE OR REPLACE FUNCTION public.fn_get_platform_admin_count()  RETURNS integer  LANGUAGE sql  STABLE AS $function$     SELECT COUNT(DISTINCT ur.user_id)::integer     FROM user_roles ur     JOIN roles r ON ur.role_id = r.id     WHERE r.name IN ('PlatformAdmin', 'SuperAdmin'); $function$ ;

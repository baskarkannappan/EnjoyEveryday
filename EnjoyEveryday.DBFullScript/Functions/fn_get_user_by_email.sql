-- Function: public.fn_get_user_by_email
-- Backup generated on 2026-11-11 09:05:35

CREATE OR REPLACE FUNCTION public.fn_get_user_by_email(p_email character varying)  RETURNS TABLE(id uuid, tenant_id uuid, email character varying, first_name character varying, last_name character varying, display_name character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT u.id, u.tenant_id, u.email, u.first_name, u.last_name, u.display_name, u.is_active, u.created_at, u.updated_at     FROM users u     WHERE u.normalized_email = UPPER(TRIM(p_email)) OR LOWER(u.email) = LOWER(TRIM(p_email))     LIMIT 1; END; $function$ ;

-- Function: public.fn_get_all_users
-- Backup generated on 2026-10-25 20:33:01

CREATE OR REPLACE FUNCTION public.fn_get_all_users(p_tenant_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, email character varying, first_name character varying, last_name character varying, display_name character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT u.id, u.tenant_id, u.email, u.first_name, u.last_name, u.display_name, u.is_active, u.created_at, u.updated_at     FROM users u     WHERE u.tenant_id = p_tenant_id     ORDER BY u.first_name, u.last_name; END; $function$ ;

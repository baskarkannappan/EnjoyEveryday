-- Function: public.sp_assign_platform_role
-- Backup generated on 2026-11-11 09:05:39

CREATE OR REPLACE PROCEDURE public.sp_assign_platform_role(IN p_user_id uuid, IN p_role_name character varying)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO user_roles (user_id, role_id)     SELECT p_user_id, id     FROM roles     WHERE name = p_role_name AND tenant_id IS NULL AND scope_type = 'Platform'     ON CONFLICT DO NOTHING; END; $procedure$ ;

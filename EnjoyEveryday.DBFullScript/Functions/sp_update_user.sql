-- Function: public.sp_update_user
-- Backup generated on 2026-10-25 20:34:14

CREATE OR REPLACE PROCEDURE public.sp_update_user(IN p_id uuid, IN p_tenant_id uuid, IN p_email character varying, IN p_normalized_email character varying, IN p_first_name character varying, IN p_last_name character varying, IN p_display_name character varying, IN p_is_active boolean, IN p_updated_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE users     SET first_name = p_first_name,         last_name = p_last_name,         display_name = p_display_name,         email = p_email,         normalized_email = p_normalized_email,         is_active = p_is_active,         updated_at = p_updated_at     WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;

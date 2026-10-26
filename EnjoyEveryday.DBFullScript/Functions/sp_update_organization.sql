-- Function: public.sp_update_organization
-- Backup generated on 2026-10-25 20:34:13

CREATE OR REPLACE PROCEDURE public.sp_update_organization(IN p_id uuid, IN p_tenant_id uuid, IN p_name character varying, IN p_location character varying, IN p_is_active boolean, IN p_updated_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE organizations     SET name = p_name,         location = p_location,         is_active = p_is_active,         updated_at = p_updated_at     WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;

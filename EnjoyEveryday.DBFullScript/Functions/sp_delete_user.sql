-- Function: public.sp_delete_user
-- Backup generated on 2026-10-25 20:34:00

CREATE OR REPLACE PROCEDURE public.sp_delete_user(IN p_tenant_id uuid, IN p_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM users WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;

-- Function: public.sp_delete_teacher
-- Backup generated on 2026-11-11 09:05:50

CREATE OR REPLACE PROCEDURE public.sp_delete_teacher(IN p_tenant_id uuid, IN p_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE teachers SET status = 'Inactive'     WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;

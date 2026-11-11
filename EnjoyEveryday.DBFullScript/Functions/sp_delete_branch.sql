-- Function: public.sp_delete_branch
-- Backup generated on 2026-11-11 09:05:47

CREATE OR REPLACE PROCEDURE public.sp_delete_branch(IN p_tenant_id uuid, IN p_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM branches     WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;

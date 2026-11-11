-- Function: public.sp_delete_child
-- Backup generated on 2026-11-11 09:05:48

CREATE OR REPLACE PROCEDURE public.sp_delete_child(IN p_tenant_id uuid, IN p_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE children SET is_active = false     WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;

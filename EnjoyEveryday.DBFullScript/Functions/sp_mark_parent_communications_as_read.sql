-- Function: public.sp_mark_parent_communications_as_read
-- Backup generated on 2026-10-25 20:34:04

CREATE OR REPLACE PROCEDURE public.sp_mark_parent_communications_as_read(IN p_tenant_id uuid, IN p_child_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE parent_communications     SET delivery_status = 'Read', updated_at = NOW()     WHERE tenant_id = p_tenant_id       AND child_id = p_child_id       AND teacher_id = '00000000-0000-0000-0000-000000000000'       AND delivery_status != 'Read'; END; $procedure$ ;

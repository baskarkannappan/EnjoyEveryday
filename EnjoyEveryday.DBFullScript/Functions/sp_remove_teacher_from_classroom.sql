-- Function: public.sp_remove_teacher_from_classroom
-- Backup generated on 2026-11-11 09:05:56

CREATE OR REPLACE PROCEDURE public.sp_remove_teacher_from_classroom(IN p_tenant_id uuid, IN p_classroom_id uuid, IN p_user_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM classroom_teachers     WHERE classroom_id = p_classroom_id       AND user_id = p_user_id       AND tenant_id = p_tenant_id; END; $procedure$ ;

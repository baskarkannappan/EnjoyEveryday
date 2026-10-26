-- Function: public.sp_assign_substitute_coverage
-- Backup generated on 2026-10-25 20:33:49

CREATE OR REPLACE PROCEDURE public.sp_assign_substitute_coverage(IN p_tenant_id uuid, IN p_leave_id uuid, IN p_substitute_teacher_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE staff_leaves     SET substitute_teacher_id = p_substitute_teacher_id,         coverage_status = 'Covered'     WHERE tenant_id = p_tenant_id AND id = p_leave_id; END; $procedure$ ;

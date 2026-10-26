-- Function: public.fn_get_present_child_ids_by_classroom
-- Backup generated on 2026-10-25 20:33:40

CREATE OR REPLACE FUNCTION public.fn_get_present_child_ids_by_classroom(p_tenant_id uuid, p_classroom_id uuid, p_date date)  RETURNS TABLE(child_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ca.child_id     FROM child_attendance ca     WHERE ca.tenant_id = p_tenant_id AND ca.classroom_id = p_classroom_id AND ca.attendance_date = p_date AND ca.status IN ('Present', 'Late'); END; $function$ ;

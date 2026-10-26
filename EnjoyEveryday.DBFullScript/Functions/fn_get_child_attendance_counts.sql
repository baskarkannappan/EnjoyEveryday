-- Function: public.fn_get_child_attendance_counts
-- Backup generated on 2026-10-25 20:33:07

CREATE OR REPLACE FUNCTION public.fn_get_child_attendance_counts(p_tenant_id uuid, p_branch_id uuid, p_date date)  RETURNS TABLE(classroom_id uuid, present_count integer)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ca.classroom_id, COUNT(*)::INT AS present_count     FROM child_attendance ca     WHERE ca.tenant_id = p_tenant_id AND ca.branch_id = p_branch_id AND ca.attendance_date = p_date       AND (ca.status = 'Present' OR ca.status = 'Late')       AND ca.departure_time IS NULL     GROUP BY ca.classroom_id; END; $function$ ;

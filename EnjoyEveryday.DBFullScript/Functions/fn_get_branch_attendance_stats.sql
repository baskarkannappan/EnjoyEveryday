-- Function: public.fn_get_branch_attendance_stats
-- Backup generated on 2026-10-25 20:33:04

CREATE OR REPLACE FUNCTION public.fn_get_branch_attendance_stats(p_tenant_id uuid, p_branch_id uuid)  RETURNS TABLE(total_attendance_records integer, present_count integer)  LANGUAGE sql  STABLE AS $function$     SELECT          COUNT(*)::int AS total_attendance_records,         COUNT(CASE WHEN status = 'Present' THEN 1 END)::int AS present_count     FROM child_attendance     WHERE tenant_id = p_tenant_id AND branch_id = p_branch_id       AND attendance_date >= CURRENT_DATE - INTERVAL '30 days'; $function$ ;

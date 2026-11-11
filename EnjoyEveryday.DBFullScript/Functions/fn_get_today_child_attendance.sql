-- Function: public.fn_get_today_child_attendance
-- Backup generated on 2026-11-11 09:05:34

CREATE OR REPLACE FUNCTION public.fn_get_today_child_attendance(p_tenant_id uuid, p_child_id uuid, p_date date)  RETURNS SETOF child_attendance  LANGUAGE sql  STABLE AS $function$     SELECT * FROM child_attendance     WHERE tenant_id = p_tenant_id AND child_id = p_child_id AND attendance_date = p_date     LIMIT 1; $function$ ;

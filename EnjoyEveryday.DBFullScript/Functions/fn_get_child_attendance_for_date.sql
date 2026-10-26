-- Function: public.fn_get_child_attendance_for_date
-- Backup generated on 2026-10-25 20:33:07

CREATE OR REPLACE FUNCTION public.fn_get_child_attendance_for_date(p_child_id uuid, p_date date)  RETURNS SETOF child_attendance  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM child_attendance     WHERE child_id = p_child_id AND attendance_date = p_date     LIMIT 1; $function$ ;

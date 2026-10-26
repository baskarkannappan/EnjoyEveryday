-- Function: public.sp_close_end_of_day_teachers
-- Backup generated on 2026-10-25 20:33:49

CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_teachers(IN p_clock_out time without time zone, IN p_ids uuid[])  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE teacher_attendance     SET clock_out_time = p_clock_out,         notes = CASE WHEN notes IS NULL OR notes = '' THEN 'System auto-checkout at End of Day'                      ELSE notes || ' · System auto-checkout at End of Day' END,         updated_at = CURRENT_TIMESTAMP     WHERE id = ANY(p_ids); END; $procedure$ ;

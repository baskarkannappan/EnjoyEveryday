-- Function: public.sp_close_end_of_day_teachers
-- Backup generated on 2026-11-11 09:05:41

CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_teachers(IN p_clock_out time without time zone, IN p_ids uuid[])  LANGUAGE plpgsql AS $procedure$ BEGIN     CALL public.sp_close_end_of_day_teachers(p_clock_out::interval, p_ids); END; $procedure$ ;

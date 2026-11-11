-- Function: public.sp_close_end_of_day_children
-- Backup generated on 2026-11-11 09:05:41

CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_children(IN p_departure time without time zone, IN p_user_id uuid, IN p_ids uuid[])  LANGUAGE plpgsql AS $procedure$ BEGIN     CALL public.sp_close_end_of_day_children(p_departure::interval, p_user_id, p_ids); END; $procedure$ ;

-- Function: public.sp_close_end_of_day_activities
-- Backup generated on 2026-11-11 09:05:40

CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_activities(IN p_tenant_id uuid, IN p_schedule_ids uuid[], IN p_end_time time without time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     CALL public.sp_close_end_of_day_activities(p_tenant_id, p_schedule_ids, p_end_time::interval); END; $procedure$ ;

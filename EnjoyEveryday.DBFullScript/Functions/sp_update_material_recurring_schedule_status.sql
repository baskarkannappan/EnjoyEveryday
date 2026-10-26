-- Function: public.sp_update_material_recurring_schedule_status
-- Backup generated on 2026-10-25 20:34:12

CREATE OR REPLACE PROCEDURE public.sp_update_material_recurring_schedule_status(IN p_schedule_id uuid, IN p_status character varying, IN p_next_run_date timestamp with time zone DEFAULT NULL::timestamp with time zone, IN p_last_run_date timestamp with time zone DEFAULT NULL::timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE material_recurring_schedules SET         status = p_status,         next_run_date = COALESCE(p_next_run_date, next_run_date),         last_run_date = COALESCE(p_last_run_date, last_run_date),         updated_at = NOW()     WHERE id = p_schedule_id; END; $procedure$ ;

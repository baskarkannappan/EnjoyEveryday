-- Function: public.sp_update_event_task
-- Backup generated on 2026-10-25 20:34:09

CREATE OR REPLACE PROCEDURE public.sp_update_event_task(IN p_id uuid, IN p_description character varying, IN p_owner_id uuid, IN p_due_date timestamp with time zone, IN p_is_completed boolean, IN p_completed_at timestamp with time zone, IN p_updated_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE event_tasks      SET description = p_description, owner_id = p_owner_id, due_date = p_due_date,          is_completed = p_is_completed, completed_at = p_completed_at, updated_at = p_updated_at     WHERE id = p_id; END; $procedure$ ;

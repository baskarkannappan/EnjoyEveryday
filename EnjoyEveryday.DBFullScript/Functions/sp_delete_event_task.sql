-- Function: public.sp_delete_event_task
-- Backup generated on 2026-11-11 09:05:48

CREATE OR REPLACE PROCEDURE public.sp_delete_event_task(IN p_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM event_tasks WHERE id = p_id; END; $procedure$ ;

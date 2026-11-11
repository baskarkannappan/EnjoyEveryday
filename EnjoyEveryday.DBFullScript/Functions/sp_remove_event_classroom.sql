-- Function: public.sp_remove_event_classroom
-- Backup generated on 2026-11-11 09:05:56

CREATE OR REPLACE PROCEDURE public.sp_remove_event_classroom(IN p_event_id uuid, IN p_classroom_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM event_classrooms WHERE event_id = p_event_id AND classroom_id = p_classroom_id; END; $procedure$ ;

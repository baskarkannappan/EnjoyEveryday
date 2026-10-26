-- Function: public.sp_add_event_classroom
-- Backup generated on 2026-10-25 20:33:48

CREATE OR REPLACE PROCEDURE public.sp_add_event_classroom(IN p_event_id uuid, IN p_classroom_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO event_classrooms (event_id, classroom_id)      VALUES (p_event_id, p_classroom_id)     ON CONFLICT DO NOTHING; END; $procedure$ ;

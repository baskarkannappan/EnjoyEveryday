-- Function: public.sp_remove_event_experience
-- Backup generated on 2026-10-25 20:34:05

CREATE OR REPLACE PROCEDURE public.sp_remove_event_experience(IN p_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM event_experiences WHERE id = p_id; END; $procedure$ ;

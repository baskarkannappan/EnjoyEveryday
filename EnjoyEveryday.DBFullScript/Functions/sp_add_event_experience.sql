-- Function: public.sp_add_event_experience
-- Backup generated on 2026-10-25 20:33:48

CREATE OR REPLACE PROCEDURE public.sp_add_event_experience(IN p_id uuid, IN p_event_id uuid, IN p_experience_id uuid, IN p_experience_schedule_id uuid, IN p_experience_session_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO event_experiences (id, event_id, experience_id, experience_schedule_id, experience_session_id)     VALUES (p_id, p_event_id, p_experience_id, p_experience_schedule_id, p_experience_session_id); END; $procedure$ ;

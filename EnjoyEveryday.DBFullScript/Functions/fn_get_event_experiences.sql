-- Function: public.fn_get_event_experiences
-- Backup generated on 2026-10-25 20:33:17

CREATE OR REPLACE FUNCTION public.fn_get_event_experiences(p_event_id uuid)  RETURNS TABLE(id uuid, event_id uuid, experience_id uuid, experience_schedule_id uuid, experience_session_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ee.id, ee.event_id, ee.experience_id, ee.experience_schedule_id, ee.experience_session_id     FROM event_experiences ee     WHERE ee.event_id = p_event_id; END; $function$ ;

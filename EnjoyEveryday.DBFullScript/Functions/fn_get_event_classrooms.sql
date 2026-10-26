-- Function: public.fn_get_event_classrooms
-- Backup generated on 2026-10-25 20:33:17

CREATE OR REPLACE FUNCTION public.fn_get_event_classrooms(p_event_id uuid)  RETURNS TABLE(classroom_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ec.classroom_id FROM event_classrooms ec WHERE ec.event_id = p_event_id; END; $function$ ;

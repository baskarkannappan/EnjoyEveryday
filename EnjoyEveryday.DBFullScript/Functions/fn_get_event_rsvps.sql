-- Function: public.fn_get_event_rsvps
-- Backup generated on 2026-10-25 20:33:18

CREATE OR REPLACE FUNCTION public.fn_get_event_rsvps(p_event_id uuid)  RETURNS TABLE(id uuid, event_id uuid, child_id uuid, guardian_id uuid, status character varying, notes text, created_at timestamp with time zone, updated_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT er.id, er.event_id, er.child_id, er.guardian_id, er.status, er.notes, er.created_at, er.updated_at     FROM event_rsvps er     WHERE er.event_id = p_event_id; END; $function$ ;

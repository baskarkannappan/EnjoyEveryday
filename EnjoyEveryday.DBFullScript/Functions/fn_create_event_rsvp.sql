-- Function: public.fn_create_event_rsvp
-- Backup generated on 2026-10-25 20:32:54

CREATE OR REPLACE FUNCTION public.fn_create_event_rsvp(p_id uuid, p_event_id uuid, p_child_id uuid, p_guardian_id uuid, p_status character varying, p_notes text, p_created_at timestamp with time zone, p_updated_at timestamp with time zone)  RETURNS uuid  LANGUAGE plpgsql AS $function$ BEGIN     INSERT INTO event_rsvps (id, event_id, child_id, guardian_id, status, notes, created_at, updated_at)     VALUES (p_id, p_event_id, p_child_id, p_guardian_id, p_status, p_notes, p_created_at, p_updated_at);     RETURN p_id; END; $function$ ;

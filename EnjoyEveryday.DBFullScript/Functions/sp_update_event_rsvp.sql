-- Function: public.sp_update_event_rsvp
-- Backup generated on 2026-10-25 20:34:09

CREATE OR REPLACE PROCEDURE public.sp_update_event_rsvp(IN p_id uuid, IN p_status character varying, IN p_notes text, IN p_updated_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE event_rsvps      SET status = p_status, notes = p_notes, updated_at = p_updated_at     WHERE id = p_id; END; $procedure$ ;

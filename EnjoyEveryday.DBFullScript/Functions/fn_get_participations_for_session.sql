-- Function: public.fn_get_participations_for_session
-- Backup generated on 2026-10-25 20:33:35

CREATE OR REPLACE FUNCTION public.fn_get_participations_for_session(p_session_id uuid)  RETURNS TABLE(id uuid, experience_session_id uuid, child_id uuid, participation_status character varying, participation_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ep.id, ep.experience_session_id,             ep.child_id, ep.participation_status,             ep.participation_notes,             ep.created_at, ep.updated_at     FROM experience_participations ep     WHERE ep.experience_session_id = p_session_id; END; $function$ ;

-- Function: public.sp_delete_participations_for_session
-- Backup generated on 2026-10-25 20:33:59

CREATE OR REPLACE PROCEDURE public.sp_delete_participations_for_session(IN p_session_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM experience_participations WHERE experience_session_id = p_session_id; END; $procedure$ ;

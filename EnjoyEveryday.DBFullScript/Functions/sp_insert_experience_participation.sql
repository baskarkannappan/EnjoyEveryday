-- Function: public.sp_insert_experience_participation
-- Backup generated on 2026-10-25 20:34:02

CREATE OR REPLACE PROCEDURE public.sp_insert_experience_participation(IN p_id uuid, IN p_experience_session_id uuid, IN p_child_id uuid, IN p_participation_status character varying, IN p_participation_notes text)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO experience_participations (         id, experience_session_id, child_id, participation_status, participation_notes     ) VALUES (         p_id, p_experience_session_id, p_child_id, p_participation_status, p_participation_notes     ); END; $procedure$ ;

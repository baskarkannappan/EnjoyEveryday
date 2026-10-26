-- Function: public.sp_update_experience_feedback
-- Backup generated on 2026-10-25 20:34:10

CREATE OR REPLACE PROCEDURE public.sp_update_experience_feedback(IN p_id uuid, IN p_tenant_id uuid, IN p_stars integer, IN p_rating character varying, IN p_notes text)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE experience_feedback     SET stars = p_stars,         rating = p_rating,         notes = p_notes     WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;

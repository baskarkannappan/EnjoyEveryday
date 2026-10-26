-- Function: public.sp_insert_experience_feedback
-- Backup generated on 2026-10-25 20:34:02

CREATE OR REPLACE PROCEDURE public.sp_insert_experience_feedback(IN p_id uuid, IN p_tenant_id uuid, IN p_experience_schedule_id uuid, IN p_experience_id uuid, IN p_teacher_id uuid, IN p_stars integer, IN p_rating character varying, IN p_notes text, IN p_created_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO experience_feedback (id, tenant_id, experience_schedule_id, experience_id, teacher_id, stars, rating, notes, created_at)     VALUES (p_id, p_tenant_id, p_experience_schedule_id, p_experience_id, p_teacher_id, p_stars, p_rating, p_notes, p_created_at); END; $procedure$ ;

-- Function: public.fn_get_all_experience_feedbacks
-- Backup generated on 2026-10-25 20:32:59

CREATE OR REPLACE FUNCTION public.fn_get_all_experience_feedbacks(p_tenant_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, experience_schedule_id uuid, experience_id uuid, teacher_id uuid, stars integer, rating character varying, notes text, created_at timestamp with time zone)  LANGUAGE plpgsql  STABLE AS $function$ BEGIN     RETURN QUERY     SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,            ef.teacher_id, ef.stars, ef.rating::VARCHAR, ef.notes, ef.created_at     FROM experience_feedback ef     WHERE ef.tenant_id = p_tenant_id     ORDER BY ef.created_at DESC; END; $function$ ;

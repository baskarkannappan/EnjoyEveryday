-- Function: public.fn_get_experience_feedback_by_schedule_id
-- Backup generated on 2026-10-25 20:33:20

CREATE OR REPLACE FUNCTION public.fn_get_experience_feedback_by_schedule_id(p_tenant_id uuid, p_schedule_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, experience_schedule_id uuid, experience_id uuid, teacher_id uuid, stars integer, rating integer, notes text, created_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,            ef.teacher_id, ef.stars, ef.rating, ef.notes, ef.created_at     FROM experience_feedback ef     WHERE ef.experience_schedule_id = p_schedule_id AND ef.tenant_id = p_tenant_id     ORDER BY ef.created_at DESC; END; $function$ ;

-- Function: public.fn_get_experience_feedback_by_id
-- Backup generated on 2026-10-25 20:33:19

CREATE OR REPLACE FUNCTION public.fn_get_experience_feedback_by_id(p_tenant_id uuid, p_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, experience_schedule_id uuid, experience_id uuid, teacher_id uuid, stars integer, rating integer, notes text, created_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,            ef.teacher_id, ef.stars, ef.rating, ef.notes, ef.created_at     FROM experience_feedback ef     WHERE ef.id = p_id AND ef.tenant_id = p_tenant_id; END; $function$ ;

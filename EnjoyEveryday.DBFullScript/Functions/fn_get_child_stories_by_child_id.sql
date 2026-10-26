-- Function: public.fn_get_child_stories_by_child_id
-- Backup generated on 2026-10-25 20:33:10

CREATE OR REPLACE FUNCTION public.fn_get_child_stories_by_child_id(p_tenant_id uuid, p_child_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, child_id uuid, teacher_id uuid, experience_schedule_id uuid, content text, status character varying, created_at timestamp with time zone, updated_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT cs.id, cs.tenant_id, cs.child_id, cs.teacher_id,            cs.experience_schedule_id, cs.content,            cs.status, cs.created_at, cs.updated_at     FROM child_stories cs     WHERE cs.child_id = p_child_id AND cs.tenant_id = p_tenant_id     ORDER BY cs.created_at DESC; END; $function$ ;

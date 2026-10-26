-- Function: public.fn_get_branch_room_stats
-- Backup generated on 2026-10-25 20:33:06

CREATE OR REPLACE FUNCTION public.fn_get_branch_room_stats(p_tenant_id uuid, p_branch_id uuid)  RETURNS TABLE(room_count integer, total_cap integer)  LANGUAGE sql  STABLE AS $function$     SELECT          COUNT(*)::int AS room_count,         COALESCE(SUM(capacity), 0)::int AS total_cap     FROM classrooms     WHERE tenant_id = p_tenant_id AND branch_id = p_branch_id; $function$ ;

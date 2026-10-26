-- Function: public.fn_get_parent_approved_milestones
-- Backup generated on 2026-10-25 20:33:34

CREATE OR REPLACE FUNCTION public.fn_get_parent_approved_milestones(p_child_id uuid)  RETURNS SETOF child_milestones  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM child_milestones     WHERE child_id = p_child_id        AND parent_visible = true     ORDER BY milestone_date DESC; $function$ ;

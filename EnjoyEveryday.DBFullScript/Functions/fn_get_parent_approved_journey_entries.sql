-- Function: public.fn_get_parent_approved_journey_entries
-- Backup generated on 2026-10-25 20:33:34

CREATE OR REPLACE FUNCTION public.fn_get_parent_approved_journey_entries(p_child_id uuid)  RETURNS SETOF child_journey_entries  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM child_journey_entries     WHERE child_id = p_child_id        AND is_parent_visible = true     ORDER BY observation_date DESC; $function$ ;

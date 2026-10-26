-- Function: public.fn_get_development_areas_for_journey_entry
-- Backup generated on 2026-10-25 20:33:14

CREATE OR REPLACE FUNCTION public.fn_get_development_areas_for_journey_entry(p_journey_entry_id uuid)  RETURNS TABLE(development_area_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT cjd.development_area_id     FROM child_journey_developments cjd     WHERE cjd.journey_entry_id = p_journey_entry_id; END; $function$ ;

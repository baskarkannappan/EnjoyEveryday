-- Function: public.fn_get_portfolio_entry_development_areas
-- Backup generated on 2026-10-25 20:33:39

CREATE OR REPLACE FUNCTION public.fn_get_portfolio_entry_development_areas(p_tenant_id uuid, p_child_id uuid)  RETURNS TABLE(journey_entry_id uuid, development_area_id uuid, name character varying, icon character varying)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT d.journey_entry_id, d.development_area_id,            a.name::VARCHAR, a.icon::VARCHAR     FROM child_journey_developments d     JOIN development_areas a ON d.development_area_id = a.id     WHERE d.journey_entry_id IN (         SELECT cje.id FROM child_journey_entries cje WHERE cje.tenant_id = p_tenant_id AND cje.child_id = p_child_id     ); END; $function$ ;

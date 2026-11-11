-- Function: public.fn_get_active_master_lookups
-- Backup generated on 2026-11-11 09:04:37

CREATE OR REPLACE FUNCTION public.fn_get_active_master_lookups(p_category character varying DEFAULT NULL::character varying)  RETURNS TABLE(id uuid, category character varying, code character varying, display_name character varying, display_order integer, is_active boolean, metadata_json jsonb)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ml.id, ml.category, ml.code, ml.display_name, ml.display_order, ml.is_active, ml.metadata_json     FROM public.master_lookups ml     WHERE ml.is_active = true       AND (p_category IS NULL OR ml.category = p_category)     ORDER BY ml.category, ml.display_order, ml.display_name; END; $function$ ;

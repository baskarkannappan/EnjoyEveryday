-- Function: public.fn_get_floor_map_markers_by_map_ids
-- Backup generated on 2026-10-25 20:33:24

CREATE OR REPLACE FUNCTION public.fn_get_floor_map_markers_by_map_ids(p_map_ids uuid[])  RETURNS TABLE(id uuid, floor_map_id uuid, marker_type character varying, label character varying, instructions text, normalized_x double precision, normalized_y double precision, display_order integer, created_at timestamp with time zone)  LANGUAGE sql  STABLE AS $function$     SELECT id, floor_map_id, marker_type, label, instructions, normalized_x, normalized_y, display_order, created_at     FROM floor_map_markers     WHERE floor_map_id = ANY(p_map_ids)     ORDER BY display_order ASC; $function$ ;

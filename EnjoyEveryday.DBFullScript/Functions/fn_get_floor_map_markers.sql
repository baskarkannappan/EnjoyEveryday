-- Function: public.fn_get_floor_map_markers
-- Backup generated on 2026-10-25 20:33:24

CREATE OR REPLACE FUNCTION public.fn_get_floor_map_markers(p_floor_map_id uuid)  RETURNS TABLE(id uuid, floor_map_id uuid, marker_type character varying, label character varying, instructions text, normalized_x double precision, normalized_y double precision, display_order integer, created_at timestamp with time zone)  LANGUAGE sql  STABLE AS $function$     SELECT id, floor_map_id, marker_type, label, instructions, normalized_x, normalized_y, display_order, created_at     FROM floor_map_markers     WHERE floor_map_id = p_floor_map_id     ORDER BY display_order ASC, created_at ASC; $function$ ;

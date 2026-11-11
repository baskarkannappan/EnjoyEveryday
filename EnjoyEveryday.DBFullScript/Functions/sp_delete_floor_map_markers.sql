-- Function: public.sp_delete_floor_map_markers
-- Backup generated on 2026-11-11 09:05:49

CREATE OR REPLACE PROCEDURE public.sp_delete_floor_map_markers(IN p_floor_map_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM floor_map_markers WHERE floor_map_id = p_floor_map_id; END; $procedure$ ;

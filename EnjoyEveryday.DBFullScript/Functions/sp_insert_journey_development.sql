-- Function: public.sp_insert_journey_development
-- Backup generated on 2026-11-11 09:05:54

CREATE OR REPLACE PROCEDURE public.sp_insert_journey_development(IN p_journey_entry_id uuid, IN p_development_area_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO child_journey_developments (journey_entry_id, development_area_id)      VALUES (p_journey_entry_id, p_development_area_id); END; $procedure$ ;

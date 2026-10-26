-- Function: public.sp_delete_journey_developments
-- Backup generated on 2026-10-25 20:33:59

CREATE OR REPLACE PROCEDURE public.sp_delete_journey_developments(IN p_journey_entry_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM child_journey_developments WHERE journey_entry_id = p_journey_entry_id; END; $procedure$ ;

-- Function: public.sp_close_end_of_day_events
-- Backup generated on 2026-10-25 20:33:49

CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_events(IN p_ids uuid[])  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE events     SET status = 'Completed',         description = CASE WHEN description IS NULL OR description = '' THEN 'System Auto-Closed at End of Day'                            WHEN description LIKE '%System Auto-Closed%' THEN description                            ELSE description || ' [System Auto-Closed]' END,         updated_at = CURRENT_TIMESTAMP     WHERE id = ANY(p_ids); END; $procedure$ ;

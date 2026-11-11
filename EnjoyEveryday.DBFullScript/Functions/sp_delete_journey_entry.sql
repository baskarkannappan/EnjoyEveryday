-- Function: public.sp_delete_journey_entry
-- Backup generated on 2026-11-11 09:05:50

CREATE OR REPLACE PROCEDURE public.sp_delete_journey_entry(IN p_tenant_id uuid, IN p_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM child_journey_entries WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;

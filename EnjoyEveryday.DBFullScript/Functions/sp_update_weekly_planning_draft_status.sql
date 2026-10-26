-- Function: public.sp_update_weekly_planning_draft_status
-- Backup generated on 2026-10-25 20:34:14

CREATE OR REPLACE PROCEDURE public.sp_update_weekly_planning_draft_status(IN p_tenant_id uuid, IN p_draft_id uuid, IN p_status character varying)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE weekly_planning_draft_requests SET         status = p_status,         updated_at = CURRENT_TIMESTAMP     WHERE id = p_draft_id AND tenant_id = p_tenant_id; END; $procedure$ ;

-- Function: public.sp_update_branch_operational_goal_status
-- Backup generated on 2026-10-25 20:34:07

CREATE OR REPLACE PROCEDURE public.sp_update_branch_operational_goal_status(IN p_tenant_id uuid, IN p_goal_id uuid, IN p_status character varying, IN p_current_value character varying)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE branch_operational_goals     SET status = p_status,         current_value = COALESCE(p_current_value, current_value),         updated_at = NOW()     WHERE tenant_id = p_tenant_id AND id = p_goal_id; END; $procedure$ ;

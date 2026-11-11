-- Function: public.sp_revoke_user_branch
-- Backup generated on 2026-11-11 09:05:57

CREATE OR REPLACE PROCEDURE public.sp_revoke_user_branch(IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_user_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE user_branch_assignments     SET revoked_at = NOW()     WHERE user_id = p_user_id       AND branch_id = p_branch_id       AND organization_id = p_organization_id       AND tenant_id = p_tenant_id; END; $procedure$ ;

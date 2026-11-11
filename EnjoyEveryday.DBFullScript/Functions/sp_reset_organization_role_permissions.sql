-- Function: public.sp_reset_organization_role_permissions
-- Backup generated on 2026-11-11 09:05:57

CREATE OR REPLACE PROCEDURE public.sp_reset_organization_role_permissions(IN p_tenant_id uuid, IN p_organization_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM organization_role_permissions     WHERE tenant_id = p_tenant_id       AND organization_id = p_organization_id; END; $procedure$ ;

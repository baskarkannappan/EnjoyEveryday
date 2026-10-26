-- Function: public.fn_has_active_membership
-- Backup generated on 2026-10-25 20:33:45

CREATE OR REPLACE FUNCTION public.fn_has_active_membership(p_tenant_id uuid, p_organization_id uuid, p_user_id uuid)  RETURNS boolean  LANGUAGE plpgsql AS $function$ BEGIN     RETURN EXISTS(         SELECT 1 FROM organization_memberships m         JOIN users u ON u.id = m.user_id         WHERE m.tenant_id = p_tenant_id AND m.organization_id = p_organization_id AND m.user_id = p_user_id           AND m.status = 'Active' AND u.is_active = true     ); END; $function$ ;

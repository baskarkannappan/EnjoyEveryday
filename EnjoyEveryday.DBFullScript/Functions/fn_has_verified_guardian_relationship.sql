-- Function: public.fn_has_verified_guardian_relationship
-- Backup generated on 2026-10-25 20:33:46

CREATE OR REPLACE FUNCTION public.fn_has_verified_guardian_relationship(p_tenant_id uuid, p_user_id uuid, p_child_id uuid)  RETURNS boolean  LANGUAGE plpgsql AS $function$ BEGIN     RETURN EXISTS(         SELECT 1 FROM family_relationships fr         JOIN children c ON c.id = fr.child_id AND c.tenant_id = fr.tenant_id         JOIN users u ON u.id = fr.user_id AND u.tenant_id = fr.tenant_id         WHERE fr.tenant_id = p_tenant_id AND fr.user_id = p_user_id AND fr.child_id = p_child_id           AND fr.status = 'Verified' AND fr.revoked_at IS NULL           AND c.is_active = true AND u.is_active = true     ); END; $function$ ;

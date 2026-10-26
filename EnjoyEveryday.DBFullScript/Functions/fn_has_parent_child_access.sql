-- Function: public.fn_has_parent_child_access
-- Backup generated on 2026-10-25 20:33:46

CREATE OR REPLACE FUNCTION public.fn_has_parent_child_access(p_tenant_id uuid, p_user_id uuid, p_child_id uuid)  RETURNS boolean  LANGUAGE plpgsql AS $function$ BEGIN     RETURN EXISTS(         SELECT 1          FROM family_relationships fr         JOIN children c ON fr.child_id = c.id         WHERE fr.user_id = p_user_id            AND fr.child_id = p_child_id           AND fr.status = 'Verified' AND fr.revoked_at IS NULL           AND c.is_active = true     ); END; $function$ ;

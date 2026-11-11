-- Function: public.fn_get_user_assigned_branches
-- Backup generated on 2026-11-11 09:05:34

CREATE OR REPLACE FUNCTION public.fn_get_user_assigned_branches(p_user_id uuid, p_organization_id uuid)  RETURNS TABLE(id uuid, name character varying, location character varying, is_active boolean)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT b.id, b.name::VARCHAR, b.location::VARCHAR, b.is_active     FROM user_branch_assignments uba     JOIN branches b ON uba.branch_id = b.id     WHERE uba.user_id = p_user_id       AND uba.organization_id = p_organization_id       AND uba.revoked_at IS NULL       AND b.is_active = true     ORDER BY b.name; END; $function$ ;

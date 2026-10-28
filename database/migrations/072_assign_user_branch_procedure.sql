CREATE OR REPLACE PROCEDURE public.sp_assign_user_branch(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_user_id UUID,
    p_assigned_by UUID DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO user_branch_assignments (user_id, branch_id, organization_id, tenant_id, assigned_by, created_at, revoked_at)
    VALUES (p_user_id, p_branch_id, p_organization_id, p_tenant_id, p_assigned_by, NOW(), NULL)
    ON CONFLICT (user_id, branch_id) DO UPDATE
    SET revoked_at = NULL,
        assigned_by = COALESCE(p_assigned_by, user_branch_assignments.assigned_by);
END;
$$;

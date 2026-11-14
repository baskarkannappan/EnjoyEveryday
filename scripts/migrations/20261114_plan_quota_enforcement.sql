-- Migration: Plan Quota Capacity Enforcement

-- 1. Add plan quota columns to organizations table
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS plan_id UUID DEFAULT '22222222-2222-2222-2222-222222222222';
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS plan_code VARCHAR(50) DEFAULT 'PLAN-BLOOM';
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS plan_name VARCHAR(100) DEFAULT 'Bloom (Growth)';
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS max_branches INT DEFAULT 2;
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS max_classrooms INT DEFAULT 8;
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS max_children INT DEFAULT 90;
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS includes_ai BOOLEAN DEFAULT true;

-- 2. Stored procedure to update organization plan quotas
CREATE OR REPLACE PROCEDURE public.sp_update_organization_plan_quota(
    p_org_id UUID,
    p_plan_id UUID,
    p_plan_code VARCHAR,
    p_plan_name VARCHAR,
    p_max_branches INT,
    p_max_classrooms INT,
    p_max_children INT,
    p_includes_ai BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE organizations
    SET plan_id = p_plan_id,
        plan_code = p_plan_code,
        plan_name = p_plan_name,
        max_branches = p_max_branches,
        max_classrooms = p_max_classrooms,
        max_children = p_max_children,
        includes_ai = p_includes_ai,
        updated_at = NOW()
    WHERE id = p_org_id;
END;
$$;

-- 3. Stored function to query organization quota usage
CREATE OR REPLACE FUNCTION public.fn_get_organization_quota_usage(p_org_id UUID)
RETURNS TABLE(
    organization_id UUID,
    plan_id UUID,
    plan_code VARCHAR,
    plan_name VARCHAR,
    max_branches INT,
    current_branches INT,
    max_classrooms INT,
    current_classrooms INT,
    max_children INT,
    current_children INT,
    includes_ai BOOLEAN
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        o.id AS organization_id,
        COALESCE(o.plan_id, '22222222-2222-2222-2222-222222222222'::UUID) AS plan_id,
        COALESCE(o.plan_code, 'PLAN-BLOOM')::VARCHAR AS plan_code,
        COALESCE(o.plan_name, 'Bloom (Growth)')::VARCHAR AS plan_name,
        COALESCE(o.max_branches, 2)::INT AS max_branches,
        (SELECT COUNT(*)::INT FROM branches b WHERE b.organization_id = o.id AND b.is_active = TRUE) AS current_branches,
        COALESCE(o.max_classrooms, 8)::INT AS max_classrooms,
        (SELECT COUNT(*)::INT FROM classrooms c JOIN branches b ON c.branch_id = b.id WHERE b.organization_id = o.id AND c.is_active = TRUE) AS current_classrooms,
        COALESCE(o.max_children, 90)::INT AS max_children,
        (SELECT COUNT(*)::INT FROM children ch JOIN classrooms c ON ch.classroom_id = c.id JOIN branches b ON c.branch_id = b.id WHERE b.organization_id = o.id AND ch.is_active = TRUE) AS current_children,
        COALESCE(o.includes_ai, TRUE)::BOOLEAN AS includes_ai
    FROM organizations o
    WHERE o.id = p_org_id;
$$;

-- 4. Set NEW ORG to Sprout (Starter) quotas if exists
UPDATE organizations
SET plan_id = '11111111-1111-1111-1111-111111111111',
    plan_code = 'PLAN-SPROUT',
    plan_name = 'Sprout (Starter)',
    max_branches = 1,
    max_classrooms = 3,
    max_children = 35,
    includes_ai = FALSE
WHERE name ILIKE '%NEW ORG%';


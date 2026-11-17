-- Migration 128: Return tariff_name and monthly_rate from fn_get_platform_organizations_overview
-- Fixes organizations overview grid showing hardcoded ₹299 instead of the organization's regional plan/tariff rate.

DROP FUNCTION IF EXISTS public.fn_get_platform_organizations_overview();

CREATE OR REPLACE FUNCTION public.fn_get_platform_organizations_overview()
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    account_status VARCHAR,
    suspension_reason TEXT,
    created_at TIMESTAMPTZ,
    branch_count INT,
    admin_email VARCHAR,
    plan_id UUID,
    plan_code VARCHAR,
    plan_name VARCHAR,
    max_branches INT,
    max_classrooms INT,
    max_children INT,
    includes_ai BOOLEAN,
    pricing_region_id UUID,
    pricing_region_code VARCHAR,
    pricing_region_name VARCHAR,
    tariff_id UUID,
    tariff_name VARCHAR,
    monthly_rate NUMERIC(12,2),
    currency VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT o.id AS id, o.tenant_id AS tenant_id, o.name AS name,
           o.location AS location, o.is_active AS is_active,
           COALESCE(o.account_status, CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END)::varchar AS account_status,
           o.suspension_reason,
           o.created_at AS created_at,
           COALESCE((SELECT COUNT(*) FROM branches b WHERE b.organization_id = o.id), 0)::int AS branch_count,
           (SELECT u.email FROM organization_memberships om JOIN users u ON om.user_id = u.id WHERE om.organization_id = o.id AND om.status = 'Active' ORDER BY om.created_at LIMIT 1)::varchar AS admin_email,
           o.plan_id,
           COALESCE(o.plan_code, 'PLAN-BLOOM')::varchar AS plan_code,
           COALESCE(o.plan_name, 'Bloom (Growth)')::varchar AS plan_name,
           COALESCE(o.max_branches, 2)::int AS max_branches,
           COALESCE(o.max_classrooms, 8)::int AS max_classrooms,
           COALESCE(o.max_children, 90)::int AS max_children,
           COALESCE(o.includes_ai, TRUE)::boolean AS includes_ai,
           o.pricing_region_id,
           o.pricing_region_code::varchar,
           o.pricing_region_name::varchar,
           o.tariff_id,
           COALESCE(rpt.name, rpt_def.name, 'Monthly')::varchar AS tariff_name,
           COALESCE(rpt.price, rpt_def.price, 299.00)::numeric(12,2) AS monthly_rate,
           COALESCE(o.currency, 'INR')::varchar AS currency
    FROM organizations o
    LEFT JOIN region_plan_tariffs rpt ON rpt.id = o.tariff_id
    LEFT JOIN region_plan_tariffs rpt_def ON rpt_def.region_id = o.pricing_region_id 
                                         AND rpt_def.plan_id = o.plan_id 
                                         AND rpt_def.frequency = 'Monthly'
    ORDER BY o.name;
END;
$$;


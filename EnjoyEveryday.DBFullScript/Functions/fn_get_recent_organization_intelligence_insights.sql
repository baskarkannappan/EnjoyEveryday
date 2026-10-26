-- Function: public.fn_get_recent_organization_intelligence_insights
-- Backup generated on 2026-10-25 20:33:41

CREATE OR REPLACE FUNCTION public.fn_get_recent_organization_intelligence_insights(p_tenant_id uuid, p_organization_id uuid, p_branch_id uuid, p_classroom_id uuid, p_reporting_period character varying)  RETURNS SETOF organization_intelligence_insights  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM organization_intelligence_insights     WHERE tenant_id = p_tenant_id       AND organization_id = p_organization_id       AND (p_branch_id IS NULL OR branch_id = p_branch_id)       AND (p_classroom_id IS NULL OR classroom_id = p_classroom_id)       AND reporting_period = p_reporting_period     ORDER BY created_at DESC     LIMIT 10; $function$ ;

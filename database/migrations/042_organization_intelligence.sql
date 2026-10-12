-- ============================================================
-- EnjoyEveryday — Migration 042: Organization Intelligence
-- Stores organization, branch, and classroom operational insights,
-- metric snapshots, evidence strength indicators, and audit records.
-- ============================================================

CREATE TABLE IF NOT EXISTS organization_intelligence_insights (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    branch_id UUID REFERENCES branches(id) ON DELETE CASCADE,
    classroom_id UUID REFERENCES classrooms(id) ON DELETE CASCADE,
    scope_type VARCHAR(50) NOT NULL, -- Organization, Branch, Classroom
    reporting_period VARCHAR(50) NOT NULL, -- Last7Days, Last30Days, ThisMonth
    category VARCHAR(50) NOT NULL, -- OperationalOverview, CurriculumVariety, PlanningGaps, DataQuality, Engagement
    title VARCHAR(255) NOT NULL,
    observation TEXT NOT NULL,
    supporting_metrics_json JSONB,
    possible_explanation TEXT,
    suggested_next_step TEXT,
    evidence_strength VARCHAR(50) NOT NULL DEFAULT 'Strong', -- Strong, Limited, Insufficient
    data_limitations TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_org_intel_tenant_org ON organization_intelligence_insights(tenant_id, organization_id);
CREATE INDEX IF NOT EXISTS idx_org_intel_category ON organization_intelligence_insights(tenant_id, category);
CREATE INDEX IF NOT EXISTS idx_org_intel_created ON organization_intelligence_insights(tenant_id, created_at DESC);

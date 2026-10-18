-- 058_branch_benchmarking.sql
-- Multi-Branch Benchmarking, Health Scorecards, and Operational Improvement Goals

CREATE TABLE IF NOT EXISTS branch_operational_goals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    organization_id UUID REFERENCES organizations(id),
    branch_id UUID REFERENCES branches(id),
    title VARCHAR(200) NOT NULL,
    category VARCHAR(50) NOT NULL DEFAULT 'Enrollment', -- Enrollment, StaffingRatios, Attendance, Financial, QualityCurriculum
    target_metric VARCHAR(100),
    current_value VARCHAR(50),
    target_value VARCHAR(50),
    status VARCHAR(50) NOT NULL DEFAULT 'InProgress', -- InProgress, Completed, AtRisk
    target_date DATE,
    owner_name VARCHAR(100),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_branch_goals_tenant_org ON branch_operational_goals(tenant_id, organization_id);
CREATE INDEX IF NOT EXISTS idx_branch_goals_branch ON branch_operational_goals(branch_id);

-- Seed realistic operational improvement goals for standard branches
INSERT INTO branch_operational_goals (
    id, tenant_id, organization_id, branch_id, title, category, target_metric, current_value, target_value, status, target_date, owner_name
)
SELECT 
    '11111111-b111-4000-8000-000000000001'::uuid,
    t.id,
    o.id,
    b.id,
    'Achieve 90% Infant Classroom Capacity across Nestlings room',
    'Enrollment',
    'Enrollment Occupancy',
    '80%',
    '90%',
    'InProgress',
    CURRENT_DATE + INTERVAL '30 days',
    'Director Sarah Jenkins'
FROM tenants t
JOIN organizations o ON o.tenant_id = t.id
JOIN branches b ON b.tenant_id = t.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;

INSERT INTO branch_operational_goals (
    id, tenant_id, organization_id, branch_id, title, category, target_metric, current_value, target_value, status, target_date, owner_name
)
SELECT 
    '22222222-b222-4000-8000-000000000002'::uuid,
    t.id,
    o.id,
    b.id,
    'Eliminate afternoon teacher-to-child ratio variances through float dispatch',
    'StaffingRatios',
    'Ratio Compliance Rate',
    '92%',
    '100%',
    'InProgress',
    CURRENT_DATE + INTERVAL '14 days',
    'Head Teacher Marcus Cole'
FROM tenants t
JOIN organizations o ON o.tenant_id = t.id
JOIN branches b ON b.tenant_id = t.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;

INSERT INTO branch_operational_goals (
    id, tenant_id, organization_id, branch_id, title, category, target_metric, current_value, target_value, status, target_date, owner_name
)
SELECT 
    '33333333-b333-4000-8000-000000000003'::uuid,
    t.id,
    o.id,
    b.id,
    'Ensure 100% of enrolled children have development evidence logged in all 5 domains',
    'QualityCurriculum',
    'Domain Coverage Completeness',
    '88%',
    '100%',
    'Completed',
    CURRENT_DATE - INTERVAL '3 days',
    'Curriculum Lead Maya Peterson'
FROM tenants t
JOIN organizations o ON o.tenant_id = t.id
JOIN branches b ON b.tenant_id = t.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;

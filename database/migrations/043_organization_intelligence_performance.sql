-- ============================================================
-- EnjoyEveryday — Migration 043: Organization Intelligence Performance & Metadata
-- Adds composite indexes for scoped reporting and distinguishes deterministic vs AI observations
-- ============================================================

-- 1. Metadata columns on insights table
ALTER TABLE organization_intelligence_insights 
ADD COLUMN IF NOT EXISTS source_type VARCHAR(50) NOT NULL DEFAULT 'DeterministicRule',
ADD COLUMN IF NOT EXISTS is_ai_generated BOOLEAN NOT NULL DEFAULT FALSE;

-- 2. Performance indexes for attendance aggregations
CREATE INDEX IF NOT EXISTS idx_ca_tenant_branch_date 
ON child_attendance(tenant_id, branch_id, attendance_date);

CREATE INDEX IF NOT EXISTS idx_ca_tenant_classroom_date 
ON child_attendance(tenant_id, classroom_id, attendance_date);

-- 3. Performance index for experience schedule aggregations
CREATE INDEX IF NOT EXISTS idx_es_tenant_date_classroom 
ON experience_schedules(tenant_id, scheduled_date, classroom_id);

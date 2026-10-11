-- ============================================================
-- EnjoyEveryday — Migration 031: Event Scope & Branch Association
-- Adds: scope ('Organization', 'Branch', 'Classroom') and branch_id to events
-- ============================================================

ALTER TABLE events 
ADD COLUMN IF NOT EXISTS scope VARCHAR(30) NOT NULL DEFAULT 'Classroom',
ADD COLUMN IF NOT EXISTS branch_id UUID REFERENCES branches(id) ON DELETE SET NULL;

-- Migrate any existing records where is_organization_wide = true
UPDATE events SET scope = 'Organization' WHERE is_organization_wide = TRUE;

CREATE INDEX IF NOT EXISTS idx_events_scope ON events(tenant_id, scope);
CREATE INDEX IF NOT EXISTS idx_events_branch ON events(branch_id);

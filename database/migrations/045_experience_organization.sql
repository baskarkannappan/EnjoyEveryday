-- Migration 045: Experience Organization Isolation
-- Links experiences to a specific organization so they are not globally shared across organizations.

ALTER TABLE experiences ADD COLUMN IF NOT EXISTS organization_id UUID REFERENCES organizations(id);
CREATE INDEX IF NOT EXISTS idx_experiences_org ON experiences(organization_id);

-- Backfill existing experiences to the default daycare organization
UPDATE experiences 
SET organization_id = '20000000-0000-0000-0000-000000000001'
WHERE organization_id IS NULL;

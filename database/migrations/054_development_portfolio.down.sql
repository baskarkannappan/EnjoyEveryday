-- 054_development_portfolio.down.sql
-- Rollback for Development Portfolio & Standard Domains

DROP INDEX IF EXISTS idx_journey_entries_portfolio;
DROP INDEX IF EXISTS idx_child_milestones_portfolio;

DELETE FROM development_areas 
WHERE tenant_id IS NULL 
  AND id IN (
    'a0000000-0000-0000-0000-000000000001',
    'a0000000-0000-0000-0000-000000000002',
    'a0000000-0000-0000-0000-000000000003',
    'a0000000-0000-0000-0000-000000000004',
    'a0000000-0000-0000-0000-000000000005'
  );

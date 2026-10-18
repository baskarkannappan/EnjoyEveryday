-- ============================================================
-- EnjoyEveryday - Migration 054: Development Portfolio & Standard Domains
-- Seeds standard early-childhood developmental domains and indexes for portfolio
-- ============================================================

-- 1. Seed global standard developmental domains (tenant_id IS NULL so they are available to all tenants)
INSERT INTO development_areas (id, tenant_id, name, description, icon, display_order, is_active)
SELECT 'a0000000-0000-0000-0000-000000000001', NULL, 'Language & Communication', 'Speech, vocabulary, listening comprehension, storytelling, and early phonological awareness.', 'bi-chat-quote', 10, TRUE
WHERE NOT EXISTS (SELECT 1 FROM development_areas WHERE name = 'Language & Communication' AND tenant_id IS NULL);

INSERT INTO development_areas (id, tenant_id, name, description, icon, display_order, is_active)
SELECT 'a0000000-0000-0000-0000-000000000002', NULL, 'Social & Emotional', 'Self-regulation, empathy, peer cooperation, emotional expression, and relationship building.', 'bi-people', 20, TRUE
WHERE NOT EXISTS (SELECT 1 FROM development_areas WHERE name = 'Social & Emotional' AND tenant_id IS NULL);

INSERT INTO development_areas (id, tenant_id, name, description, icon, display_order, is_active)
SELECT 'a0000000-0000-0000-0000-000000000003', NULL, 'Physical & Motor', 'Gross motor coordination, fine motor dexterity, spatial balance, and sensory-motor skills.', 'bi-activity', 30, TRUE
WHERE NOT EXISTS (SELECT 1 FROM development_areas WHERE name = 'Physical & Motor' AND tenant_id IS NULL);

INSERT INTO development_areas (id, tenant_id, name, description, icon, display_order, is_active)
SELECT 'a0000000-0000-0000-0000-000000000004', NULL, 'Cognitive & Problem Solving', 'Curiosity, spatial reasoning, cause-and-effect inquiry, pattern recognition, and focus.', 'bi-lightbulb', 40, TRUE
WHERE NOT EXISTS (SELECT 1 FROM development_areas WHERE name = 'Cognitive & Problem Solving' AND tenant_id IS NULL);

INSERT INTO development_areas (id, tenant_id, name, description, icon, display_order, is_active)
SELECT 'a0000000-0000-0000-0000-000000000005', NULL, 'Creative Arts & Sensory', 'Musical expression, imaginative play, tactile exploration, color and texture experimentation.', 'bi-palette', 50, TRUE
WHERE NOT EXISTS (SELECT 1 FROM development_areas WHERE name = 'Creative Arts & Sensory' AND tenant_id IS NULL);

-- 2. Index for fast portfolio timeline filtering by child and parent visibility
CREATE INDEX IF NOT EXISTS idx_journey_entries_portfolio 
ON child_journey_entries(tenant_id, child_id, is_parent_visible, observation_date DESC);

CREATE INDEX IF NOT EXISTS idx_child_milestones_portfolio 
ON child_milestones(tenant_id, child_id, parent_visible, milestone_date DESC);

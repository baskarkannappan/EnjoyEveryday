-- ============================================================
-- EnjoyEveryday — Migration 025: Child Journey Session FKs
-- Adds experience_session_id and experience_participation_id to child_journey_entries
-- ============================================================

ALTER TABLE child_journey_entries
ADD COLUMN experience_session_id UUID REFERENCES experience_sessions(id) ON DELETE SET NULL,
ADD COLUMN experience_participation_id UUID REFERENCES experience_participations(id) ON DELETE SET NULL;

CREATE INDEX idx_child_journey_session ON child_journey_entries(experience_session_id);
CREATE INDEX idx_child_journey_participation ON child_journey_entries(experience_participation_id);

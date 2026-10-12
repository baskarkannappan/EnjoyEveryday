-- Migration 039: Child Journey Insights (Phase 2)
-- Enables AI-assisted, evidence-grounded child progress summaries and reflections.

CREATE TABLE IF NOT EXISTS child_journey_insight_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    classroom_id UUID NOT NULL,
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    requested_by_user_id UUID NOT NULL,
    period_type VARCHAR(50) NOT NULL DEFAULT 'Last30Days', -- 'Last7Days', 'Last30Days', 'CurrentMonth', 'Custom'
    period_start_date DATE NOT NULL,
    period_end_date DATE NOT NULL,
    evidence_count INT NOT NULL DEFAULT 0,
    overall_summary TEXT,
    evidence_gaps TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_journey_insight_requests_tenant_child 
    ON child_journey_insight_requests(tenant_id, child_id, created_at DESC);

CREATE TABLE IF NOT EXISTS child_journey_insight_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id UUID NOT NULL REFERENCES child_journey_insight_requests(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL,
    category VARCHAR(50) NOT NULL, -- 'JourneySummary', 'RecordedInterests', 'ParticipationPatterns', 'LearningOpportunities', 'IdeasToConsider'
    classification VARCHAR(50) NOT NULL DEFAULT 'RecordedFact', -- 'RecordedFact', 'ObservedPattern', 'EducationalSuggestion'
    statement TEXT NOT NULL,
    evidence_strength VARCHAR(50) NOT NULL DEFAULT 'Supported', -- 'Strong', 'Supported', 'Emerging', 'Limited'
    supporting_evidence JSONB, -- Array of { sourceId, sourceType, title, summary }
    suggested_experience_id UUID,
    suggested_experience_title VARCHAR(255),
    rank_order INT NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_journey_insight_items_request 
    ON child_journey_insight_items(request_id, category, rank_order);

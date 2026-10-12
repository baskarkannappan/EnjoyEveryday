-- Migration 038: AI Activity Recommendation Engine Audit & Decision Tracking

CREATE TABLE IF NOT EXISTS activity_recommendation_requests (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    classroom_id UUID NOT NULL,
    child_id UUID,
    requested_by_user_id UUID NOT NULL,
    request_timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    scope VARCHAR(50) NOT NULL, -- 'Classroom' or 'Child'
    context_preferences JSONB, -- duration, indoor/outdoor, materials, learning areas
    evidence_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS activity_recommendation_items (
    id UUID PRIMARY KEY,
    request_id UUID NOT NULL REFERENCES activity_recommendation_requests(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL,
    experience_id UUID NOT NULL REFERENCES experiences(id) ON DELETE CASCADE,
    experience_title VARCHAR(255) NOT NULL,
    suggested_format VARCHAR(100),
    estimated_duration_minutes INT,
    rationale TEXT NOT NULL,
    supporting_evidence JSONB, -- Array of source references (SourceId, SourceType, EventDate, Title)
    materials JSONB, -- Array of required materials
    rank_order INT NOT NULL DEFAULT 1,
    is_evidence_limited BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS activity_recommendation_decisions (
    id UUID PRIMARY KEY,
    request_id UUID NOT NULL REFERENCES activity_recommendation_requests(id) ON DELETE CASCADE,
    recommendation_item_id UUID NOT NULL REFERENCES activity_recommendation_items(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL,
    decided_by_user_id UUID NOT NULL,
    decision VARCHAR(50) NOT NULL, -- 'Accepted', 'Edited', 'Rejected'
    rejection_reason TEXT,
    resulting_schedule_id UUID REFERENCES experience_schedules(id) ON DELETE SET NULL,
    decided_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_rec_req_classroom ON activity_recommendation_requests(tenant_id, classroom_id);
CREATE INDEX IF NOT EXISTS idx_rec_req_child ON activity_recommendation_requests(tenant_id, child_id);
CREATE INDEX IF NOT EXISTS idx_rec_items_request ON activity_recommendation_items(request_id);
CREATE INDEX IF NOT EXISTS idx_rec_decisions_req ON activity_recommendation_decisions(request_id);

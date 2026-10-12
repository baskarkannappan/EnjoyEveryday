-- Migration 040: AI Weekly Planning Assistant (Phase 3)
-- Enables AI-assisted, editable weekly planning drafts grounded in Experience Library & Classroom Memory.

CREATE TABLE IF NOT EXISTS weekly_planning_draft_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    classroom_id UUID NOT NULL REFERENCES classrooms(id) ON DELETE CASCADE,
    requested_by_user_id UUID NOT NULL,
    week_start_date DATE NOT NULL,
    week_end_date DATE NOT NULL,
    focus_areas VARCHAR(255),
    indoor_outdoor_preference VARCHAR(50) DEFAULT 'Balanced', -- 'Indoor', 'Outdoor', 'Balanced'
    materials_notes TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'Draft', -- 'Draft', 'Saved', 'Approved', 'Cancelled'
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_weekly_planning_requests_tenant_classroom 
    ON weekly_planning_draft_requests(tenant_id, classroom_id, week_start_date DESC);

CREATE TABLE IF NOT EXISTS weekly_planning_draft_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id UUID NOT NULL REFERENCES weekly_planning_draft_requests(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL,
    scheduled_date DATE NOT NULL,
    day_of_week VARCHAR(20) NOT NULL, -- 'Monday', 'Tuesday', etc.
    time_of_day VARCHAR(50) NOT NULL DEFAULT 'Morning', -- 'Morning', 'Afternoon'
    experience_id UUID NOT NULL REFERENCES experiences(id) ON DELETE RESTRICT,
    experience_title VARCHAR(255) NOT NULL,
    learning_goal TEXT,
    group_format VARCHAR(50) DEFAULT 'Small Group', -- 'Individual', 'Small Group', 'Whole Class'
    estimated_duration_minutes INT DEFAULT 25,
    required_materials TEXT,
    rationale TEXT,
    validation_status VARCHAR(50) NOT NULL DEFAULT 'Valid', -- 'Valid', 'Warning', 'Conflict'
    validation_notes TEXT,
    resulting_schedule_id UUID REFERENCES experience_schedules(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_weekly_planning_items_request 
    ON weekly_planning_draft_items(request_id, scheduled_date);

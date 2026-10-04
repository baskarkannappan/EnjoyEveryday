-- ============================================================
-- EnjoyEveryday - Migration 020: Child Journey
-- Creates: development_areas, child_journey_entries, child_journey_developments,
--          child_journey_evidence, child_milestones
-- ============================================================

CREATE TABLE IF NOT EXISTS development_areas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    icon VARCHAR(100),
    display_order INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);
CREATE INDEX IF NOT EXISTS idx_development_areas_tenant ON development_areas(tenant_id);

CREATE TABLE IF NOT EXISTS child_journey_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    enrollment_id UUID,
    classroom_id UUID REFERENCES classrooms(id) ON DELETE SET NULL,
    teacher_id UUID NOT NULL REFERENCES teachers(id),
    experience_id UUID REFERENCES experiences(id) ON DELETE SET NULL,
    experience_schedule_id UUID REFERENCES experience_schedules(id) ON DELETE SET NULL,
    little_moment_id UUID,
    milestone_id UUID,
    journey_type VARCHAR(100) NOT NULL,
    title VARCHAR(255),
    observation TEXT NOT NULL,
    child_voice TEXT,
    teacher_reflection TEXT,
    observation_date TIMESTAMPTZ NOT NULL,
    participation_type VARCHAR(100),
    is_milestone BOOLEAN NOT NULL DEFAULT FALSE,
    is_parent_visible BOOLEAN NOT NULL DEFAULT FALSE,
    parent_visibility_status VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by UUID,
    version INT NOT NULL DEFAULT 1
);
CREATE INDEX IF NOT EXISTS idx_journey_entries_tenant ON child_journey_entries(tenant_id);
CREATE INDEX IF NOT EXISTS idx_journey_entries_child ON child_journey_entries(child_id);
CREATE INDEX IF NOT EXISTS idx_journey_entries_teacher ON child_journey_entries(teacher_id);
CREATE INDEX IF NOT EXISTS idx_journey_entries_classroom ON child_journey_entries(classroom_id);

CREATE TABLE IF NOT EXISTS child_journey_developments (
    journey_entry_id UUID NOT NULL REFERENCES child_journey_entries(id) ON DELETE CASCADE,
    development_area_id UUID NOT NULL REFERENCES development_areas(id) ON DELETE CASCADE,
    PRIMARY KEY (journey_entry_id, development_area_id)
);

CREATE TABLE IF NOT EXISTS child_journey_evidence (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    journey_entry_id UUID NOT NULL REFERENCES child_journey_entries(id) ON DELETE CASCADE,
    evidence_type VARCHAR(100) NOT NULL,
    media_id UUID,
    text_content TEXT,
    captured_at TIMESTAMPTZ,
    captured_by UUID,
    visibility VARCHAR(50) NOT NULL DEFAULT 'InternalOnly',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_journey_evidence_journey ON child_journey_evidence(journey_entry_id);

CREATE TABLE IF NOT EXISTS child_milestones (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    classroom_id UUID REFERENCES classrooms(id) ON DELETE SET NULL,
    teacher_id UUID NOT NULL REFERENCES teachers(id),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    milestone_date TIMESTAMPTZ NOT NULL,
    development_area_id UUID REFERENCES development_areas(id) ON DELETE SET NULL,
    evidence_summary TEXT,
    journey_entry_id UUID REFERENCES child_journey_entries(id) ON DELETE SET NULL,
    parent_visible BOOLEAN NOT NULL DEFAULT FALSE,
    status VARCHAR(50) NOT NULL DEFAULT 'Active',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by UUID
);
CREATE INDEX IF NOT EXISTS idx_child_milestones_tenant ON child_milestones(tenant_id);
CREATE INDEX IF NOT EXISTS idx_child_milestones_child ON child_milestones(child_id);

-- ============================================================
-- EnjoyEveryday — Migration 030: Celebrations & Events
-- Creates: event_types, events, event_classrooms, event_experiences, event_tasks, event_rsvps
-- Updates: child_journey_entries (adds event_id)
-- ============================================================

CREATE TABLE IF NOT EXISTS event_types (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    name            VARCHAR(100) NOT NULL,
    description     TEXT,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_event_types_tenant ON event_types(tenant_id);

CREATE TABLE IF NOT EXISTS events (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    title           VARCHAR(200) NOT NULL,
    description     TEXT,
    event_type_id   UUID REFERENCES event_types(id),
    event_date      DATE NOT NULL,
    start_time      TIME,
    end_time        TIME,
    location        VARCHAR(255),
    organizer_id    UUID REFERENCES users(id),
    status          VARCHAR(50) NOT NULL DEFAULT 'Draft',
    is_organization_wide BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_events_tenant ON events(tenant_id);
CREATE INDEX idx_events_date ON events(event_date);

CREATE TABLE IF NOT EXISTS event_classrooms (
    event_id        UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    classroom_id    UUID NOT NULL REFERENCES classrooms(id) ON DELETE CASCADE,
    PRIMARY KEY (event_id, classroom_id)
);

CREATE TABLE IF NOT EXISTS event_experiences (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id                UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    experience_id           UUID NOT NULL REFERENCES experiences(id),
    experience_schedule_id  UUID REFERENCES experience_schedules(id),
    experience_session_id   UUID REFERENCES experience_sessions(id)
);

CREATE INDEX idx_event_experiences_event ON event_experiences(event_id);

CREATE TABLE IF NOT EXISTS event_tasks (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id        UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    description     TEXT NOT NULL,
    owner_id        UUID REFERENCES users(id),
    due_date        TIMESTAMPTZ,
    is_completed    BOOLEAN NOT NULL DEFAULT FALSE,
    completed_at    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_event_tasks_event ON event_tasks(event_id);

CREATE TABLE IF NOT EXISTS event_rsvps (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id        UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    child_id        UUID NOT NULL REFERENCES children(id),
    guardian_id     UUID NOT NULL REFERENCES users(id),
    status          VARCHAR(50) NOT NULL DEFAULT 'Pending',
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (event_id, child_id)
);

CREATE INDEX idx_event_rsvps_event ON event_rsvps(event_id);

-- Alter existing table child_journey_entries
ALTER TABLE child_journey_entries 
ADD COLUMN IF NOT EXISTS event_id UUID REFERENCES events(id) ON DELETE SET NULL;

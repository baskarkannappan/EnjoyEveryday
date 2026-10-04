-- ============================================================
-- EnjoyEveryday — Migration 024: Experience Sessions
-- Creates: experience_sessions, experience_session_teachers, experience_participations
-- ============================================================

CREATE TABLE IF NOT EXISTS experience_sessions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    experience_id   UUID NOT NULL REFERENCES experiences(id),
    experience_schedule_id UUID REFERENCES experience_schedules(id) ON DELETE SET NULL,
    classroom_id    UUID NOT NULL REFERENCES classrooms(id),
    session_date    DATE NOT NULL,
    start_time      TIME,
    end_time        TIME,
    status          VARCHAR(50) NOT NULL DEFAULT 'Planned',
    primary_teacher_id UUID REFERENCES users(id),
    actual_notes    TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_experience_sessions_tenant ON experience_sessions(tenant_id);
CREATE INDEX idx_experience_sessions_classroom ON experience_sessions(classroom_id);
CREATE INDEX idx_experience_sessions_schedule ON experience_sessions(experience_schedule_id);

CREATE TABLE IF NOT EXISTS experience_session_teachers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    experience_session_id UUID NOT NULL REFERENCES experience_sessions(id) ON DELETE CASCADE,
    teacher_id      UUID NOT NULL REFERENCES users(id),
    role            VARCHAR(50) NOT NULL DEFAULT 'LeadTeacher',
    UNIQUE (experience_session_id, teacher_id)
);

CREATE INDEX idx_exp_sess_teachers_session ON experience_session_teachers(experience_session_id);

CREATE TABLE IF NOT EXISTS experience_participations (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    experience_session_id UUID NOT NULL REFERENCES experience_sessions(id) ON DELETE CASCADE,
    child_id        UUID NOT NULL REFERENCES children(id),
    participation_status VARCHAR(50) NOT NULL DEFAULT 'Participated',
    participation_notes TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (experience_session_id, child_id)
);

CREATE INDEX idx_exp_participations_session ON experience_participations(experience_session_id);
CREATE INDEX idx_exp_participations_child ON experience_participations(child_id);

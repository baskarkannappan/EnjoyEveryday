-- ============================================================
-- EnjoyEveryday — Migration 003: Experiences
-- Creates: experiences, experience_versions
-- ============================================================

CREATE TABLE IF NOT EXISTS experiences (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    title           VARCHAR(200) NOT NULL,
    description     TEXT,
    status          VARCHAR(50) NOT NULL DEFAULT 'Idea',
    dna_payload     JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_experiences_tenant ON experiences(tenant_id);
CREATE INDEX idx_experiences_status ON experiences(status);

CREATE TABLE IF NOT EXISTS experience_versions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    experience_id   UUID NOT NULL REFERENCES experiences(id) ON DELETE CASCADE,
    version_number  INT NOT NULL,
    title           VARCHAR(200) NOT NULL,
    description     TEXT,
    dna_payload     JSONB NOT NULL DEFAULT '{}'::jsonb,
    change_reason   VARCHAR(500),
    modified_by_user_id UUID NOT NULL REFERENCES users(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (experience_id, version_number)
);

CREATE INDEX idx_exp_versions_exp ON experience_versions(experience_id);

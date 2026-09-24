-- ============================================================
-- EnjoyEveryday — Migration 002: People
-- Creates: children, family_relationships, classroom_teachers
-- ============================================================

-- CHILDREN
CREATE TABLE IF NOT EXISTS children (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    first_name      VARCHAR(100) NOT NULL,
    last_name       VARCHAR(100) NOT NULL,
    date_of_birth   DATE,
    classroom_id    UUID REFERENCES classrooms(id),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_children_tenant ON children(tenant_id);
CREATE INDEX idx_children_classroom ON children(classroom_id);

-- FAMILY RELATIONSHIPS
CREATE TABLE IF NOT EXISTS family_relationships (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    relationship    VARCHAR(50) NOT NULL,
    is_primary      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (child_id, user_id)
);

CREATE INDEX idx_family_relationships_tenant ON family_relationships(tenant_id);

-- CLASSROOM TEACHERS
CREATE TABLE IF NOT EXISTS classroom_teachers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    classroom_id    UUID NOT NULL REFERENCES classrooms(id) ON DELETE CASCADE,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    is_primary      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (classroom_id, user_id)
);

CREATE INDEX idx_classroom_teachers_tenant ON classroom_teachers(tenant_id);

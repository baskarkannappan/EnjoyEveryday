-- ============================================================
-- EnjoyEveryday — Migration 001: Foundation Tables
-- Creates: tenants, organizations, branches, classrooms,
--          users (ASP.NET Identity compatible), roles, audit_log
-- ============================================================

-- TENANTS
CREATE TABLE IF NOT EXISTS tenants (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(200) NOT NULL,
    slug            VARCHAR(100) NOT NULL UNIQUE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ORGANIZATIONS
CREATE TABLE IF NOT EXISTS organizations (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    name            VARCHAR(200) NOT NULL,
    location        VARCHAR(300),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_organizations_tenant ON organizations(tenant_id);

-- BRANCHES
CREATE TABLE IF NOT EXISTS branches (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    name            VARCHAR(200) NOT NULL,
    location        VARCHAR(300),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_branches_tenant ON branches(tenant_id);
CREATE INDEX idx_branches_organization ON branches(organization_id);

-- CLASSROOMS
CREATE TABLE IF NOT EXISTS classrooms (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    branch_id       UUID NOT NULL REFERENCES branches(id),
    name            VARCHAR(200) NOT NULL,
    age_group       VARCHAR(20),
    capacity        INT,
    environment     VARCHAR(50) DEFAULT 'Indoor',
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_classrooms_tenant ON classrooms(tenant_id);
CREATE INDEX idx_classrooms_branch ON classrooms(branch_id);

-- ROLES
CREATE TABLE IF NOT EXISTS roles (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID REFERENCES tenants(id), -- NULL = system role
    name            VARCHAR(100) NOT NULL,
    description     VARCHAR(500),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- USERS (ASP.NET Identity compatible structure)
CREATE TABLE IF NOT EXISTS users (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               UUID NOT NULL REFERENCES tenants(id),
    email                   VARCHAR(256) NOT NULL,
    normalized_email        VARCHAR(256) NOT NULL,
    email_confirmed         BOOLEAN NOT NULL DEFAULT FALSE,
    password_hash           TEXT,
    security_stamp          VARCHAR(256),
    concurrency_stamp       VARCHAR(256),
    phone_number            VARCHAR(50),
    phone_number_confirmed  BOOLEAN NOT NULL DEFAULT FALSE,
    two_factor_enabled      BOOLEAN NOT NULL DEFAULT FALSE,
    lockout_end             TIMESTAMPTZ,
    lockout_enabled         BOOLEAN NOT NULL DEFAULT TRUE,
    access_failed_count     INT NOT NULL DEFAULT 0,
    first_name              VARCHAR(100) NOT NULL,
    last_name               VARCHAR(100) NOT NULL,
    display_name            VARCHAR(200),
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_users_email ON users(normalized_email);
CREATE INDEX idx_users_tenant ON users(tenant_id);

-- USER ROLES
CREATE TABLE IF NOT EXISTS user_roles (
    user_id         UUID NOT NULL REFERENCES users(id),
    role_id         UUID NOT NULL REFERENCES roles(id),
    PRIMARY KEY (user_id, role_id)
);

-- PERMISSIONS
CREATE TABLE IF NOT EXISTS permissions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(100) NOT NULL UNIQUE,
    description     VARCHAR(500)
);

-- ROLE PERMISSIONS
CREATE TABLE IF NOT EXISTS role_permissions (
    role_id         UUID NOT NULL REFERENCES roles(id),
    permission_id   UUID NOT NULL REFERENCES permissions(id),
    PRIMARY KEY (role_id, permission_id)
);

-- AUDIT LOG
CREATE TABLE IF NOT EXISTS audit_log (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID REFERENCES tenants(id),
    user_id         UUID REFERENCES users(id),
    action          VARCHAR(200) NOT NULL,
    entity_type     VARCHAR(100),
    entity_id       UUID,
    details         JSONB,
    ip_address      VARCHAR(45),
    timestamp       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_log_tenant ON audit_log(tenant_id);
CREATE INDEX idx_audit_log_timestamp ON audit_log(timestamp);
CREATE INDEX idx_audit_log_entity ON audit_log(entity_type, entity_id);

-- SEED SYSTEM ROLES
INSERT INTO roles (id, tenant_id, name, description) VALUES
    ('a0000000-0000-0000-0000-000000000001', NULL, 'PlatformAdmin', 'Platform-level administrator'),
    ('a0000000-0000-0000-0000-000000000002', NULL, 'OrganizationOwner', 'Organization owner'),
    ('a0000000-0000-0000-0000-000000000003', NULL, 'OrganizationAdmin', 'Organization administrator'),
    ('a0000000-0000-0000-0000-000000000004', NULL, 'BranchAdmin', 'Branch administrator'),
    ('a0000000-0000-0000-0000-000000000005', NULL, 'Teacher', 'Teacher / Experience Facilitator'),
    ('a0000000-0000-0000-0000-000000000006', NULL, 'AssistantTeacher', 'Assistant Teacher'),
    ('a0000000-0000-0000-0000-000000000007', NULL, 'Parent', 'Parent / Family member')
ON CONFLICT DO NOTHING;

-- SEED PERMISSIONS
INSERT INTO permissions (id, name, description) VALUES
    ('b0000000-0000-0000-0000-000000000001', 'tenant.manage', 'Create and manage tenants'),
    ('b0000000-0000-0000-0000-000000000002', 'organization.manage', 'Manage organization settings'),
    ('b0000000-0000-0000-0000-000000000003', 'branch.manage', 'Manage branches'),
    ('b0000000-0000-0000-0000-000000000004', 'classroom.manage', 'Manage classrooms'),
    ('b0000000-0000-0000-0000-000000000005', 'user.manage', 'Manage users'),
    ('b0000000-0000-0000-0000-000000000006', 'experience.create', 'Create experiences'),
    ('b0000000-0000-0000-0000-000000000007', 'experience.approve', 'Approve experiences'),
    ('b0000000-0000-0000-0000-000000000008', 'experience.schedule', 'Schedule experiences'),
    ('b0000000-0000-0000-0000-000000000009', 'experience.execute', 'Start and complete experiences'),
    ('b0000000-0000-0000-0000-000000000010', 'child.manage', 'Manage children'),
    ('b0000000-0000-0000-0000-000000000011', 'child.observe', 'Record observations'),
    ('b0000000-0000-0000-0000-000000000012', 'family.view', 'View family portal'),
    ('b0000000-0000-0000-0000-000000000013', 'safety.manage', 'Manage safety and trust'),
    ('b0000000-0000-0000-0000-000000000014', 'ai.use', 'Use AI features'),
    ('b0000000-0000-0000-0000-000000000015', 'audit.view', 'View audit logs')
ON CONFLICT DO NOTHING;

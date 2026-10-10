-- ============================================================
-- EnjoyEveryday — Migration 029: Multi-Organization Memberships
-- Creates: organization_memberships, user_organization_roles, role scopes
-- ============================================================



-- 1. Clean up any invalid data before constraints
DELETE FROM user_roles WHERE user_id NOT IN (SELECT id FROM users);
DELETE FROM user_roles WHERE role_id NOT IN (SELECT id FROM roles);

-- 2. Add Unique Constraints for Composite Foreign Keys
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uk_users_tenant') THEN
        ALTER TABLE users ADD CONSTRAINT uk_users_tenant UNIQUE (id, tenant_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uk_organizations_tenant') THEN
        ALTER TABLE organizations ADD CONSTRAINT uk_organizations_tenant UNIQUE (id, tenant_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uk_roles_tenant') THEN
        ALTER TABLE roles ADD CONSTRAINT uk_roles_tenant UNIQUE (id, tenant_id);
    END IF;
END $$;

-- 3. Add ScopeType to roles
ALTER TABLE roles ADD COLUMN IF NOT EXISTS scope_type VARCHAR(20) NOT NULL DEFAULT 'Organization';
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_roles_scope') THEN
        ALTER TABLE roles ADD CONSTRAINT chk_roles_scope CHECK (scope_type IN ('Platform', 'Tenant', 'Organization'));
    END IF;
END $$;

-- Update system roles
UPDATE roles SET scope_type = 'Platform' WHERE tenant_id IS NULL AND name = 'PlatformAdmin';
UPDATE roles SET scope_type = 'Tenant' WHERE name IN ('OrganizationOwner', 'OrganizationAdmin');

-- 4. Organization Memberships Table
CREATE TABLE IF NOT EXISTS organization_memberships (
    user_id             UUID NOT NULL,
    organization_id     UUID NOT NULL,
    tenant_id           UUID NOT NULL,
    status              VARCHAR(50) NOT NULL DEFAULT 'Active',
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, organization_id),
    CONSTRAINT uk_org_memberships_tenant UNIQUE (user_id, organization_id, tenant_id),
    FOREIGN KEY (user_id, tenant_id) REFERENCES users(id, tenant_id) ON DELETE CASCADE,
    FOREIGN KEY (organization_id, tenant_id) REFERENCES organizations(id, tenant_id) ON DELETE CASCADE
);

-- 5. Organization-Scoped Roles Table
CREATE TABLE IF NOT EXISTS user_organization_roles (
    user_id             UUID NOT NULL,
    role_id             UUID NOT NULL,
    organization_id     UUID NOT NULL,
    tenant_id           UUID NOT NULL,
    PRIMARY KEY (user_id, role_id, organization_id),
    FOREIGN KEY (user_id, organization_id, tenant_id) REFERENCES organization_memberships(user_id, organization_id, tenant_id) ON DELETE CASCADE,
    FOREIGN KEY (role_id, tenant_id) REFERENCES roles(id, tenant_id) ON DELETE CASCADE
);

-- 6. Pre-Flight Mapping & Safe Migration
CREATE TEMP TABLE safe_mappings AS
SELECT ur.user_id, ur.role_id, o.id as organization_id, u.tenant_id
FROM user_roles ur
JOIN users u ON ur.user_id = u.id
JOIN roles r ON ur.role_id = r.id
JOIN organizations o ON o.tenant_id = u.tenant_id
WHERE r.scope_type = 'Organization'
  AND u.tenant_id = r.tenant_id
  AND EXISTS (SELECT 1 FROM tenants t WHERE t.id = u.tenant_id AND (SELECT COUNT(*) FROM organizations WHERE tenant_id = t.id) = 1);

-- 7. Insert Memberships
INSERT INTO organization_memberships (user_id, organization_id, tenant_id)
SELECT DISTINCT user_id, organization_id, tenant_id FROM safe_mappings
ON CONFLICT DO NOTHING;

-- 8. Insert Scoped Roles
INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
SELECT user_id, role_id, organization_id, tenant_id FROM safe_mappings
ON CONFLICT DO NOTHING;

-- 9. Validate Reconciliation
DO $$
DECLARE
    expected_count INT;
    actual_count INT;
BEGIN
    SELECT COUNT(*) INTO expected_count FROM safe_mappings;
    SELECT COUNT(*) INTO actual_count FROM user_organization_roles;
    
    IF expected_count != actual_count THEN
        RAISE EXCEPTION 'Reconciliation failed: Expected % migrations, but got %', expected_count, actual_count;
    END IF;
END $$;

-- 10. Delete safely migrated assignments from legacy table
DELETE FROM user_roles ur
USING safe_mappings vm
WHERE ur.user_id = vm.user_id AND ur.role_id = vm.role_id;



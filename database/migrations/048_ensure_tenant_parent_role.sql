-- ============================================================
-- EnjoyEveryday — Migration 048: Ensure Tenant-Scoped Parent Role
-- Ensures every tenant has an Organization-scoped 'Parent' role
-- for user_organization_roles composite foreign key compatibility.
-- ============================================================

INSERT INTO roles (id, tenant_id, name, description, scope_type)
SELECT gen_random_uuid(), id, 'Parent', 'Parent or guardian with family portal access', 'Organization'
FROM tenants t
WHERE NOT EXISTS (
    SELECT 1 FROM roles r WHERE r.tenant_id = t.id AND r.name = 'Parent'
);

-- ============================================================
-- EnjoyEveryday — Migration 076: Operational Roles & Lead Teacher
-- Adds the canonical LeadTeacher system template, tenant roles,
-- and classroom coordination permissions.
-- ============================================================

-- 1. System Role Template (tenant_id IS NULL)
INSERT INTO roles (id, tenant_id, name, description, scope_type)
VALUES (
    'a0000000-0000-0000-0000-000000000010'::uuid,
    NULL,
    'LeadTeacher',
    'Lead Teacher coordinating classroom delivery and supporting the teaching team',
    'Organization'
)
ON CONFLICT (id) DO UPDATE SET 
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    scope_type = EXCLUDED.scope_type;

-- 2. Tenant-scoped LeadTeacher roles for all active tenants
INSERT INTO roles (id, tenant_id, name, description, scope_type)
SELECT 
    gen_random_uuid(),
    t.id,
    'LeadTeacher',
    'Lead Teacher coordinating classroom delivery and supporting the teaching team',
    'Organization'
FROM tenants t
WHERE NOT EXISTS (
    SELECT 1 FROM roles r WHERE r.tenant_id = t.id AND r.name = 'LeadTeacher'
);

-- 3. Seed Permissions for System LeadTeacher Template
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'a0000000-0000-0000-0000-000000000010'::uuid, p.id
FROM permissions p
WHERE p.name IN (
    -- Core Teaching Permissions
    'ai.use',
    'attendance.record',
    'attendance.view',
    'care.record',
    'care.view',
    'child.observe',
    'child.view',
    'comms.draft',
    'comms.view',
    'consent.view',
    'experience.execute',
    'experience.plan.submit',
    'experience.schedule',
    'experience.view',
    'safety.map.view',
    'safety.report',
    'materials.view',
    -- Lead Teacher Classroom Coordination Permissions
    'care.handover',
    'care.plan.manage',
    'experience.plan.approve',
    'observation.approve',
    'staff.roster.view'
)
ON CONFLICT DO NOTHING;

-- 4. Seed Permissions for Tenant LeadTeacher Roles
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
JOIN permissions p ON p.name IN (
    'ai.use',
    'attendance.record',
    'attendance.view',
    'care.record',
    'care.view',
    'child.observe',
    'child.view',
    'comms.draft',
    'comms.view',
    'consent.view',
    'experience.execute',
    'experience.plan.submit',
    'experience.schedule',
    'experience.view',
    'safety.map.view',
    'safety.report',
    'materials.view',
    'care.handover',
    'care.plan.manage',
    'experience.plan.approve',
    'observation.approve',
    'staff.roster.view'
)
WHERE r.name = 'LeadTeacher' AND r.tenant_id IS NOT NULL
ON CONFLICT DO NOTHING;

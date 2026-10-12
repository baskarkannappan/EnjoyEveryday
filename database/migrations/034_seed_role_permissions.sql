-- ============================================================
-- Migration 034: Seed Role Permissions
-- Ensures admin/owner roles have all management permissions
-- ============================================================

DO $$
DECLARE
    role_rec RECORD;
    perm_rec RECORD;
BEGIN
    -- Grant all permissions to PlatformAdmin, OrganizationOwner, and OrganizationAdmin
    FOR role_rec IN SELECT id, name FROM roles WHERE name IN ('PlatformAdmin', 'OrganizationOwner', 'OrganizationAdmin', 'Daycare Admin')
    LOOP
        FOR perm_rec IN SELECT id FROM permissions
        LOOP
            INSERT INTO role_permissions (role_id, permission_id)
            VALUES (role_rec.id, perm_rec.id)
            ON CONFLICT DO NOTHING;
        END LOOP;
    END LOOP;

    -- Grant branch, classroom, child, and experience permissions to BranchAdmin
    FOR role_rec IN SELECT id FROM roles WHERE name = 'BranchAdmin'
    LOOP
        FOR perm_rec IN SELECT id FROM permissions WHERE name IN ('branch.manage', 'classroom.manage', 'child.manage', 'child.observe', 'experience.create', 'experience.execute', 'experience.schedule', 'ai.use')
        LOOP
            INSERT INTO role_permissions (role_id, permission_id)
            VALUES (role_rec.id, perm_rec.id)
            ON CONFLICT DO NOTHING;
        END LOOP;
    END LOOP;
END $$;

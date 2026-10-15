-- ============================================================
-- EnjoyEveryday — Migration 044: Role & Permission Matrix
-- Reuses: roles, permissions, role_permissions, user_roles (platform only),
--         organization_memberships, user_organization_roles,
--         classroom_teachers, family_relationships, audit_log.
-- Adds (no existing entity could hold these):
--         user_branch_assignments  (branch-admin scope)
--         support_access_grants    (purpose-limited platform support access)
--         family_relationships.status / verified_* / revoked_*
-- The role→permission template below is generated from
-- EnjoyEveryday.Shared.Authorization.RoleTemplates and is checked by
-- RoleTemplateMigrationTests so the two can never drift silently.
-- ============================================================

-- 1. PERMISSION DEFINITIONS --------------------------------------------------
INSERT INTO permissions (name, description) VALUES
    ('platform.configure', 'platform.configure'),
    ('tenant.manage', 'tenant.manage'),
    ('platform.plans', 'platform.plans'),
    ('platform.health', 'platform.health'),
    ('platform.security.view', 'platform.security.view'),
    ('platform.roles.manage', 'platform.roles.manage'),
    ('platform.support.access', 'platform.support.access'),
    ('organization.manage', 'organization.manage'),
    ('branch.manage', 'branch.manage'),
    ('classroom.manage', 'classroom.manage'),
    ('user.manage', 'user.manage'),
    ('role.assign', 'role.assign'),
    ('staff.hr.view', 'staff.hr.view'),
    ('audit.view', 'audit.view'),
    ('child.view', 'child.view'),
    ('child.manage', 'child.manage'),
    ('child.enrollment.manage', 'child.enrollment.manage'),
    ('family.manage', 'family.manage'),
    ('family.verify', 'family.verify'),
    ('family.view', 'family.view'),
    ('experience.view', 'experience.view'),
    ('experience.create', 'experience.create'),
    ('experience.approve', 'experience.approve'),
    ('experience.schedule', 'experience.schedule'),
    ('experience.plan.submit', 'experience.plan.submit'),
    ('experience.plan.approve', 'experience.plan.approve'),
    ('experience.execute', 'experience.execute'),
    ('child.observe', 'child.observe'),
    ('observation.approve', 'observation.approve'),
    ('journey.view.internal', 'journey.view.internal'),
    ('journey.publish', 'journey.publish'),
    ('ai.use', 'ai.use'),
    ('attendance.view', 'attendance.view'),
    ('attendance.record', 'attendance.record'),
    ('attendance.correct', 'attendance.correct'),
    ('attendance.approve', 'attendance.approve'),
    ('care.record', 'care.record'),
    ('care.view', 'care.view'),
    ('care.plan.manage', 'care.plan.manage'),
    ('care.sensitive.manage', 'care.sensitive.manage'),
    ('safety.manage', 'safety.manage'),
    ('safety.map.manage', 'safety.map.manage'),
    ('safety.map.publish', 'safety.map.publish'),
    ('safety.map.view', 'safety.map.view'),
    ('emergency.contacts.manage', 'emergency.contacts.manage'),
    ('safety.report', 'safety.report'),
    ('consent.manage', 'consent.manage'),
    ('consent.issue', 'consent.issue'),
    ('consent.review', 'consent.review'),
    ('consent.view', 'consent.view'),
    ('event.create', 'event.create'),
    ('event.manage', 'event.manage'),
    ('event.approved', 'event.approved'),
    ('event.published', 'event.published'),
    ('event.cancelled', 'event.cancelled'),
    ('comms.draft', 'comms.draft'),
    ('comms.send', 'comms.send'),
    ('comms.approve', 'comms.approve'),
    ('comms.view', 'comms.view'),
    ('insights.org.view', 'insights.org.view'),
    ('insights.branch.view', 'insights.branch.view'),
    ('insights.classroom.view', 'insights.classroom.view'),
    ('insights.org.generate', 'insights.org.generate'),
    ('export.org', 'export.org'),
    ('export.child', 'export.child')
ON CONFLICT (name) DO NOTHING;

-- 2. SYSTEM ROLES (templates; tenant copies are created below) ---------------
INSERT INTO roles (id, tenant_id, name, description, scope_type)
SELECT v.id, NULL, v.name, v.description, 'Organization'
FROM (VALUES
    ('a0000000-0000-0000-0000-000000000008'::uuid, 'DaycareTeacher', 'Daycare Teacher'),
    ('a0000000-0000-0000-0000-000000000009'::uuid, 'ClassroomTeacher', 'Classroom Teacher')
) AS v(id, name, description)
WHERE NOT EXISTS (SELECT 1 FROM roles r WHERE r.tenant_id IS NULL AND r.name = v.name);

UPDATE roles SET scope_type = 'Platform' WHERE tenant_id IS NULL AND name = 'PlatformAdmin';

-- 3. ROLE → PERMISSION TEMPLATE ----------------------------------------------
CREATE OR REPLACE FUNCTION enjoy_role_template()
RETURNS TABLE(role_name text, permission_name text) AS $fn$
    SELECT t.role_name::text, t.permission_name::text FROM (VALUES
        ('PlatformAdmin', 'platform.configure'),
        ('PlatformAdmin', 'platform.health'),
        ('PlatformAdmin', 'platform.plans'),
        ('PlatformAdmin', 'platform.roles.manage'),
        ('PlatformAdmin', 'platform.security.view'),
        ('PlatformAdmin', 'tenant.manage'),
        ('OrganizationOwner', 'ai.use'),
        ('OrganizationOwner', 'attendance.approve'),
        ('OrganizationOwner', 'attendance.correct'),
        ('OrganizationOwner', 'attendance.record'),
        ('OrganizationOwner', 'attendance.view'),
        ('OrganizationOwner', 'audit.view'),
        ('OrganizationOwner', 'branch.manage'),
        ('OrganizationOwner', 'care.plan.manage'),
        ('OrganizationOwner', 'care.view'),
        ('OrganizationOwner', 'child.enrollment.manage'),
        ('OrganizationOwner', 'child.manage'),
        ('OrganizationOwner', 'child.observe'),
        ('OrganizationOwner', 'child.view'),
        ('OrganizationOwner', 'classroom.manage'),
        ('OrganizationOwner', 'comms.approve'),
        ('OrganizationOwner', 'comms.draft'),
        ('OrganizationOwner', 'comms.send'),
        ('OrganizationOwner', 'comms.view'),
        ('OrganizationOwner', 'consent.issue'),
        ('OrganizationOwner', 'consent.manage'),
        ('OrganizationOwner', 'consent.review'),
        ('OrganizationOwner', 'consent.view'),
        ('OrganizationOwner', 'emergency.contacts.manage'),
        ('OrganizationOwner', 'event.approved'),
        ('OrganizationOwner', 'event.cancelled'),
        ('OrganizationOwner', 'event.create'),
        ('OrganizationOwner', 'event.manage'),
        ('OrganizationOwner', 'event.published'),
        ('OrganizationOwner', 'experience.approve'),
        ('OrganizationOwner', 'experience.create'),
        ('OrganizationOwner', 'experience.execute'),
        ('OrganizationOwner', 'experience.plan.approve'),
        ('OrganizationOwner', 'experience.plan.submit'),
        ('OrganizationOwner', 'experience.schedule'),
        ('OrganizationOwner', 'experience.view'),
        ('OrganizationOwner', 'export.child'),
        ('OrganizationOwner', 'export.org'),
        ('OrganizationOwner', 'family.manage'),
        ('OrganizationOwner', 'family.verify'),
        ('OrganizationOwner', 'insights.branch.view'),
        ('OrganizationOwner', 'insights.classroom.view'),
        ('OrganizationOwner', 'insights.org.generate'),
        ('OrganizationOwner', 'insights.org.view'),
        ('OrganizationOwner', 'journey.publish'),
        ('OrganizationOwner', 'journey.view.internal'),
        ('OrganizationOwner', 'observation.approve'),
        ('OrganizationOwner', 'organization.manage'),
        ('OrganizationOwner', 'role.assign'),
        ('OrganizationOwner', 'safety.manage'),
        ('OrganizationOwner', 'safety.map.manage'),
        ('OrganizationOwner', 'safety.map.publish'),
        ('OrganizationOwner', 'safety.map.view'),
        ('OrganizationOwner', 'safety.report'),
        ('OrganizationOwner', 'user.manage'),
        ('OrganizationAdmin', 'ai.use'),
        ('OrganizationAdmin', 'attendance.approve'),
        ('OrganizationAdmin', 'attendance.correct'),
        ('OrganizationAdmin', 'attendance.record'),
        ('OrganizationAdmin', 'attendance.view'),
        ('OrganizationAdmin', 'audit.view'),
        ('OrganizationAdmin', 'branch.manage'),
        ('OrganizationAdmin', 'care.plan.manage'),
        ('OrganizationAdmin', 'care.view'),
        ('OrganizationAdmin', 'child.enrollment.manage'),
        ('OrganizationAdmin', 'child.manage'),
        ('OrganizationAdmin', 'child.observe'),
        ('OrganizationAdmin', 'child.view'),
        ('OrganizationAdmin', 'classroom.manage'),
        ('OrganizationAdmin', 'comms.approve'),
        ('OrganizationAdmin', 'comms.draft'),
        ('OrganizationAdmin', 'comms.send'),
        ('OrganizationAdmin', 'comms.view'),
        ('OrganizationAdmin', 'consent.issue'),
        ('OrganizationAdmin', 'consent.manage'),
        ('OrganizationAdmin', 'consent.review'),
        ('OrganizationAdmin', 'consent.view'),
        ('OrganizationAdmin', 'emergency.contacts.manage'),
        ('OrganizationAdmin', 'event.approved'),
        ('OrganizationAdmin', 'event.cancelled'),
        ('OrganizationAdmin', 'event.create'),
        ('OrganizationAdmin', 'event.manage'),
        ('OrganizationAdmin', 'event.published'),
        ('OrganizationAdmin', 'experience.approve'),
        ('OrganizationAdmin', 'experience.create'),
        ('OrganizationAdmin', 'experience.execute'),
        ('OrganizationAdmin', 'experience.plan.approve'),
        ('OrganizationAdmin', 'experience.plan.submit'),
        ('OrganizationAdmin', 'experience.schedule'),
        ('OrganizationAdmin', 'experience.view'),
        ('OrganizationAdmin', 'export.child'),
        ('OrganizationAdmin', 'export.org'),
        ('OrganizationAdmin', 'family.manage'),
        ('OrganizationAdmin', 'family.verify'),
        ('OrganizationAdmin', 'insights.branch.view'),
        ('OrganizationAdmin', 'insights.classroom.view'),
        ('OrganizationAdmin', 'insights.org.generate'),
        ('OrganizationAdmin', 'insights.org.view'),
        ('OrganizationAdmin', 'journey.publish'),
        ('OrganizationAdmin', 'journey.view.internal'),
        ('OrganizationAdmin', 'observation.approve'),
        ('OrganizationAdmin', 'organization.manage'),
        ('OrganizationAdmin', 'role.assign'),
        ('OrganizationAdmin', 'safety.manage'),
        ('OrganizationAdmin', 'safety.map.manage'),
        ('OrganizationAdmin', 'safety.map.publish'),
        ('OrganizationAdmin', 'safety.map.view'),
        ('OrganizationAdmin', 'safety.report'),
        ('OrganizationAdmin', 'user.manage'),
        ('BranchAdmin', 'ai.use'),
        ('BranchAdmin', 'attendance.approve'),
        ('BranchAdmin', 'attendance.correct'),
        ('BranchAdmin', 'attendance.record'),
        ('BranchAdmin', 'attendance.view'),
        ('BranchAdmin', 'audit.view'),
        ('BranchAdmin', 'branch.manage'),
        ('BranchAdmin', 'care.plan.manage'),
        ('BranchAdmin', 'care.view'),
        ('BranchAdmin', 'child.enrollment.manage'),
        ('BranchAdmin', 'child.manage'),
        ('BranchAdmin', 'child.observe'),
        ('BranchAdmin', 'child.view'),
        ('BranchAdmin', 'classroom.manage'),
        ('BranchAdmin', 'comms.approve'),
        ('BranchAdmin', 'comms.draft'),
        ('BranchAdmin', 'comms.send'),
        ('BranchAdmin', 'comms.view'),
        ('BranchAdmin', 'consent.issue'),
        ('BranchAdmin', 'consent.review'),
        ('BranchAdmin', 'consent.view'),
        ('BranchAdmin', 'emergency.contacts.manage'),
        ('BranchAdmin', 'event.approved'),
        ('BranchAdmin', 'event.cancelled'),
        ('BranchAdmin', 'event.create'),
        ('BranchAdmin', 'event.manage'),
        ('BranchAdmin', 'event.published'),
        ('BranchAdmin', 'experience.execute'),
        ('BranchAdmin', 'experience.plan.approve'),
        ('BranchAdmin', 'experience.plan.submit'),
        ('BranchAdmin', 'experience.schedule'),
        ('BranchAdmin', 'experience.view'),
        ('BranchAdmin', 'family.manage'),
        ('BranchAdmin', 'family.verify'),
        ('BranchAdmin', 'insights.branch.view'),
        ('BranchAdmin', 'insights.classroom.view'),
        ('BranchAdmin', 'journey.publish'),
        ('BranchAdmin', 'journey.view.internal'),
        ('BranchAdmin', 'observation.approve'),
        ('BranchAdmin', 'safety.map.manage'),
        ('BranchAdmin', 'safety.map.publish'),
        ('BranchAdmin', 'safety.map.view'),
        ('BranchAdmin', 'safety.report'),
        ('BranchAdmin', 'user.manage'),
        ('DaycareTeacher', 'ai.use'),
        ('DaycareTeacher', 'attendance.record'),
        ('DaycareTeacher', 'attendance.view'),
        ('DaycareTeacher', 'care.record'),
        ('DaycareTeacher', 'care.view'),
        ('DaycareTeacher', 'child.observe'),
        ('DaycareTeacher', 'child.view'),
        ('DaycareTeacher', 'comms.draft'),
        ('DaycareTeacher', 'comms.view'),
        ('DaycareTeacher', 'consent.view'),
        ('DaycareTeacher', 'experience.execute'),
        ('DaycareTeacher', 'experience.plan.submit'),
        ('DaycareTeacher', 'experience.schedule'),
        ('DaycareTeacher', 'experience.view'),
        ('DaycareTeacher', 'safety.map.view'),
        ('DaycareTeacher', 'safety.report'),
        ('ClassroomTeacher', 'ai.use'),
        ('ClassroomTeacher', 'attendance.record'),
        ('ClassroomTeacher', 'attendance.view'),
        ('ClassroomTeacher', 'care.record'),
        ('ClassroomTeacher', 'care.view'),
        ('ClassroomTeacher', 'child.observe'),
        ('ClassroomTeacher', 'child.view'),
        ('ClassroomTeacher', 'comms.draft'),
        ('ClassroomTeacher', 'comms.view'),
        ('ClassroomTeacher', 'consent.view'),
        ('ClassroomTeacher', 'experience.execute'),
        ('ClassroomTeacher', 'experience.plan.submit'),
        ('ClassroomTeacher', 'experience.schedule'),
        ('ClassroomTeacher', 'experience.view'),
        ('ClassroomTeacher', 'safety.map.view'),
        ('ClassroomTeacher', 'safety.report')
    ) AS t(role_name, permission_name)
$fn$ LANGUAGE sql IMMUTABLE;

-- System roles: only PlatformAdmin keeps permissions (platform-only). 034 had granted it everything.
DELETE FROM role_permissions WHERE role_id IN (SELECT id FROM roles WHERE tenant_id IS NULL);
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM enjoy_role_template() t
JOIN roles r ON r.tenant_id IS NULL AND r.name = t.role_name AND r.scope_type = 'Platform'
JOIN permissions p ON p.name = t.permission_name
ON CONFLICT DO NOTHING;

-- 4. TENANT-SCOPED ROLE COPIES (user_organization_roles FK requires (role_id, tenant_id)) --
CREATE OR REPLACE FUNCTION ensure_tenant_roles(p_tenant uuid) RETURNS void AS $fn$
BEGIN
    INSERT INTO roles (tenant_id, name, description, scope_type)
    SELECT p_tenant, v.n, v.n, 'Organization'
    FROM (VALUES ('OrganizationOwner'), ('OrganizationAdmin'), ('BranchAdmin'), ('DaycareTeacher'), ('ClassroomTeacher')) AS v(n)
    WHERE NOT EXISTS (SELECT 1 FROM roles r WHERE r.tenant_id = p_tenant AND r.name = v.n);

    UPDATE roles SET scope_type = 'Organization'
    WHERE tenant_id = p_tenant AND name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher');

    DELETE FROM role_permissions rp USING roles r
    WHERE rp.role_id = r.id AND r.tenant_id = p_tenant
      AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher');

    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM enjoy_role_template() t
    JOIN roles r ON r.tenant_id = p_tenant AND r.name = t.role_name
    JOIN permissions p ON p.name = t.permission_name
    ON CONFLICT DO NOTHING;
END
$fn$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION trg_tenants_ensure_roles() RETURNS trigger AS $fn$
BEGIN
    PERFORM ensure_tenant_roles(NEW.id);
    RETURN NEW;
END
$fn$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS tenants_ensure_roles ON tenants;
CREATE TRIGGER tenants_ensure_roles AFTER INSERT ON tenants
FOR EACH ROW EXECUTE FUNCTION trg_tenants_ensure_roles();

DO $$
DECLARE t uuid;
BEGIN
    FOR t IN SELECT id FROM tenants LOOP
        PERFORM ensure_tenant_roles(t);
    END LOOP;
END $$;

-- 5. FAMILY RELATIONSHIP VERIFICATION ----------------------------------------
-- Existing rows were already treated as trusted by the application, so they become 'Verified'.
-- NEW rows default to 'Pending' (deny by default until a guardian-verify permission holder approves).
ALTER TABLE family_relationships ADD COLUMN IF NOT EXISTS status VARCHAR(20) NOT NULL DEFAULT 'Verified';
ALTER TABLE family_relationships ALTER COLUMN status SET DEFAULT 'Pending';
ALTER TABLE family_relationships ADD COLUMN IF NOT EXISTS verified_at TIMESTAMPTZ;
ALTER TABLE family_relationships ADD COLUMN IF NOT EXISTS verified_by UUID;
ALTER TABLE family_relationships ADD COLUMN IF NOT EXISTS revoked_at TIMESTAMPTZ;
ALTER TABLE family_relationships ADD COLUMN IF NOT EXISTS revoked_by UUID;
UPDATE family_relationships SET verified_at = COALESCE(verified_at, created_at) WHERE status = 'Verified' AND verified_at IS NULL;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_family_relationship_status') THEN
        ALTER TABLE family_relationships ADD CONSTRAINT chk_family_relationship_status
            CHECK (status IN ('Pending', 'Verified', 'Revoked'));
    END IF;
END $$;
CREATE INDEX IF NOT EXISTS idx_family_relationships_user_child ON family_relationships (tenant_id, user_id, child_id)
    WHERE status = 'Verified' AND revoked_at IS NULL;

-- 6. BRANCH ADMIN ASSIGNMENTS ------------------------------------------------
CREATE TABLE IF NOT EXISTS user_branch_assignments (
    user_id         UUID NOT NULL,
    branch_id       UUID NOT NULL REFERENCES branches(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL,
    tenant_id       UUID NOT NULL,
    assigned_by     UUID,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    revoked_at      TIMESTAMPTZ,
    PRIMARY KEY (user_id, branch_id),
    FOREIGN KEY (user_id, tenant_id) REFERENCES users(id, tenant_id) ON DELETE CASCADE,
    FOREIGN KEY (organization_id, tenant_id) REFERENCES organizations(id, tenant_id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS idx_user_branch_assignments_lookup ON user_branch_assignments (tenant_id, organization_id, user_id) WHERE revoked_at IS NULL;

-- 7. PLATFORM SUPPORT ACCESS (explicit, purpose-limited, expiring, revocable) --
CREATE TABLE IF NOT EXISTS support_access_grants (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    granted_to_user_id  UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    granted_by_user_id  UUID,
    purpose             VARCHAR(500) NOT NULL CHECK (length(trim(purpose)) > 0),
    read_only           BOOLEAN NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL,
    revoked_at          TIMESTAMPTZ,
    CONSTRAINT chk_support_grant_window CHECK (expires_at > created_at AND expires_at <= created_at + INTERVAL '7 days')
);
CREATE INDEX IF NOT EXISTS idx_support_access_grants_lookup ON support_access_grants (tenant_id, granted_to_user_id) WHERE revoked_at IS NULL;

-- 8. MIGRATE LEGACY GLOBAL ORGANIZATION ROLES -> ORGANIZATION-SCOPED ----------
-- Legacy user_roles rows used system roles for tenant-wide admin/teacher access. They are moved to the
-- single organization of the tenant. Tenants with several organizations are NOT guessed at: those users
-- keep no role until an administrator assigns one per organization (deny by default).
CREATE TEMP TABLE legacy_role_moves AS
SELECT ur.user_id, ur.role_id AS legacy_role_id, u.tenant_id,
       (SELECT o.id FROM organizations o WHERE o.tenant_id = u.tenant_id) AS organization_id,
       CASE r.name WHEN 'Teacher' THEN 'ClassroomTeacher' WHEN 'AssistantTeacher' THEN 'DaycareTeacher' ELSE r.name END AS target_role
FROM user_roles ur
JOIN users u ON u.id = ur.user_id
JOIN roles r ON r.id = ur.role_id
WHERE r.tenant_id IS NULL
  AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'Teacher', 'AssistantTeacher')
  AND (SELECT COUNT(*) FROM organizations o WHERE o.tenant_id = u.tenant_id) = 1;

INSERT INTO organization_memberships (user_id, organization_id, tenant_id)
SELECT DISTINCT user_id, organization_id, tenant_id FROM legacy_role_moves
ON CONFLICT DO NOTHING;

INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
SELECT m.user_id, tr.id, m.organization_id, m.tenant_id
FROM legacy_role_moves m
JOIN roles tr ON tr.tenant_id = m.tenant_id AND tr.name = m.target_role
ON CONFLICT DO NOTHING;

DELETE FROM user_roles ur USING legacy_role_moves m
WHERE ur.user_id = m.user_id AND ur.role_id = m.legacy_role_id;

DROP TABLE legacy_role_moves;

-- Teachers keep the classroom they were employed into as their assigned classroom.
INSERT INTO classroom_teachers (tenant_id, classroom_id, user_id, is_primary)
SELECT t.tenant_id, te.primary_classroom_id, t.user_id, TRUE
FROM teacher_employments te
JOIN teachers t ON t.id = te.teacher_id
JOIN classrooms c ON c.id = te.primary_classroom_id AND c.tenant_id = t.tenant_id
JOIN users u ON u.id = t.user_id AND u.tenant_id = t.tenant_id
WHERE t.user_id IS NOT NULL AND te.primary_classroom_id IS NOT NULL
ON CONFLICT DO NOTHING;

-- 9. AUDIT HISTORY IS APPEND-ONLY ---------------------------------------------
CREATE OR REPLACE FUNCTION audit_log_append_only() RETURNS trigger AS $fn$
BEGIN
    RAISE EXCEPTION 'audit_log is append-only (% not permitted)', TG_OP USING ERRCODE = 'insufficient_privilege';
END
$fn$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS audit_log_no_change ON audit_log;
CREATE TRIGGER audit_log_no_change BEFORE UPDATE OR DELETE ON audit_log
FOR EACH ROW EXECUTE FUNCTION audit_log_append_only();

DROP TRIGGER IF EXISTS audit_log_no_truncate ON audit_log;
CREATE TRIGGER audit_log_no_truncate BEFORE TRUNCATE ON audit_log
FOR EACH STATEMENT EXECUTE FUNCTION audit_log_append_only();

-- 10. DATABASE-LEVEL SAFETY NET: every organization role change is audited ----
CREATE OR REPLACE FUNCTION audit_org_role_change() RETURNS trigger AS $fn$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO audit_log (tenant_id, action, entity_type, entity_id, details)
        VALUES (NEW.tenant_id, 'Role.Granted.Db', 'User', NEW.user_id,
                jsonb_build_object('role_id', NEW.role_id, 'organization_id', NEW.organization_id));
        RETURN NEW;
    END IF;
    INSERT INTO audit_log (tenant_id, action, entity_type, entity_id, details)
    VALUES (OLD.tenant_id, 'Role.Revoked.Db', 'User', OLD.user_id,
            jsonb_build_object('role_id', OLD.role_id, 'organization_id', OLD.organization_id));
    RETURN OLD;
END
$fn$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS user_organization_roles_audit ON user_organization_roles;
CREATE TRIGGER user_organization_roles_audit AFTER INSERT OR DELETE ON user_organization_roles
FOR EACH ROW EXECUTE FUNCTION audit_org_role_change();

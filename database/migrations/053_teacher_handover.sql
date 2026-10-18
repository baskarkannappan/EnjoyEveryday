-- ============================================================
-- EnjoyEveryday — Migration 053: Smart Daily Care & Teacher Handover
-- Creates: teacher_handovers
-- Seeds: care.handover permission
-- ============================================================

-- 1. PERMISSIONS SEED
INSERT INTO permissions (name, description) VALUES
    ('care.handover', 'Create, submit, acknowledge, and review shift-to-shift teacher handovers')
ON CONFLICT (name) DO NOTHING;

-- Grant to system roles
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name IN ('PlatformAdmin', 'OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher')
  AND p.name = 'care.handover'
ON CONFLICT DO NOTHING;

-- Grant to tenant-scoped copies of these roles
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
JOIN permissions p ON p.name = 'care.handover'
WHERE r.tenant_id IS NOT NULL
  AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher')
ON CONFLICT DO NOTHING;

-- 2. TEACHER HANDOVERS TABLE
CREATE TABLE IF NOT EXISTS teacher_handovers (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id             UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    branch_id                   UUID NOT NULL REFERENCES branches(id) ON DELETE CASCADE,
    classroom_id                UUID NOT NULL REFERENCES classrooms(id) ON DELETE CASCADE,
    handover_date               DATE NOT NULL DEFAULT CURRENT_DATE,
    shift_type                  VARCHAR(50) NOT NULL DEFAULT 'MorningToAfternoon',
    outgoing_teacher_user_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    outgoing_teacher_name       VARCHAR(200) NOT NULL,
    incoming_teacher_user_id    UUID REFERENCES users(id) ON DELETE SET NULL,
    incoming_teacher_name       VARCHAR(200),
    summary_notes               TEXT,
    child_specific_notes        TEXT,
    outstanding_care_tasks      TEXT,
    pending_followups           TEXT,
    status                      VARCHAR(30) NOT NULL DEFAULT 'PendingAcknowledgement',
    is_critical                 BOOLEAN NOT NULL DEFAULT FALSE,
    acknowledged_at             TIMESTAMPTZ,
    acknowledged_by_user_id     UUID REFERENCES users(id) ON DELETE SET NULL,
    acknowledged_by_name        VARCHAR(200),
    acknowledgement_notes       TEXT,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_teacher_handovers_org ON teacher_handovers(tenant_id, organization_id, branch_id);
CREATE INDEX IF NOT EXISTS idx_teacher_handovers_classroom_date ON teacher_handovers(classroom_id, handover_date);
CREATE INDEX IF NOT EXISTS idx_teacher_handovers_status ON teacher_handovers(status);

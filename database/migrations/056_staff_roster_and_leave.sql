-- ============================================================
-- EnjoyEveryday - Migration 056: Staff Roster, Leave & Coverage
-- Creates: staff_leaves, classroom_statutory_ratios, and seeds compliance data
-- ============================================================

-- 1. Create staff_leaves table
CREATE TABLE IF NOT EXISTS staff_leaves (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    teacher_id UUID NOT NULL REFERENCES teachers(id) ON DELETE CASCADE,
    leave_type VARCHAR(50) NOT NULL, -- Vacation, Sick, Personal, Training, MaternityPaternity
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    reason TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'Approved', -- Pending, Approved, Rejected, Cancelled
    coverage_status VARCHAR(50) NOT NULL DEFAULT 'Uncovered', -- Covered, Uncovered, AgencyTempNeeded
    substitute_teacher_id UUID REFERENCES teachers(id) ON DELETE SET NULL,
    reviewed_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_staff_leaves_tenant ON staff_leaves(tenant_id, start_date, end_date);
CREATE INDEX IF NOT EXISTS idx_staff_leaves_teacher ON staff_leaves(teacher_id);

-- 2. Create statutory child-to-educator ratios table
CREATE TABLE IF NOT EXISTS classroom_statutory_ratios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    age_group VARCHAR(50) NOT NULL, -- Infant, Toddler, Preschool, SchoolAge
    max_children_per_educator INT NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Seed global baseline statutory ratios (tenant_id IS NULL applies to all tenants)
INSERT INTO classroom_statutory_ratios (id, tenant_id, age_group, max_children_per_educator, description)
SELECT 'b0000000-0000-0000-0000-000000000001', NULL, 'Infant', 3, 'Children under 18 months: 1 educator per 3 infants (max 4)'
WHERE NOT EXISTS (SELECT 1 FROM classroom_statutory_ratios WHERE age_group = 'Infant' AND tenant_id IS NULL);

INSERT INTO classroom_statutory_ratios (id, tenant_id, age_group, max_children_per_educator, description)
SELECT 'b0000000-0000-0000-0000-000000000002', NULL, 'Toddler', 5, 'Children 18 to 30 months: 1 educator per 5 toddlers'
WHERE NOT EXISTS (SELECT 1 FROM classroom_statutory_ratios WHERE age_group = 'Toddler' AND tenant_id IS NULL);

INSERT INTO classroom_statutory_ratios (id, tenant_id, age_group, max_children_per_educator, description)
SELECT 'b0000000-0000-0000-0000-000000000003', NULL, 'Preschool', 8, 'Children 30 months to 5 years: 1 educator per 8 preschoolers'
WHERE NOT EXISTS (SELECT 1 FROM classroom_statutory_ratios WHERE age_group = 'Preschool' AND tenant_id IS NULL);

INSERT INTO classroom_statutory_ratios (id, tenant_id, age_group, max_children_per_educator, description)
SELECT 'b0000000-0000-0000-0000-000000000004', NULL, 'SchoolAge', 12, 'Children 5+ years: 1 educator per 12 school-age children'
WHERE NOT EXISTS (SELECT 1 FROM classroom_statutory_ratios WHERE age_group = 'SchoolAge' AND tenant_id IS NULL);

-- 3. Register permissions for staff roster & leave management
INSERT INTO permissions (name, description) VALUES
    ('staff.roster.view', 'View staff shift roster and statutory educator ratios'),
    ('staff.leave.manage', 'Approve leave requests and assign substitute teacher coverage')
ON CONFLICT (name) DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name IN ('PlatformAdmin', 'OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin')
  AND p.name IN ('staff.roster.view', 'staff.leave.manage')
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
JOIN permissions p ON p.name IN ('staff.roster.view', 'staff.leave.manage')
WHERE r.tenant_id IS NOT NULL
  AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin')
ON CONFLICT DO NOTHING;

-- Teachers can view roster
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name IN ('DaycareTeacher', 'ClassroomTeacher')
  AND p.name = 'staff.roster.view'
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
JOIN permissions p ON p.name = 'staff.roster.view'
WHERE r.tenant_id IS NOT NULL
  AND r.name IN ('DaycareTeacher', 'ClassroomTeacher')
ON CONFLICT DO NOTHING;

-- 4. Seed sample leave request for demonstration
INSERT INTO staff_leaves (id, tenant_id, teacher_id, leave_type, start_date, end_date, reason, status, coverage_status)
SELECT 
    'f0000000-0000-0000-0000-000000000001',
    t.tenant_id,
    t.id,
    'Training',
    CURRENT_DATE,
    CURRENT_DATE + INTERVAL '1 day',
    'Certified Early Childhood STEM workshop.',
    'Approved',
    'Covered'
FROM teachers t
LIMIT 1
ON CONFLICT DO NOTHING;

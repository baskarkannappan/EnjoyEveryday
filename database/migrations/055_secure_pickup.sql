-- ============================================================
-- EnjoyEveryday - Migration 055: Secure Pickup & Child Handover
-- Adds departure verification fields and authorized pickup indexing
-- ============================================================

-- 1. Extend child_attendance with verifiable secure pickup fields
ALTER TABLE child_attendance
ADD COLUMN IF NOT EXISTS pickup_person_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS pickup_person_relationship VARCHAR(100),
ADD COLUMN IF NOT EXISTS pickup_verification_method VARCHAR(100),
ADD COLUMN IF NOT EXISTS pickup_notes TEXT,
ADD COLUMN IF NOT EXISTS authorized_pickup_id UUID REFERENCES authorized_pickups(id) ON DELETE SET NULL,
ADD COLUMN IF NOT EXISTS departure_verified_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL;

-- 2. Indexes for fast lookup during child pickup verification
CREATE INDEX IF NOT EXISTS idx_authorized_pickups_child ON authorized_pickups(tenant_id, child_id, is_active);
CREATE INDEX IF NOT EXISTS idx_child_restrictions_child ON child_person_restrictions(tenant_id, child_id);
CREATE INDEX IF NOT EXISTS idx_child_attendance_departure ON child_attendance(tenant_id, child_id, attendance_date, departure_time);

-- 3. Register permission for secure pickup verification
INSERT INTO permissions (name, description) VALUES
    ('attendance.pickup.verify', 'Verify identity and authorize child release at departure')
ON CONFLICT (name) DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name IN ('PlatformAdmin', 'OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher')
  AND p.name = 'attendance.pickup.verify'
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
JOIN permissions p ON p.name = 'attendance.pickup.verify'
WHERE r.tenant_id IS NOT NULL
  AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher')
ON CONFLICT DO NOTHING;

-- 4. Ensure authorized_pickups photo_url can store full URLs or data URIs
ALTER TABLE authorized_pickups ALTER COLUMN photo_url TYPE TEXT;

-- 5. Seed demo authorized pickups for existing children if none exist
INSERT INTO authorized_pickups (id, tenant_id, child_id, person_id, relationship, pickup_code, photo_url, is_active, notes)
SELECT 
    gen_random_uuid(),
    c.tenant_id,
    c.id,
    NULL,
    'Parent / Guardian',
    '7492',
    CASE WHEN c.photo_url IS NOT NULL AND LENGTH(c.photo_url) < 1000 THEN c.photo_url ELSE NULL END,
    TRUE,
    'Primary authorized guardian on file. Visual recognition or photo ID.'
FROM children c
WHERE NOT EXISTS (SELECT 1 FROM authorized_pickups ap WHERE ap.child_id = c.id);

-- Also add an alternate pickup for first few children
INSERT INTO authorized_pickups (id, tenant_id, child_id, person_id, relationship, pickup_code, photo_url, is_active, notes)
SELECT 
    gen_random_uuid(),
    c.tenant_id,
    c.id,
    NULL,
    'Grandmother',
    '3819',
    NULL,
    TRUE,
    'Authorized for Tuesday/Thursday afternoon pick-up.'
FROM children c
WHERE c.first_name = 'Aarav' OR c.first_name = 'Maya'
LIMIT 2
ON CONFLICT DO NOTHING;

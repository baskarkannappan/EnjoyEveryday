-- ============================================================
-- EnjoyEveryday — Migration 033: Seed Mock Parent and Family Relationship
-- Seeds a verified parent user, links to mock child Aarav Patel,
-- and assigns the 'Parent' role.
-- ============================================================

-- 1. Create Mock Parent User: Sunita Patel
INSERT INTO users (
    id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp, is_active
) VALUES (
    '50000000-0000-0000-0000-000000000010',
    '10000000-0000-0000-0000-000000000001',
    'parent@littlestars.com',
    'PARENT@LITTLESTARS.COM',
    TRUE,
    'Sunita',
    'Patel',
    'Sunita Patel',
    '',
    gen_random_uuid()::text,
    TRUE
) ON CONFLICT DO NOTHING;

-- 2. Assign Parent Role ('a0000000-0000-0000-0000-000000000007' is Parent role)
INSERT INTO user_roles (user_id, role_id)
VALUES (
    '50000000-0000-0000-0000-000000000010',
    'a0000000-0000-0000-0000-000000000007'
) ON CONFLICT DO NOTHING;

-- 3. Create Family Relationship linking Sunita Patel (Parent) to Aarav Patel (Child)
INSERT INTO family_relationships (
    id, tenant_id, child_id, user_id, relationship, is_primary
) VALUES (
    'f0000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    'c0000000-0000-0000-0000-000000000001',
    '50000000-0000-0000-0000-000000000010',
    'Mother',
    TRUE
) ON CONFLICT (child_id, user_id) DO NOTHING;

-- 4. Create Person record & Child Guardian record for safety/profile
INSERT INTO persons (
    id, tenant_id, user_id, first_name, last_name, relationship, email, mobile_phone, has_portal_access, receives_daily_updates, receives_photos
) VALUES (
    '70000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    '50000000-0000-0000-0000-000000000010',
    'Sunita',
    'Patel',
    'Mother',
    'parent@littlestars.com',
    '416-555-0199',
    TRUE,
    TRUE,
    TRUE
) ON CONFLICT DO NOTHING;

INSERT INTO child_guardians (
    id, tenant_id, child_id, person_id, relationship, is_primary, has_legal_custody, can_pickup, can_receive_information, can_authorize_medical_care
) VALUES (
    '80000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    'c0000000-0000-0000-0000-000000000001',
    '70000000-0000-0000-0000-000000000001',
    'Mother',
    TRUE,
    TRUE,
    TRUE,
    TRUE,
    TRUE
) ON CONFLICT (child_id, person_id) DO NOTHING;

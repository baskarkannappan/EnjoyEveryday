-- ============================================================
-- EnjoyEveryday — Migration 002: Seed Data (Development/Demo)
-- Creates 3 sample tenants with organizations, branches,
-- classrooms, and teachers for development use.
-- ============================================================

-- TENANT 1: Little Stars Daycare
INSERT INTO tenants (id, name, slug) VALUES
    ('10000000-0000-0000-0000-000000000001', 'Little Stars Daycare', 'little-stars')
ON CONFLICT (slug) DO NOTHING;

INSERT INTO organizations (id, tenant_id, name, location) VALUES
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'Little Stars Daycare', 'Toronto · 3 branches')
ON CONFLICT DO NOTHING;

INSERT INTO branches (id, tenant_id, organization_id, name, location) VALUES
    ('30000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'Downtown', 'Toronto Downtown'),
    ('30000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'Midtown', 'Toronto Midtown'),
    ('30000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'Uptown', 'Toronto Uptown')
ON CONFLICT DO NOTHING;

INSERT INTO classrooms (id, tenant_id, branch_id, name, age_group, capacity, environment) VALUES
    ('40000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'Bluebirds', '3–4', 18, 'Indoor'),
    ('40000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'Sunflowers', '4–5', 16, 'Indoor')
ON CONFLICT DO NOTHING;

-- Teacher user for Little Stars (password: "Test123!")
INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp) VALUES
    ('50000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'priya@littlestars.com', 'PRIYA@LITTLESTARS.COM', TRUE, 'Priya', 'Sharma', 'Priya', '', gen_random_uuid()::text)
ON CONFLICT DO NOTHING;

INSERT INTO user_roles (user_id, role_id) VALUES
    ('50000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000005')
ON CONFLICT DO NOTHING;

-- Admin user for Little Stars
INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp) VALUES
    ('50000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 'admin@littlestars.com', 'ADMIN@LITTLESTARS.COM', TRUE, 'Admin', 'User', 'Admin', '', gen_random_uuid()::text)
ON CONFLICT DO NOTHING;

INSERT INTO user_roles (user_id, role_id) VALUES
    ('50000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000002')
ON CONFLICT DO NOTHING;


-- TENANT 2: Sunshine Garden Preschool
INSERT INTO tenants (id, name, slug) VALUES
    ('10000000-0000-0000-0000-000000000002', 'Sunshine Garden Preschool', 'sunshine-garden')
ON CONFLICT (slug) DO NOTHING;

INSERT INTO organizations (id, tenant_id, name, location) VALUES
    ('20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002', 'Sunshine Garden Preschool', 'Toronto · 2 branches')
ON CONFLICT DO NOTHING;

INSERT INTO branches (id, tenant_id, organization_id, name, location) VALUES
    ('30000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002', 'East End', 'Toronto East'),
    ('30000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002', 'West End', 'Toronto West')
ON CONFLICT DO NOTHING;

INSERT INTO classrooms (id, tenant_id, branch_id, name, age_group, capacity, environment) VALUES
    ('40000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000004', 'Butterflies', '4–5', 14, 'Outdoor'),
    ('40000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000004', 'Ladybugs', '3–4', 12, 'Indoor')
ON CONFLICT DO NOTHING;

INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp) VALUES
    ('50000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000002', 'daniel@sunshinegarden.com', 'DANIEL@SUNSHINEGARDEN.COM', TRUE, 'Daniel', 'Chen', 'Daniel', '', gen_random_uuid()::text)
ON CONFLICT DO NOTHING;

INSERT INTO user_roles (user_id, role_id) VALUES
    ('50000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000005')
ON CONFLICT DO NOTHING;


-- TENANT 3: Happy Roots Early Learning
INSERT INTO tenants (id, name, slug) VALUES
    ('10000000-0000-0000-0000-000000000003', 'Happy Roots Early Learning', 'happy-roots')
ON CONFLICT (slug) DO NOTHING;

INSERT INTO organizations (id, tenant_id, name, location) VALUES
    ('20000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000003', 'Happy Roots Early Learning', 'Vancouver · 1 branch')
ON CONFLICT DO NOTHING;

INSERT INTO branches (id, tenant_id, organization_id, name, location) VALUES
    ('30000000-0000-0000-0000-000000000006', '10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000003', 'Main Centre', 'Vancouver Downtown')
ON CONFLICT DO NOTHING;

INSERT INTO classrooms (id, tenant_id, branch_id, name, age_group, capacity, environment) VALUES
    ('40000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000006', 'Little Explorers', '3–4', 15, 'Indoor')
ON CONFLICT DO NOTHING;

INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp) VALUES
    ('50000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000003', 'maya@happyroots.com', 'MAYA@HAPPYROOTS.COM', TRUE, 'Maya', 'Williams', 'Maya', '', gen_random_uuid()::text)
ON CONFLICT DO NOTHING;

INSERT INTO user_roles (user_id, role_id) VALUES
    ('50000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000005')
ON CONFLICT DO NOTHING;

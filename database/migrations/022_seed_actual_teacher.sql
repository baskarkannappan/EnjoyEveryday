INSERT INTO teachers (
    id, tenant_id, user_id, first_name, last_name, display_name, status, created_at, updated_at
) VALUES (
    '50000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', 'Priya', 'Sharma', 'Priya Sharma', 'Active', NOW(), NOW()
) ON CONFLICT (id) DO NOTHING;

INSERT INTO children (
    id, tenant_id, first_name, last_name, date_of_birth, classroom_id
) VALUES (
    'c0000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'Aarav', 'Patel', '2023-01-15', '40000000-0000-0000-0000-000000000001'
) ON CONFLICT (id) DO NOTHING;

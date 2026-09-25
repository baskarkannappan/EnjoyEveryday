-- Migration 007: Seed Experiences & Schedules

DO $$ 
DECLARE
    t1 UUID := '10000000-0000-0000-0000-000000000001';
    user1 UUID := '50000000-0000-0000-0000-000000000001';
    class1 UUID := '40000000-0000-0000-0000-000000000001';
    
    exp1 UUID := '60000000-0000-0000-0000-000000000001';
    exp2 UUID := '60000000-0000-0000-0000-000000000002';
    exp3 UUID := '60000000-0000-0000-0000-000000000003';
    exp4 UUID := '60000000-0000-0000-0000-000000000004';

    d1 DATE := CURRENT_DATE;
    d2 DATE := CURRENT_DATE + INTERVAL '1 day';
    d3 DATE := CURRENT_DATE + INTERVAL '2 days';
    d4 DATE := CURRENT_DATE + INTERVAL '3 days';
BEGIN
    INSERT INTO experiences (id, tenant_id, title, description, status, dna_payload, created_by_user_id) VALUES
    (exp1, t1, 'Tiny Discoveries', 'Look closely at tiny things hiding in the classroom.', 'Published', '{"type": "Explore"}', user1),
    (exp2, t1, 'Our Little City', 'Children build a city together.', 'Published', '{"type": "Create"}', user1),
    (exp3, t1, 'Frog Rescue', 'A cooperative rescue mission.', 'Published', '{"type": "Together"}', user1),
    (exp4, t1, 'Animal Adventure', 'Move, sound and imagine like animals.', 'Published', '{"type": "Move"}', user1)
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO experience_schedules (id, tenant_id, experience_id, classroom_id, scheduled_date, time_of_day, status) VALUES
    (gen_random_uuid(), t1, exp1, class1, d1, 'Morning', 'Scheduled'),
    (gen_random_uuid(), t1, exp2, class1, d2, 'Morning', 'Scheduled'),
    (gen_random_uuid(), t1, exp3, class1, d3, 'Morning', 'Scheduled'),
    (gen_random_uuid(), t1, exp4, class1, d4, 'Morning', 'Scheduled')
    ON CONFLICT DO NOTHING;
END $$;

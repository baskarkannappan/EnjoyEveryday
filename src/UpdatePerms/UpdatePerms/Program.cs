using Npgsql;
using System;

await using var conn = new NpgsqlConnection("Host=localhost;Port=5433;Database=enjoyeveryday;Username=postgres;Password=postgres;Include Error Detail=true");
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand(@"
DO $$
DECLARE
    role_rec RECORD;
    perm_rec RECORD;
BEGIN
    FOR role_rec IN SELECT id FROM roles WHERE name IN ('PlatformAdmin', 'OrganizationOwner', 'OrganizationAdmin', 'Daycare Admin')
    LOOP
        FOR perm_rec IN SELECT id FROM permissions
        LOOP
            INSERT INTO role_permissions (role_id, permission_id)
            VALUES (role_rec.id, perm_rec.id)
            ON CONFLICT DO NOTHING;
        END LOOP;
    END LOOP;

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
", conn);
await cmd.ExecuteNonQueryAsync();
Console.WriteLine("Role permissions updated successfully in DB.");

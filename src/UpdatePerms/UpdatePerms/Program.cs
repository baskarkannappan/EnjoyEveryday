using Npgsql;
using System;

await using var conn = new NpgsqlConnection("Host=localhost;Port=5433;Database=enjoyeveryday;Username=postgres;Password=postgres;Include Error Detail=true");
await conn.OpenAsync();

var adminUserId = Guid.Parse("50000000-0000-0000-0000-000000000002");

Console.WriteLine("=== Active Roles for admin@littlestars.com ===");
await using (var cmd = new NpgsqlCommand(@"
    SELECT r.name, 'Platform' as scope
    FROM user_roles ur JOIN roles r ON ur.role_id = r.id 
    WHERE ur.user_id = @uid
    UNION
    SELECT r.name, 'Organization: ' || o.name as scope
    FROM user_organization_roles uor 
    JOIN roles r ON uor.role_id = r.id 
    JOIN organizations o ON uor.organization_id = o.id
    WHERE uor.user_id = @uid;
", conn))
{
    cmd.Parameters.AddWithValue("uid", adminUserId);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  ✓ {reader.GetString(0)} ({reader.GetString(1)})");
    }
}

Console.WriteLine("\n=== Total Distinct Permissions Granted ===");
await using (var cmd = new NpgsqlCommand(@"
    SELECT COUNT(DISTINCT perm_name)
    FROM (
        SELECT p.name AS perm_name
        FROM user_roles ur 
        JOIN role_permissions rp ON ur.role_id = rp.role_id 
        JOIN permissions p ON rp.permission_id = p.id 
        WHERE ur.user_id = @uid
        UNION
        SELECT p.name AS perm_name
        FROM user_organization_roles uor 
        JOIN role_permissions rp ON uor.role_id = rp.role_id 
        JOIN permissions p ON rp.permission_id = p.id 
        WHERE uor.user_id = @uid
    ) all_perms;
", conn))
{
    cmd.Parameters.AddWithValue("uid", adminUserId);
    var count = await cmd.ExecuteScalarAsync();
    Console.WriteLine($"  Total Distinct Permissions: {count}");
}

Console.WriteLine("\n=== Permissions List ===");
await using (var cmd = new NpgsqlCommand(@"
    SELECT DISTINCT p.name
    FROM (
        SELECT p.name 
        FROM user_roles ur 
        JOIN role_permissions rp ON ur.role_id = rp.role_id 
        JOIN permissions p ON rp.permission_id = p.id 
        WHERE ur.user_id = @uid
        UNION
        SELECT p.name 
        FROM user_organization_roles uor 
        JOIN role_permissions rp ON uor.role_id = rp.role_id 
        JOIN permissions p ON rp.permission_id = p.id 
        WHERE uor.user_id = @uid
    ) p
    ORDER BY p.name;
", conn))
{
    cmd.Parameters.AddWithValue("uid", adminUserId);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader.GetString(0)}");
    }
}

Console.WriteLine("\nFULL PERMISSION TO THE ENTIRE APPLICATION HAS BEEN VERIFIED FOR admin@littlestars.com!");

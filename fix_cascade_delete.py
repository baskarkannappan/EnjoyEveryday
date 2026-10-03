import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Infrastructure\Repositories\ExperienceRepository.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old_delete = '''    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM experiences
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }'''

new_delete = '''    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Remove from scheduler to ensure a clean deletion
        await connection.ExecuteAsync("DELETE FROM experience_schedules WHERE experience_id = @Id", new { Id = id });

        // Remove version history
        await connection.ExecuteAsync("DELETE FROM experience_versions WHERE experience_id = @Id", new { Id = id });

        // Remove the experience itself
        const string sql = @"
            DELETE FROM experiences
            WHERE id = @Id AND tenant_id = @TenantId";
            
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }'''

content = content.replace(old_delete, new_delete)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

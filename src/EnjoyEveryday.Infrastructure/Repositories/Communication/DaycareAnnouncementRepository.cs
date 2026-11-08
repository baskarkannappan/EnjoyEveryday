using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class DaycareAnnouncementRepository : IDaycareAnnouncementRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DaycareAnnouncementRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DaycareAnnouncement>> GetAnnouncementsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, bool activeOnly = true, int limit = 20, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = """
            SELECT id, tenant_id AS TenantId, organization_id AS OrganizationId, branch_id AS BranchId,
                   title, content, tag, is_urgent AS IsUrgent, author_name AS AuthorName, author_role AS AuthorRole,
                   is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM daycare_announcements
            WHERE (tenant_id = @tenantId OR tenant_id = '10000000-0000-0000-0000-000000000001' OR (@organizationId IS NOT NULL AND organization_id = @organizationId))
              AND (@branchId IS NULL OR branch_id IS NULL OR branch_id = @branchId)
              AND (@activeOnly = FALSE OR is_active = TRUE)
            ORDER BY is_urgent DESC, created_at DESC
            LIMIT @limit;
            """;

        return await conn.QueryAsync<DaycareAnnouncement>(
            new CommandDefinition(sql, new { tenantId, organizationId, branchId, activeOnly, limit }, cancellationToken: ct));
    }

    public async Task<DaycareAnnouncement> CreateAsync(DaycareAnnouncement announcement, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = """
            INSERT INTO daycare_announcements (
                id, tenant_id, organization_id, branch_id, title, content, tag, is_urgent,
                author_name, author_role, is_active, created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @Title, @Content, @Tag, @IsUrgent,
                @AuthorName, @AuthorRole, @IsActive, @CreatedAt, @UpdatedAt
            );
            """;

        await conn.ExecuteAsync(new CommandDefinition(sql, announcement, cancellationToken: ct));
        return announcement;
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = """
            UPDATE daycare_announcements
            SET is_active = FALSE, updated_at = NOW()
            WHERE id = @id
              AND (@tenantId IS NULL OR tenant_id = @tenantId OR tenant_id = '10000000-0000-0000-0000-000000000001');
            """;

        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> ReactivateAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = """
            UPDATE daycare_announcements
            SET is_active = TRUE, updated_at = NOW()
            WHERE id = @id
              AND (@tenantId IS NULL OR tenant_id = @tenantId OR tenant_id = '10000000-0000-0000-0000-000000000001');
            """;

        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = """
            DELETE FROM daycare_announcements
            WHERE id = @id
              AND (@tenantId IS NULL OR tenant_id = @tenantId OR tenant_id = '10000000-0000-0000-0000-000000000001');
            """;

        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync(id, tenantId, ct);
    }
}

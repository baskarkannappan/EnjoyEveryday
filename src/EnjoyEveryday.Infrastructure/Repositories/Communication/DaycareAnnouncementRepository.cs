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
        return await conn.QueryFunctionAsync<DaycareAnnouncement>(
            "fn_get_daycare_announcements",
            new
            {
                p_tenant_id = tenantId,
                p_organization_id = organizationId,
                p_branch_id = branchId,
                p_active_only = activeOnly,
                p_limit = limit
            },
            cancellationToken: ct);
    }

    public async Task<DaycareAnnouncement> CreateAsync(DaycareAnnouncement announcement, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_create_daycare_announcement",
            new
            {
                p_id = announcement.Id,
                p_tenant_id = announcement.TenantId,
                p_organization_id = announcement.OrganizationId,
                p_branch_id = announcement.BranchId,
                p_title = announcement.Title,
                p_content = announcement.Content,
                p_tag = announcement.Tag,
                p_is_urgent = announcement.IsUrgent,
                p_author_name = announcement.AuthorName,
                p_author_role = announcement.AuthorRole,
                p_is_active = announcement.IsActive,
                p_created_at = announcement.CreatedAt,
                p_updated_at = announcement.UpdatedAt
            },
            cancellationToken: ct);
        return announcement;
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_set_daycare_announcement_status",
            new { p_id = id, p_tenant_id = tenantId, p_is_active = false },
            cancellationToken: ct);
        return true;
    }

    public async Task<bool> ReactivateAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_set_daycare_announcement_status",
            new { p_id = id, p_tenant_id = tenantId, p_is_active = true },
            cancellationToken: ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_delete_daycare_announcement",
            new { p_id = id, p_tenant_id = tenantId },
            cancellationToken: ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync(id, tenantId, ct);
    }
}

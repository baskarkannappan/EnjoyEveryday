using Dapper;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

/// <summary>
/// Live (uncached), tenant-filtered lookups behind IResourceAuthorizer. Every query binds @TenantId.
/// </summary>
public class AccessScopeRepository : IAccessScopeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AccessScopeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private sealed class ScopeRow
    {
        public Guid TenantId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? ClassroomId { get; set; }
    }

    private static ResourceScope? ToScope(ScopeRow? r) =>
        r == null ? null : new ResourceScope(r.TenantId, r.OrganizationId, r.BranchId, r.ClassroomId);

    public async Task<bool> HasActiveMembershipAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM organization_memberships m
                JOIN users u ON u.id = m.user_id AND u.tenant_id = m.tenant_id
                WHERE m.tenant_id = @TenantId AND m.organization_id = @OrganizationId AND m.user_id = @UserId
                  AND m.status = 'Active' AND u.is_active = true)";
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return await c.ExecuteScalarAsync<bool>(sql, new { TenantId = tenantId, OrganizationId = organizationId, UserId = userId });
    }

    public async Task<IReadOnlyList<string>> GetOrganizationRoleNamesAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT r.name
            FROM user_organization_roles uor
            JOIN roles r ON r.id = uor.role_id AND r.tenant_id = uor.tenant_id
            JOIN organization_memberships m ON m.user_id = uor.user_id AND m.organization_id = uor.organization_id AND m.tenant_id = uor.tenant_id
            WHERE uor.tenant_id = @TenantId AND uor.organization_id = @OrganizationId AND uor.user_id = @UserId
              AND m.status = 'Active'";
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return (await c.QueryAsync<string>(sql, new { TenantId = tenantId, OrganizationId = organizationId, UserId = userId })).ToList();
    }

    public async Task<IReadOnlySet<Guid>> GetAssignedBranchIdsAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT branch_id FROM user_branch_assignments
            WHERE tenant_id = @TenantId AND organization_id = @OrganizationId AND user_id = @UserId AND revoked_at IS NULL";
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return (await c.QueryAsync<Guid>(sql, new { TenantId = tenantId, OrganizationId = organizationId, UserId = userId })).ToHashSet();
    }

    public async Task<IReadOnlySet<Guid>> GetAssignedClassroomIdsAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        const string sql = @"SELECT classroom_id FROM classroom_teachers WHERE tenant_id = @TenantId AND user_id = @UserId";
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return (await c.QueryAsync<Guid>(sql, new { TenantId = tenantId, UserId = userId })).ToHashSet();
    }

    public async Task<IReadOnlySet<Guid>> GetBranchIdsOfAssignedClassroomsAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT DISTINCT cl.branch_id
            FROM classroom_teachers ct
            JOIN classrooms cl ON cl.id = ct.classroom_id AND cl.tenant_id = ct.tenant_id
            WHERE ct.tenant_id = @TenantId AND ct.user_id = @UserId";
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return (await c.QueryAsync<Guid>(sql, new { TenantId = tenantId, UserId = userId })).ToHashSet();
    }

    public async Task<ResourceScope?> GetBranchScopeAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT b.tenant_id AS TenantId, b.organization_id AS OrganizationId, b.id AS BranchId, NULL::uuid AS ClassroomId
            FROM branches b WHERE b.id = @Id AND b.tenant_id = @TenantId";
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return ToScope(await c.QueryFirstOrDefaultAsync<ScopeRow>(sql, new { Id = branchId, TenantId = tenantId }));
    }

    public async Task<ResourceScope?> GetClassroomScopeAsync(Guid tenantId, Guid classroomId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT cl.tenant_id AS TenantId, b.organization_id AS OrganizationId, b.id AS BranchId, cl.id AS ClassroomId
            FROM classrooms cl JOIN branches b ON b.id = cl.branch_id AND b.tenant_id = cl.tenant_id
            WHERE cl.id = @Id AND cl.tenant_id = @TenantId";
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return ToScope(await c.QueryFirstOrDefaultAsync<ScopeRow>(sql, new { Id = classroomId, TenantId = tenantId }));
    }

    public async Task<ResourceScope?> GetChildScopeAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT ch.tenant_id AS TenantId, b.organization_id AS OrganizationId, b.id AS BranchId, cl.id AS ClassroomId
            FROM children ch
            LEFT JOIN classrooms cl ON cl.id = ch.classroom_id AND cl.tenant_id = ch.tenant_id
            LEFT JOIN branches b ON b.id = cl.branch_id AND b.tenant_id = cl.tenant_id
            WHERE ch.id = @Id AND ch.tenant_id = @TenantId";
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return ToScope(await c.QueryFirstOrDefaultAsync<ScopeRow>(sql, new { Id = childId, TenantId = tenantId }));
    }

    public async Task<bool> HasVerifiedGuardianRelationshipAsync(Guid tenantId, Guid userId, Guid childId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM family_relationships fr
                JOIN children c ON c.id = fr.child_id AND c.tenant_id = fr.tenant_id
                JOIN users u ON u.id = fr.user_id AND u.tenant_id = fr.tenant_id
                WHERE fr.tenant_id = @TenantId AND fr.user_id = @UserId AND fr.child_id = @ChildId
                  AND fr.status = 'Verified' AND fr.revoked_at IS NULL
                  AND c.is_active = true AND u.is_active = true)";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<bool>(sql, new { TenantId = tenantId, UserId = userId, ChildId = childId });
    }

    public async Task<SupportGrant?> GetActiveSupportGrantAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, tenant_id AS TenantId, granted_to_user_id AS GrantedToUserId, purpose AS Purpose,
                   expires_at AS ExpiresAt, read_only AS ReadOnly
            FROM support_access_grants
            WHERE tenant_id = @TenantId AND granted_to_user_id = @UserId
              AND revoked_at IS NULL AND expires_at > NOW()
            ORDER BY expires_at DESC LIMIT 1";
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return await c.QueryFirstOrDefaultAsync<SupportGrant>(sql, new { TenantId = tenantId, UserId = userId });
    }
}

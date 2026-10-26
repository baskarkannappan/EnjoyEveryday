using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
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
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return await c.ExecuteScalarFunctionAsync<bool>(
            "fn_has_active_membership",
            new { TenantId = tenantId, OrganizationId = organizationId, UserId = userId },
            cancellationToken: ct);
    }

    public async Task<IReadOnlyList<string>> GetOrganizationRoleNamesAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return (await c.QueryFunctionAsync<string>(
            "fn_get_organization_role_names",
            new { TenantId = tenantId, OrganizationId = organizationId, UserId = userId },
            cancellationToken: ct)).ToList();
    }

    public async Task<IReadOnlySet<Guid>> GetAssignedBranchIdsAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return (await c.QueryFunctionAsync<Guid>(
            "fn_get_assigned_branch_ids",
            new { TenantId = tenantId, OrganizationId = organizationId, UserId = userId },
            cancellationToken: ct)).ToHashSet();
    }

    public async Task<IReadOnlySet<Guid>> GetAssignedClassroomIdsAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return (await c.QueryFunctionAsync<Guid>(
            "fn_get_assigned_classroom_ids",
            new { TenantId = tenantId, UserId = userId },
            cancellationToken: ct)).ToHashSet();
    }

    public async Task<IReadOnlySet<Guid>> GetBranchIdsOfAssignedClassroomsAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return (await c.QueryFunctionAsync<Guid>(
            "fn_get_branch_ids_of_assigned_classrooms",
            new { TenantId = tenantId, UserId = userId },
            cancellationToken: ct)).ToHashSet();
    }

    public async Task<ResourceScope?> GetBranchScopeAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return ToScope(await c.QuerySingleFunctionAsync<ScopeRow>(
            "fn_get_branch_scope",
            new { TenantId = tenantId, Id = branchId },
            cancellationToken: ct));
    }

    public async Task<ResourceScope?> GetClassroomScopeAsync(Guid tenantId, Guid classroomId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return ToScope(await c.QuerySingleFunctionAsync<ScopeRow>(
            "fn_get_classroom_scope",
            new { TenantId = tenantId, Id = classroomId },
            cancellationToken: ct));
    }

    public async Task<ResourceScope?> GetChildScopeAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return ToScope(await c.QuerySingleFunctionAsync<ScopeRow>(
            "fn_get_child_scope",
            new { TenantId = tenantId, Id = childId },
            cancellationToken: ct));
    }

    public async Task<bool> HasVerifiedGuardianRelationshipAsync(Guid tenantId, Guid userId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarFunctionAsync<bool>(
            "fn_has_verified_guardian_relationship",
            new { TenantId = tenantId, UserId = userId, ChildId = childId },
            cancellationToken: ct);
    }

    public async Task<SupportGrant?> GetActiveSupportGrantAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        return await c.QuerySingleFunctionAsync<SupportGrant>(
            "fn_get_active_support_grant",
            new { TenantId = tenantId, UserId = userId },
            cancellationToken: ct);
    }
}

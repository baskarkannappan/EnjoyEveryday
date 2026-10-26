using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ChildRepository : IChildRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChildRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] AddChildParams =
    [
        "Id", "TenantId", "FirstName", "LastName", "DateOfBirth", "ClassroomId",
        "PhotoUrl", "ProfileCompletionPercentage", "EnrollmentStatus", "DraftData",
        "IsActive", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] UpdateChildParams =
    [
        "Id", "TenantId", "FirstName", "LastName", "DateOfBirth", "ClassroomId",
        "PhotoUrl", "ProfileCompletionPercentage", "EnrollmentStatus", "DraftData",
        "IsActive", "UpdatedAt"
    ];

    public async Task<Child?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<Child>(
            "fn_get_child_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => GetAllAsync(tenantId, null, cancellationToken);

    public async Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<Child>(
            "fn_get_all_children",
            new { TenantId = tenantId, OrganizationId = organizationId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Child>> GetByClassroomIdAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<Child>(
            "fn_get_children_by_classroom",
            new { TenantId = tenantId, ClassroomId = classroomId },
            cancellationToken: cancellationToken);
    }

    public async Task<Child> AddAsync(Child child, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleFunctionAsync<Child>(
            "fn_create_child",
            child,
            parameterNames: AddChildParams,
            cancellationToken: cancellationToken);

        return created ?? child;
    }

    public async Task UpdateAsync(Child child, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_update_child",
            child,
            parameterNames: UpdateChildParams,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_delete_child",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public async Task SyncParentGuardianAsync(Guid tenantId, Guid childId, string parentEmail, string parentName, string relationship, string phone, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(parentEmail)) return;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_sync_parent_guardian",
            new
            {
                TenantId = tenantId,
                ChildId = childId,
                ParentEmail = parentEmail.Trim(),
                ParentName = parentName?.Trim(),
                Relationship = string.IsNullOrWhiteSpace(relationship) ? "Parent" : relationship.Trim(),
                Phone = phone?.Trim()
            },
            cancellationToken: cancellationToken);
    }
}

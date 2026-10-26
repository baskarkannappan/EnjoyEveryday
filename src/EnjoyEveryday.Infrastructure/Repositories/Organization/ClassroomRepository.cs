using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ClassroomRepository : IClassroomRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ClassroomRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] AddClassroomParams =
    [
        "Id", "TenantId", "BranchId", "Name", "DisplayName", "ShortName", "Code", "Description",
        "ClassroomType", "Status", "AgeGroup", "MinAgeMonths", "MaxAgeMonths", "Capacity",
        "CurrentEnrollment", "Environment", "PhotoUrl", "DraftData::jsonb", "ProfileCompletionPercentage",
        "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
    ];

    private static readonly string[] UpdateClassroomParams =
    [
        "Id", "TenantId", "Name", "DisplayName", "ShortName", "Code", "Description",
        "ClassroomType", "Status", "AgeGroup", "MinAgeMonths", "MaxAgeMonths", "Capacity",
        "CurrentEnrollment", "Environment", "PhotoUrl", "DraftData::jsonb", "ProfileCompletionPercentage",
        "IsActive", "UpdatedAt", "UpdatedBy"
    ];

    public async Task<Classroom?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<Classroom>(
            "public.fn_get_classroom_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Classroom>> GetByBranchIdAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<Classroom>(
            "public.fn_get_classrooms_by_branch",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: cancellationToken);
    }

    public Task<IEnumerable<Classroom>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => GetAllAsync(tenantId, null, cancellationToken);

    public async Task<IEnumerable<Classroom>> GetAllAsync(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<Classroom>(
            "public.fn_get_all_classrooms",
            new { TenantId = tenantId, OrganizationId = organizationId },
            cancellationToken: cancellationToken);
    }

    public async Task<Classroom> AddAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.QuerySingleFunctionAsync<dynamic>(
            "public.fn_create_classroom",
            classroom,
            parameterNames: AddClassroomParams,
            cancellationToken: cancellationToken);
        return classroom;
    }

    public async Task UpdateAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_update_classroom",
            classroom,
            parameterNames: UpdateClassroomParams,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_classroom",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }
}

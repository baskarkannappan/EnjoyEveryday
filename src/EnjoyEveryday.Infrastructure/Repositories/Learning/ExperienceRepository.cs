using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ExperienceRepository : IExperienceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExperienceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] AddExperienceParams =
    [
        "Id", "TenantId", "OrganizationId", "Title", "Description",
        "Status", "DnaPayload::jsonb", "CreatedByUserId", "CreatedAt", "UpdatedAt", "VersionId"
    ];

    private static readonly string[] UpdateExperienceParams =
    [
        "Id", "TenantId", "Title", "Description",
        "Status", "DnaPayload::jsonb", "UpdatedAt", "VersionId", "ChangeReason", "ModifiedByUserId"
    ];

    public async Task<Experience?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<Experience>(
            "public.fn_get_experience_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public Task<IEnumerable<Experience>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => GetAllAsync(tenantId, null, cancellationToken);

    public async Task<IEnumerable<Experience>> GetAllAsync(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<Experience>(
            "public.fn_get_all_experiences",
            new { TenantId = tenantId, OrganizationId = organizationId },
            cancellationToken: cancellationToken);
    }

    public async Task<Experience> AddAsync(Experience experience, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleFunctionAsync<Experience>(
            "public.fn_create_experience",
            new
            {
                experience.Id,
                experience.TenantId,
                experience.OrganizationId,
                experience.Title,
                experience.Description,
                experience.Status,
                DnaPayload = experience.DnaPayload ?? "{}",
                experience.CreatedByUserId,
                experience.CreatedAt,
                experience.UpdatedAt,
                VersionId = Guid.NewGuid()
            },
            parameterNames: AddExperienceParams,
            cancellationToken: cancellationToken);

        return created ?? experience;
    }

    public async Task UpdateAsync(Experience experience, string changeReason, Guid modifiedByUserId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_update_experience",
            new
            {
                experience.Id,
                experience.TenantId,
                experience.Title,
                experience.Description,
                experience.Status,
                DnaPayload = experience.DnaPayload ?? "{}",
                experience.UpdatedAt,
                VersionId = Guid.NewGuid(),
                ChangeReason = changeReason,
                ModifiedByUserId = modifiedByUserId
            },
            parameterNames: UpdateExperienceParams,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_experience",
            new 
            { 
                TenantId = tenantId,
                Id = id, 
                Status = ExperienceStatus.Archived.ToString(), 
                UpdatedAt = DateTimeOffset.UtcNow
            },
            cancellationToken: cancellationToken);
    }
    
    public async Task<IEnumerable<ExperienceVersion>> GetVersionsAsync(Guid experienceId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ExperienceVersion>(
            "public.fn_get_experience_versions",
            new { ExperienceId = experienceId },
            cancellationToken: cancellationToken);
    }
}

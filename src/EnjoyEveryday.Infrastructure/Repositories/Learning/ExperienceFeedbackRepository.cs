using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ExperienceFeedbackRepository : IExperienceFeedbackRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ExperienceFeedbackRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    private static readonly string[] InsertFeedbackParams =
    [
        "Id", "TenantId", "ExperienceScheduleId", "ExperienceId", "TeacherId", "Stars", "Rating", "Notes", "CreatedAt"
    ];

    private static readonly string[] UpdateFeedbackParams =
    [
        "Id", "TenantId", "Stars", "Rating", "Notes"
    ];

    public async Task<ExperienceFeedback?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<ExperienceFeedback>(
            "public.fn_get_experience_feedback_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ExperienceFeedback>> GetByScheduleIdAsync(Guid tenantId, Guid scheduleId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ExperienceFeedback>(
            "public.fn_get_experience_feedback_by_schedule_id",
            new { TenantId = tenantId, ScheduleId = scheduleId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ExperienceFeedback>> GetByExperienceIdAsync(Guid tenantId, Guid experienceId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ExperienceFeedback>(
            "public.fn_get_experience_feedback_by_experience_id",
            new { TenantId = tenantId, ExperienceId = experienceId },
            cancellationToken: cancellationToken);
    }

    public async Task<ExperienceFeedback> AddAsync(ExperienceFeedback feedback, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_insert_experience_feedback",
            feedback,
            parameterNames: InsertFeedbackParams,
            cancellationToken: cancellationToken);

        return feedback;
    }

    public async Task<IEnumerable<ExperienceFeedback>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ExperienceFeedback>(
            "public.fn_get_all_experience_feedbacks",
            new { TenantId = tenantId },
            cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(ExperienceFeedback feedback, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_update_experience_feedback",
            feedback,
            parameterNames: UpdateFeedbackParams,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_experience_feedback",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }
}

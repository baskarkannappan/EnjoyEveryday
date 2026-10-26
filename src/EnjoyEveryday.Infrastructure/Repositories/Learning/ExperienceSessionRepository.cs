using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ExperienceSessionRepository : IExperienceSessionRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ExperienceSessionRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    private static readonly string[] CreateSessionParams =
    [
        "Id", "TenantId", "ExperienceId", "ExperienceScheduleId", "ClassroomId",
        "SessionDate", "StartTime", "EndTime", "Status", "PrimaryTeacherId", "ActualNotes"
    ];

    private static readonly string[] UpdateSessionParams =
    [
        "Id", "TenantId", "Status", "ActualNotes", "StartTime", "EndTime"
    ];

    private static readonly string[] InsertParticipationParams =
    [
        "Id", "ExperienceSessionId", "ChildId", "ParticipationStatus", "ParticipationNotes"
    ];

    public async Task<ExperienceSession?> GetSessionByIdAsync(Guid tenantId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<ExperienceSession>(
            "public.fn_get_experience_session_by_id",
            new { TenantId = tenantId, SessionId = sessionId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ExperienceSession>> GetSessionsForClassroomAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ExperienceSession>(
            "public.fn_get_experience_sessions_for_classroom",
            new { TenantId = tenantId, ClassroomId = classroomId },
            cancellationToken: cancellationToken);
    }

    public async Task<ExperienceSession?> GetSessionByScheduleIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<ExperienceSession>(
            "public.fn_get_experience_session_by_schedule_id",
            new { ScheduleId = scheduleId },
            cancellationToken: cancellationToken);
    }

    public async Task CreateSessionAsync(ExperienceSession session, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_create_experience_session",
            session,
            parameterNames: CreateSessionParams,
            cancellationToken: cancellationToken);
    }

    public async Task UpdateSessionAsync(ExperienceSession session, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_update_experience_session",
            session,
            parameterNames: UpdateSessionParams,
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ExperienceParticipation>> GetParticipationsForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ExperienceParticipation>(
            "public.fn_get_participations_for_session",
            new { SessionId = sessionId },
            cancellationToken: cancellationToken);
    }

    public async Task SaveParticipationsAsync(Guid sessionId, IEnumerable<ExperienceParticipation> participations, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_participations_for_session",
            new { SessionId = sessionId },
            cancellationToken: cancellationToken);

        if (participations != null && participations.Any())
        {
            foreach (var p in participations)
            {
                if (p.Id == Guid.Empty) p.Id = Guid.NewGuid();
                p.ExperienceSessionId = sessionId;
                await connection.ExecuteProcedureAsync(
                    "public.sp_insert_experience_participation",
                    p,
                    parameterNames: InsertParticipationParams,
                    cancellationToken: cancellationToken);
            }
        }
    }

    public async Task<IEnumerable<Guid>> GetLoggedChildIdsForSessionAsync(Guid tenantId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<Guid>(
            "public.fn_get_logged_child_ids_for_session",
            new { TenantId = tenantId, SessionId = sessionId },
            cancellationToken: cancellationToken);
    }
}

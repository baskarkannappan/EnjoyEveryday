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

public class ChildJourneyRepository : IChildJourneyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChildJourneyRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ChildJourneyEntry?> GetJourneyEntryByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<ChildJourneyEntry>(
            "public.fn_get_journey_entry_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildJourneyEntry>> GetJourneyEntriesForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ChildJourneyEntry>(
            "public.fn_get_journey_entries_for_child",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    private static readonly string[] CreateJourneyEntryParams =
    [
        "Id", "TenantId", "ChildId", "EnrollmentId", "ClassroomId", "TeacherId",
        "ExperienceId", "ExperienceScheduleId", "ExperienceSessionId", "ExperienceParticipationId",
        "LittleMomentId", "MilestoneId", "EventId",
        "JourneyType", "Title", "Observation", "ChildVoice", "TeacherReflection",
        "ObservationDate", "ParticipationType", "IsMilestone", "IsParentVisible",
        "ParentVisibilityStatus", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version"
    ];

    private static readonly string[] UpdateJourneyEntryParams =
    [
        "Id", "TenantId", "JourneyType", "Title", "Observation",
        "ChildVoice", "TeacherReflection", "ObservationDate", "ParticipationType",
        "IsMilestone", "IsParentVisible", "ParentVisibilityStatus", "UpdatedAt", "UpdatedBy"
    ];

    public async Task CreateJourneyEntryAsync(ChildJourneyEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_create_journey_entry",
            entry,
            parameterNames: CreateJourneyEntryParams,
            cancellationToken: cancellationToken);
    }

    public async Task UpdateJourneyEntryAsync(ChildJourneyEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_update_journey_entry",
            entry,
            parameterNames: UpdateJourneyEntryParams,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteJourneyEntryAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_journey_entry",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<DevelopmentArea>> GetAllDevelopmentAreasAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<DevelopmentArea>(
            "public.fn_get_all_development_areas",
            new { TenantId = tenantId },
            cancellationToken: cancellationToken);
    }

    public async Task AddJourneyDevelopmentsAsync(Guid journeyEntryId, IEnumerable<Guid> developmentAreaIds, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_journey_developments",
            new { JourneyEntryId = journeyEntryId },
            cancellationToken: cancellationToken);

        if (developmentAreaIds != null && developmentAreaIds.Any())
        {
            foreach (var id in developmentAreaIds)
            {
                await connection.ExecuteProcedureAsync(
                    "public.sp_insert_journey_development",
                    new { JourneyEntryId = journeyEntryId, DevelopmentAreaId = id },
                    cancellationToken: cancellationToken);
            }
        }
    }

    public async Task<IEnumerable<Guid>> GetDevelopmentAreasForJourneyEntryAsync(Guid journeyEntryId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<Guid>(
            "public.fn_get_development_areas_for_journey_entry",
            new { JourneyEntryId = journeyEntryId },
            cancellationToken: cancellationToken);
    }

    public async Task CreateJourneyEvidenceAsync(ChildJourneyEvidence evidence, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_create_journey_evidence",
            evidence,
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildJourneyEvidence>> GetEvidenceForJourneyEntryAsync(Guid journeyEntryId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ChildJourneyEvidence>(
            "public.fn_get_evidence_for_journey_entry",
            new { JourneyEntryId = journeyEntryId },
            cancellationToken: cancellationToken);
    }

    public async Task CreateMilestoneAsync(ChildMilestone milestone, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_create_milestone",
            milestone,
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildMilestone>> GetMilestonesForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ChildMilestone>(
            "public.fn_get_milestones_for_child",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<Guid> ResolveTeacherIdAsync(Guid tenantId, Guid teacherOrUserId, Guid? classroomId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarFunctionAsync<Guid>(
            "public.fn_resolve_teacher_id",
            new { TenantId = tenantId, TeacherOrUserId = teacherOrUserId, ClassroomId = classroomId },
            cancellationToken: cancellationToken);
    }
}

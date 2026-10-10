using Dapper;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class MemoryService : IMemoryService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public MemoryService(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<MemoryEvent>> GetChildMemoriesAsync(Guid childId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        // For auth, ensure teacher has access to child. Child must be in a classroom teacher has access to.
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = $@"
        SELECT * FROM (
            -- Child Stories
            SELECT 
                cs.id AS Id,
                cs.tenant_id AS TenantId,
                cs.created_at AS EventDate,
                'Observation' AS EventType,
                'Story' AS Title,
                cs.content AS Description,
                cs.id AS SourceId,
                'child_stories' AS SourceType,
                cs.child_id AS ChildId,
                NULL::uuid AS ClassroomId,
                cs.teacher_id AS TeacherId,
                es.experience_id AS ExperienceId
            FROM child_stories cs
            LEFT JOIN experience_schedules es ON cs.experience_schedule_id = es.id
            WHERE cs.tenant_id = @TenantId AND cs.child_id = @ChildId

            UNION ALL
            
            -- Child Journey Entries
            SELECT 
                je.id AS Id,
                je.tenant_id AS TenantId,
                je.observation_date AS EventDate,
                'Observation' AS EventType,
                COALESCE(je.title, 'Journey Entry') AS Title,
                je.observation AS Description,
                je.id AS SourceId,
                'child_journey_entries' AS SourceType,
                je.child_id AS ChildId,
                je.classroom_id AS ClassroomId,
                je.teacher_id AS TeacherId,
                je.experience_id AS ExperienceId
            FROM child_journey_entries je
            WHERE je.tenant_id = @TenantId AND je.child_id = @ChildId

            UNION ALL

            -- Experience Participations (joins to Session)
            SELECT 
                s.id AS Id,
                s.tenant_id AS TenantId,
                s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::time) AS EventDate,
                'Session' AS EventType,
                'Experience Session' AS Title,
                COALESCE(p.participation_notes, s.actual_notes) AS Description,
                p.id AS SourceId,
                'experience_participations' AS SourceType,
                p.child_id AS ChildId,
                s.classroom_id AS ClassroomId,
                s.primary_teacher_id AS TeacherId,
                s.experience_id AS ExperienceId
            FROM experience_participations p
            JOIN experience_sessions s ON p.experience_session_id = s.id
            WHERE s.tenant_id = @TenantId 
              AND p.child_id = @ChildId 
              AND p.participation_status = 'Participated'
        ) AS combined
        WHERE (@StartDate IS NULL OR EventDate >= @StartDate)
          AND (@EndDate IS NULL OR EventDate <= @EndDate)
        ORDER BY EventDate DESC
        LIMIT @Limit OFFSET @Offset;
        ";

        return await conn.QueryAsync<MemoryEvent>(sql, new { 
            TenantId = _tenantContext.TenantId, 
            ChildId = childId,
            parameters.StartDate,
            parameters.EndDate,
            parameters.Limit,
            parameters.Offset
        });
    }

    public async Task<IEnumerable<MemoryEvent>> GetClassroomMemoriesAsync(Guid classroomId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = $@"
        SELECT * FROM (
            -- Child Journey Entries (linked to classroom)
            SELECT 
                je.id AS Id,
                je.tenant_id AS TenantId,
                je.observation_date AS EventDate,
                'Observation' AS EventType,
                COALESCE(je.title, 'Journey Entry') AS Title,
                je.observation AS Description,
                je.id AS SourceId,
                'child_journey_entries' AS SourceType,
                je.child_id AS ChildId,
                je.classroom_id AS ClassroomId,
                je.teacher_id AS TeacherId,
                je.experience_id AS ExperienceId
            FROM child_journey_entries je
            WHERE je.tenant_id = @TenantId AND je.classroom_id = @ClassroomId

            UNION ALL

            -- Experience Sessions (happened in classroom)
            SELECT 
                s.id AS Id,
                s.tenant_id AS TenantId,
                s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::time) AS EventDate,
                'Session' AS EventType,
                'Experience Session' AS Title,
                s.actual_notes AS Description,
                s.id AS SourceId,
                'experience_sessions' AS SourceType,
                NULL::uuid AS ChildId,
                s.classroom_id AS ClassroomId,
                s.primary_teacher_id AS TeacherId,
                s.experience_id AS ExperienceId
            FROM experience_sessions s
            WHERE s.tenant_id = @TenantId AND s.classroom_id = @ClassroomId

            UNION ALL

            -- Experience Feedbacks (for schedules in classroom)
            SELECT 
                f.id AS Id,
                f.tenant_id AS TenantId,
                f.created_at AS EventDate,
                'Feedback' AS EventType,
                'Teacher Feedback' AS Title,
                f.notes AS Description,
                f.id AS SourceId,
                'experience_feedbacks' AS SourceType,
                NULL::uuid AS ChildId,
                s.classroom_id AS ClassroomId,
                f.teacher_id AS TeacherId,
                s.experience_id AS ExperienceId
            FROM experience_feedback f
            JOIN experience_schedules s ON f.experience_schedule_id = s.id
            WHERE f.tenant_id = @TenantId AND s.classroom_id = @ClassroomId
        ) AS combined
        WHERE (@StartDate IS NULL OR EventDate >= @StartDate)
          AND (@EndDate IS NULL OR EventDate <= @EndDate)
        ORDER BY EventDate DESC
        LIMIT @Limit OFFSET @Offset;
        ";

        return await conn.QueryAsync<MemoryEvent>(sql, new { 
            TenantId = _tenantContext.TenantId, 
            ClassroomId = classroomId,
            parameters.StartDate,
            parameters.EndDate,
            parameters.Limit,
            parameters.Offset
        });
    }

    public async Task<IEnumerable<MemoryEvent>> GetTeacherMemoriesAsync(Guid teacherId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = $@"
        SELECT * FROM (
            -- Child Stories authored
            SELECT 
                cs.id AS Id,
                cs.tenant_id AS TenantId,
                cs.created_at AS EventDate,
                'Observation' AS EventType,
                'Story' AS Title,
                cs.content AS Description,
                cs.id AS SourceId,
                'child_stories' AS SourceType,
                cs.child_id AS ChildId,
                NULL::uuid AS ClassroomId,
                cs.teacher_id AS TeacherId,
                es.experience_id AS ExperienceId
            FROM child_stories cs
            LEFT JOIN experience_schedules es ON cs.experience_schedule_id = es.id
            WHERE cs.tenant_id = @TenantId AND cs.teacher_id = @TeacherId

            UNION ALL
            
            -- Child Journey Entries authored
            SELECT 
                je.id AS Id,
                je.tenant_id AS TenantId,
                je.observation_date AS EventDate,
                'Observation' AS EventType,
                COALESCE(je.title, 'Journey Entry') AS Title,
                je.observation AS Description,
                je.id AS SourceId,
                'child_journey_entries' AS SourceType,
                je.child_id AS ChildId,
                je.classroom_id AS ClassroomId,
                je.teacher_id AS TeacherId,
                je.experience_id AS ExperienceId
            FROM child_journey_entries je
            WHERE je.tenant_id = @TenantId AND je.teacher_id = @TeacherId

            UNION ALL

            -- Experience Sessions led
            SELECT 
                s.id AS Id,
                s.tenant_id AS TenantId,
                s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::time) AS EventDate,
                'Session' AS EventType,
                'Experience Session' AS Title,
                s.actual_notes AS Description,
                s.id AS SourceId,
                'experience_sessions' AS SourceType,
                NULL::uuid AS ChildId,
                s.classroom_id AS ClassroomId,
                s.primary_teacher_id AS TeacherId,
                s.experience_id AS ExperienceId
            FROM experience_sessions s
            WHERE s.tenant_id = @TenantId AND s.primary_teacher_id = @TeacherId

            UNION ALL

            -- Experience Feedbacks given
            SELECT 
                f.id AS Id,
                f.tenant_id AS TenantId,
                f.created_at AS EventDate,
                'Feedback' AS EventType,
                'Teacher Feedback' AS Title,
                f.notes AS Description,
                f.id AS SourceId,
                'experience_feedbacks' AS SourceType,
                NULL::uuid AS ChildId,
                s.classroom_id AS ClassroomId,
                f.teacher_id AS TeacherId,
                s.experience_id AS ExperienceId
            FROM experience_feedback f
            JOIN experience_schedules s ON f.experience_schedule_id = s.id
            WHERE f.tenant_id = @TenantId AND f.teacher_id = @TeacherId
        ) AS combined
        WHERE (@StartDate IS NULL OR EventDate >= @StartDate)
          AND (@EndDate IS NULL OR EventDate <= @EndDate)
        ORDER BY EventDate DESC
        LIMIT @Limit OFFSET @Offset;
        ";

        return await conn.QueryAsync<MemoryEvent>(sql, new { 
            TenantId = _tenantContext.TenantId, 
            TeacherId = teacherId,
            parameters.StartDate,
            parameters.EndDate,
            parameters.Limit,
            parameters.Offset
        });
    }

    public async Task<IEnumerable<MemoryEvent>> GetExperienceMemoriesAsync(Guid experienceId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = $@"
        SELECT * FROM (
            -- Child Stories mapped to this experience
            SELECT 
                cs.id AS Id,
                cs.tenant_id AS TenantId,
                cs.created_at AS EventDate,
                'Observation' AS EventType,
                'Story' AS Title,
                cs.content AS Description,
                cs.id AS SourceId,
                'child_stories' AS SourceType,
                cs.child_id AS ChildId,
                NULL::uuid AS ClassroomId,
                cs.teacher_id AS TeacherId,
                es.experience_id AS ExperienceId
            FROM child_stories cs
            JOIN experience_schedules es ON cs.experience_schedule_id = es.id
            WHERE cs.tenant_id = @TenantId AND es.experience_id = @ExperienceId

            UNION ALL
            
            -- Child Journey Entries
            SELECT 
                je.id AS Id,
                je.tenant_id AS TenantId,
                je.observation_date AS EventDate,
                'Observation' AS EventType,
                COALESCE(je.title, 'Journey Entry') AS Title,
                je.observation AS Description,
                je.id AS SourceId,
                'child_journey_entries' AS SourceType,
                je.child_id AS ChildId,
                je.classroom_id AS ClassroomId,
                je.teacher_id AS TeacherId,
                je.experience_id AS ExperienceId
            FROM child_journey_entries je
            WHERE je.tenant_id = @TenantId AND je.experience_id = @ExperienceId

            UNION ALL

            -- Experience Sessions
            SELECT 
                s.id AS Id,
                s.tenant_id AS TenantId,
                s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::time) AS EventDate,
                'Session' AS EventType,
                'Experience Session' AS Title,
                s.actual_notes AS Description,
                s.id AS SourceId,
                'experience_sessions' AS SourceType,
                NULL::uuid AS ChildId,
                s.classroom_id AS ClassroomId,
                s.primary_teacher_id AS TeacherId,
                s.experience_id AS ExperienceId
            FROM experience_sessions s
            WHERE s.tenant_id = @TenantId AND s.experience_id = @ExperienceId

            UNION ALL

            -- Experience Feedbacks
            SELECT 
                f.id AS Id,
                f.tenant_id AS TenantId,
                f.created_at AS EventDate,
                'Feedback' AS EventType,
                'Teacher Feedback' AS Title,
                f.notes AS Description,
                f.id AS SourceId,
                'experience_feedbacks' AS SourceType,
                NULL::uuid AS ChildId,
                s.classroom_id AS ClassroomId,
                f.teacher_id AS TeacherId,
                s.experience_id AS ExperienceId
            FROM experience_feedback f
            JOIN experience_schedules s ON f.experience_schedule_id = s.id
            WHERE f.tenant_id = @TenantId AND s.experience_id = @ExperienceId
        ) AS combined
        WHERE (@StartDate IS NULL OR EventDate >= @StartDate)
          AND (@EndDate IS NULL OR EventDate <= @EndDate)
        ORDER BY EventDate DESC
        LIMIT @Limit OFFSET @Offset;
        ";

        return await conn.QueryAsync<MemoryEvent>(sql, new { 
            TenantId = _tenantContext.TenantId, 
            ExperienceId = experienceId,
            parameters.StartDate,
            parameters.EndDate,
            parameters.Limit,
            parameters.Offset
        });
    }
}

using Dapper;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class MemoryService : IMemoryService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext? _userContext;

    public MemoryService(IDbConnectionFactory connectionFactory, ITenantContext tenantContext, IUserContext? userContext = null)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
        _userContext = userContext;
    }

    public async Task<IEnumerable<MemoryEvent>> GetChildMemoriesAsync(Guid childId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        bool isParent = _userContext?.Roles?.Contains("Parent") ?? false;

        var sql = $@"
        SELECT * FROM (
            -- 1. Child Stories (Stories & Observations)
            SELECT 
                cs.id AS Id,
                cs.tenant_id AS TenantId,
                cs.created_at AS EventDate,
                'Observation' AS EventType,
                'Observations' AS Category,
                'Story' AS Title,
                cs.content AS Description,
                cs.id AS SourceId,
                'child_stories' AS SourceType,
                cs.child_id AS ChildId,
                c.first_name || ' ' || c.last_name AS ChildName,
                NULL::uuid AS ClassroomId,
                NULL::text AS ClassroomName,
                cs.teacher_id AS TeacherId,
                COALESCE(t.first_name || ' ' || t.last_name, 'Teacher') AS TeacherName,
                es.experience_id AS ExperienceId,
                e.title AS ExperienceTitle,
                NULL::text AS MediaUrl,
                TRUE AS IsParentVisible,
                cs.status AS Status,
                'bi-journal-text' AS CategoryIcon
            FROM child_stories cs
            JOIN children c ON cs.child_id = c.id
            LEFT JOIN experience_schedules es ON cs.experience_schedule_id = es.id
            LEFT JOIN experiences e ON es.experience_id = e.id
            LEFT JOIN teachers t ON cs.teacher_id = t.id
            WHERE cs.tenant_id = @TenantId AND cs.child_id = @ChildId
              AND (@IsParent = FALSE OR cs.status = 'Approved')

            UNION ALL
            
            -- 2. Child Journey Entries (Milestones, Moments, and Observations with attached evidence photos)
            SELECT 
                je.id AS Id,
                je.tenant_id AS TenantId,
                je.observation_date AS EventDate,
                CASE WHEN je.is_milestone THEN 'Milestone' ELSE 'Observation' END AS EventType,
                CASE 
                    WHEN je.is_milestone THEN 'Milestones' 
                    WHEN je.journey_type = 'Moment' THEN 'Observations'
                    ELSE 'Observations' 
                END AS Category,
                COALESCE(je.title, 'Journey Entry') AS Title,
                je.observation AS Description,
                je.id AS SourceId,
                'child_journey_entries' AS SourceType,
                je.child_id AS ChildId,
                c.first_name || ' ' || c.last_name AS ChildName,
                je.classroom_id AS ClassroomId,
                cr.name AS ClassroomName,
                je.teacher_id AS TeacherId,
                COALESCE(t.first_name || ' ' || t.last_name, 'Teacher') AS TeacherName,
                je.experience_id AS ExperienceId,
                e.title AS ExperienceTitle,
                ev.text_content AS MediaUrl,
                je.is_parent_visible AS IsParentVisible,
                je.parent_visibility_status AS Status,
                CASE WHEN je.is_milestone THEN 'bi-trophy' ELSE 'bi-compass' END AS CategoryIcon
            FROM child_journey_entries je
            JOIN children c ON je.child_id = c.id
            LEFT JOIN classrooms cr ON je.classroom_id = cr.id
            LEFT JOIN experiences e ON je.experience_id = e.id
            LEFT JOIN teachers t ON je.teacher_id = t.id
            LEFT JOIN LATERAL (
                SELECT text_content 
                FROM child_journey_evidence 
                WHERE journey_entry_id = je.id 
                ORDER BY created_at DESC 
                LIMIT 1
            ) ev ON TRUE
            WHERE je.tenant_id = @TenantId AND je.child_id = @ChildId
              AND (@IsParent = FALSE OR (je.is_parent_visible = TRUE AND (je.parent_visibility_status = 'Approved' OR je.parent_visibility_status IS NULL)))

            UNION ALL

            -- 3. Experience Participations (Completed learning experiences)
            SELECT 
                s.id AS Id,
                s.tenant_id AS TenantId,
                s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::time) AS EventDate,
                'Session' AS EventType,
                'Learning Experiences' AS Category,
                COALESCE(e.title, 'Experience Session') AS Title,
                COALESCE(p.participation_notes, s.actual_notes, 'Child participated in this learning experience.') AS Description,
                p.id AS SourceId,
                'experience_participations' AS SourceType,
                p.child_id AS ChildId,
                c.first_name || ' ' || c.last_name AS ChildName,
                s.classroom_id AS ClassroomId,
                cr.name AS ClassroomName,
                s.primary_teacher_id AS TeacherId,
                COALESCE(t.first_name || ' ' || t.last_name, 'Teacher') AS TeacherName,
                s.experience_id AS ExperienceId,
                e.title AS ExperienceTitle,
                NULL::text AS MediaUrl,
                TRUE AS IsParentVisible,
                p.participation_status AS Status,
                'bi-palette' AS CategoryIcon
            FROM experience_participations p
            JOIN experience_sessions s ON p.experience_session_id = s.id
            JOIN children c ON p.child_id = c.id
            LEFT JOIN classrooms cr ON s.classroom_id = cr.id
            LEFT JOIN experiences e ON s.experience_id = e.id
            LEFT JOIN teachers t ON s.primary_teacher_id = t.id
            WHERE s.tenant_id = @TenantId 
              AND p.child_id = @ChildId 
              AND p.participation_status = 'Participated'

            UNION ALL

            -- 4. Child Milestones (Standalone development milestones)
            SELECT
                m.id AS Id,
                m.tenant_id AS TenantId,
                m.milestone_date AS EventDate,
                'Milestone' AS EventType,
                'Milestones' AS Category,
                m.title AS Title,
                COALESCE(m.description, m.evidence_summary, 'Developmental milestone achieved.') AS Description,
                m.id AS SourceId,
                'child_milestones' AS SourceType,
                m.child_id AS ChildId,
                c.first_name || ' ' || c.last_name AS ChildName,
                m.classroom_id AS ClassroomId,
                cr.name AS ClassroomName,
                m.teacher_id AS TeacherId,
                COALESCE(t.first_name || ' ' || t.last_name, 'Teacher') AS TeacherName,
                NULL::uuid AS ExperienceId,
                NULL::text AS ExperienceTitle,
                NULL::text AS MediaUrl,
                m.parent_visible AS IsParentVisible,
                m.status AS Status,
                'bi-star-fill' AS CategoryIcon
            FROM child_milestones m
            JOIN children c ON m.child_id = c.id
            LEFT JOIN classrooms cr ON m.classroom_id = cr.id
            LEFT JOIN teachers t ON m.teacher_id = t.id
            WHERE m.tenant_id = @TenantId AND m.child_id = @ChildId
              AND (@IsParent = FALSE OR m.parent_visible = TRUE)

            UNION ALL

            -- 5. Celebrations & Events (Approved RSVP celebrations)
            SELECT
                r.id AS Id,
                ev.tenant_id AS TenantId,
                (ev.event_date + COALESCE(ev.start_time, '09:00:00'::time))::timestamptz AS EventDate,
                'Celebration' AS EventType,
                'Celebrations' AS Category,
                ev.title AS Title,
                COALESCE(ev.description, r.notes, 'Celebration & Daycare Event') AS Description,
                ev.id AS SourceId,
                'events' AS SourceType,
                r.child_id AS ChildId,
                c.first_name || ' ' || c.last_name AS ChildName,
                NULL::uuid AS ClassroomId,
                NULL::text AS ClassroomName,
                NULL::uuid AS TeacherId,
                'Daycare Staff' AS TeacherName,
                NULL::uuid AS ExperienceId,
                NULL::text AS ExperienceTitle,
                NULL::text AS MediaUrl,
                TRUE AS IsParentVisible,
                r.status AS Status,
                'bi-balloon-heart' AS CategoryIcon
            FROM event_rsvps r
            JOIN events ev ON r.event_id = ev.id
            JOIN children c ON r.child_id = c.id
            WHERE ev.tenant_id = @TenantId 
              AND r.child_id = @ChildId
              AND ev.status = 'Published'
              AND r.status IN ('Accepted', 'Attended', 'Confirmed')

            UNION ALL

            -- 6. Child Daily Care & Wellbeing (Comfort & Positive Mood Highlights)
            SELECT
                ce.id AS Id,
                ce.tenant_id AS TenantId,
                ce.event_time AS EventDate,
                'DailyCare' AS EventType,
                'Daily Care' AS Category,
                'Wellbeing Highlight: ' || ce.comfort_mood AS Title,
                COALESCE(ce.notes, 'Positive daily care & comfort milestone.') AS Description,
                ce.id AS SourceId,
                'child_care_events' AS SourceType,
                ce.child_id AS ChildId,
                c.first_name || ' ' || c.last_name AS ChildName,
                ce.classroom_id AS ClassroomId,
                cr.name AS ClassroomName,
                ce.recorded_by_user_id AS TeacherId,
                ce.recorded_by_name AS TeacherName,
                NULL::uuid AS ExperienceId,
                NULL::text AS ExperienceTitle,
                NULL::text AS MediaUrl,
                ce.parent_visible AS IsParentVisible,
                'Recorded' AS Status,
                'bi-sun' AS CategoryIcon
            FROM child_care_events ce
            JOIN children c ON ce.child_id = c.id
            LEFT JOIN classrooms cr ON ce.classroom_id = cr.id
            WHERE ce.tenant_id = @TenantId 
              AND ce.child_id = @ChildId
              AND ce.event_type = 'Comfort'
              AND ce.comfort_mood IN ('Happy', 'Settled')
              AND (@IsParent = FALSE OR ce.parent_visible = TRUE)
        ) AS combined
        WHERE (@StartDate IS NULL OR EventDate >= @StartDate)
          AND (@EndDate IS NULL OR EventDate <= @EndDate)
          AND (@Category IS NULL OR @Category = 'All' OR Category = @Category)
          AND (@SearchQuery IS NULL OR @SearchQuery = '' OR Title ILIKE '%' || @SearchQuery || '%' OR Description ILIKE '%' || @SearchQuery || '%')
          AND (@IsParent = FALSE OR IsParentVisible = TRUE)
        ORDER BY EventDate DESC
        LIMIT @Limit OFFSET @Offset;
        ";

        return await conn.QueryAsync<MemoryEvent>(sql, new { 
            TenantId = _tenantContext.TenantId, 
            ChildId = childId,
            IsParent = isParent,
            parameters.StartDate,
            parameters.EndDate,
            parameters.Category,
            parameters.SearchQuery,
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

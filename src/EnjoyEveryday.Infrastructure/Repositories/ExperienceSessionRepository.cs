using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ExperienceSessionRepository : IExperienceSessionRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ExperienceSessionRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<ExperienceSession?> GetSessionByIdAsync(Guid tenantId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id as Id, tenant_id as TenantId, experience_id as ExperienceId, 
                   experience_schedule_id as ExperienceScheduleId, classroom_id as ClassroomId, 
                   session_date as SessionDate, start_time as StartTime, end_time as EndTime, 
                   status as Status, primary_teacher_id as PrimaryTeacherId, actual_notes as ActualNotes, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM experience_sessions
            WHERE id = @SessionId AND tenant_id = @TenantId";
        
        return await connection.QuerySingleOrDefaultAsync<ExperienceSession>(sql, new { SessionId = sessionId, TenantId = tenantId });
    }

    public async Task<IEnumerable<ExperienceSession>> GetSessionsForClassroomAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id as Id, tenant_id as TenantId, experience_id as ExperienceId, 
                   experience_schedule_id as ExperienceScheduleId, classroom_id as ClassroomId, 
                   session_date as SessionDate, start_time as StartTime, end_time as EndTime, 
                   status as Status, primary_teacher_id as PrimaryTeacherId, actual_notes as ActualNotes, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM experience_sessions
            WHERE classroom_id = @ClassroomId AND tenant_id = @TenantId
            ORDER BY session_date DESC, start_time DESC";
            
        return await connection.QueryAsync<ExperienceSession>(sql, new { ClassroomId = classroomId, TenantId = tenantId });
    }

    public async Task<ExperienceSession?> GetSessionByScheduleIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id as Id, tenant_id as TenantId, experience_id as ExperienceId, 
                   experience_schedule_id as ExperienceScheduleId, classroom_id as ClassroomId, 
                   session_date as SessionDate, start_time as StartTime, end_time as EndTime, 
                   status as Status, primary_teacher_id as PrimaryTeacherId, actual_notes as ActualNotes, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM experience_sessions
            WHERE experience_schedule_id = @ScheduleId";
        
        return await connection.QuerySingleOrDefaultAsync<ExperienceSession>(sql, new { ScheduleId = scheduleId });
    }

    public async Task CreateSessionAsync(ExperienceSession session, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO experience_sessions (
                id, tenant_id, experience_id, experience_schedule_id, classroom_id, 
                session_date, start_time, end_time, status, primary_teacher_id, actual_notes
            ) VALUES (
                @Id, @TenantId, @ExperienceId, @ExperienceScheduleId, @ClassroomId, 
                @SessionDate, @StartTime, @EndTime, @Status, @PrimaryTeacherId, @ActualNotes
            )";
            
        await connection.ExecuteAsync(sql, session);
    }

    public async Task UpdateSessionAsync(ExperienceSession session, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE experience_sessions SET
                status = @Status,
                actual_notes = @ActualNotes,
                start_time = @StartTime,
                end_time = @EndTime,
                updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId";
            
        await connection.ExecuteAsync(sql, session);
    }

    public async Task<IEnumerable<ExperienceParticipation>> GetParticipationsForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id as Id, experience_session_id as ExperienceSessionId, 
                   child_id as ChildId, participation_status as ParticipationStatus, 
                   participation_notes as ParticipationNotes, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM experience_participations
            WHERE experience_session_id = @SessionId";
            
        return await connection.QueryAsync<ExperienceParticipation>(sql, new { SessionId = sessionId });
    }

    public async Task SaveParticipationsAsync(Guid sessionId, IEnumerable<ExperienceParticipation> participations, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        
        // Simple approach for MVP: Delete existing and insert new
        await connection.ExecuteAsync("DELETE FROM experience_participations WHERE experience_session_id = @SessionId", new { SessionId = sessionId });
        
        if (participations != null && participations.Any())
        {
            const string sql = @"
                INSERT INTO experience_participations (
                    id, experience_session_id, child_id, participation_status, participation_notes
                ) VALUES (
                    @Id, @ExperienceSessionId, @ChildId, @ParticipationStatus, @ParticipationNotes
                )";
                
            foreach (var p in participations)
            {
                if (p.Id == Guid.Empty) p.Id = Guid.NewGuid();
                p.ExperienceSessionId = sessionId;
            }
            
            await connection.ExecuteAsync(sql, participations);
        }
    }
}

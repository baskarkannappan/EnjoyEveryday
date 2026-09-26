using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ExperienceFeedbackRepository : IExperienceFeedbackRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ExperienceFeedbackRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<ExperienceFeedback?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            SELECT id, tenant_id as TenantId, experience_schedule_id as ExperienceScheduleId,
                   teacher_id as TeacherId, rating as Rating, notes as Notes, created_at as CreatedAt
            FROM experience_feedback
            WHERE id = @Id AND tenant_id = @TenantId";
        return await connection.QuerySingleOrDefaultAsync<ExperienceFeedback>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<ExperienceFeedback>> GetByScheduleIdAsync(Guid tenantId, Guid scheduleId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            SELECT id, tenant_id as TenantId, experience_schedule_id as ExperienceScheduleId,
                   teacher_id as TeacherId, rating as Rating, notes as Notes, created_at as CreatedAt
            FROM experience_feedback
            WHERE experience_schedule_id = @ScheduleId AND tenant_id = @TenantId
            ORDER BY created_at DESC";
        return await connection.QueryAsync<ExperienceFeedback>(sql, new { ScheduleId = scheduleId, TenantId = tenantId });
    }

    public async Task<ExperienceFeedback> AddAsync(ExperienceFeedback feedback, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            INSERT INTO experience_feedback (id, tenant_id, experience_schedule_id, teacher_id, rating, notes, created_at)
            VALUES (@Id, @TenantId, @ExperienceScheduleId, @TeacherId, @Rating, @Notes, @CreatedAt)";
        
        await connection.ExecuteAsync(sql, feedback);
        return feedback;
    }

    public async Task<IEnumerable<ExperienceFeedback>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            SELECT id, tenant_id as TenantId, experience_schedule_id as ExperienceScheduleId,
                   teacher_id as TeacherId, rating as Rating, notes as Notes, created_at as CreatedAt
            FROM experience_feedback
            WHERE tenant_id = @TenantId
            ORDER BY created_at DESC";
        return await connection.QueryAsync<ExperienceFeedback>(sql, new { TenantId = tenantId });
    }

    public async Task UpdateAsync(ExperienceFeedback feedback, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            UPDATE experience_feedback
            SET rating = @Rating, notes = @Notes
            WHERE id = @Id AND tenant_id = @TenantId";
        await connection.ExecuteAsync(sql, feedback);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = "DELETE FROM experience_feedback WHERE id = @Id AND tenant_id = @TenantId";
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }
}


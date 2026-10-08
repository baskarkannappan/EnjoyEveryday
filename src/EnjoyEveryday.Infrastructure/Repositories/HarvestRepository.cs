using Dapper;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class HarvestRepository : IHarvestRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public HarvestRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<HarvestMetricsDto> GetMonthlyMetricsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        var sqlExp = @"
            SELECT COUNT(1) 
            FROM experience_schedules 
            WHERE tenant_id = @TenantId AND extract(month from scheduled_date) = extract(month from current_date) AND extract(year from scheduled_date) = extract(year from current_date)";
        var totalExp = await connection.QuerySingleAsync<int>(sqlExp, new { TenantId = tenantId });

        var sqlFeedback = @"
            SELECT COUNT(1) 
            FROM experience_feedback f
            JOIN experience_schedules s ON f.experience_schedule_id = s.id
            WHERE f.tenant_id = @TenantId AND extract(month from s.scheduled_date) = extract(month from current_date) AND extract(year from s.scheduled_date) = extract(year from current_date)";
        var totalFeedback = await connection.QuerySingleAsync<int>(sqlFeedback, new { TenantId = tenantId });

        return new HarvestMetricsDto
        {
            TotalExperiences = totalExp,
            TotalFeedbacks = totalFeedback
        };
    }

    public async Task<IEnumerable<HarvestPerformanceItemDto>> GetPerformanceItemsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = @"
            SELECT 
                e.id as ExperienceId,
                e.title as Title,
                'Created by ' || (SELECT first_name FROM users WHERE id = e.created_by_user_id) as Subtitle,
                (SELECT COUNT(1) FROM experience_schedules WHERE experience_id = e.id) as Uses,
                '💬 ' || (SELECT COUNT(1) FROM experience_feedback f JOIN experience_schedules s ON f.experience_schedule_id = s.id WHERE s.experience_id = e.id) as Response
            FROM experiences e
            WHERE e.tenant_id = @TenantId
            ORDER BY Uses DESC
            LIMIT 10";

        return await connection.QueryAsync<HarvestPerformanceItemDto>(sql, new { TenantId = tenantId });
    }
}

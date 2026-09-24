using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ExperienceScheduleRepository : IExperienceScheduleRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ITenantContext _tenantContext;

    public ExperienceScheduleRepository(IDbConnectionFactory dbConnectionFactory, ITenantContext tenantContext)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<ExperienceSchedule>> GetByClassroomAndDateRangeAsync(Guid classroomId, DateOnly startDate, DateOnly endDate)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var sql = @"
            SELECT id, tenant_id as TenantId, experience_id as ExperienceId, classroom_id as ClassroomId, 
                   scheduled_date as ScheduledDate, time_of_day as TimeOfDay, status as Status, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM experience_schedules
            WHERE tenant_id = @TenantId AND classroom_id = @ClassroomId AND scheduled_date >= @StartDate AND scheduled_date <= @EndDate
            ORDER BY scheduled_date ASC";

        return await connection.QueryAsync<ExperienceSchedule>(sql, new { TenantId = _tenantContext.TenantId, ClassroomId = classroomId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<ExperienceSchedule?> GetByIdAsync(Guid id)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var sql = @"
            SELECT id, tenant_id as TenantId, experience_id as ExperienceId, classroom_id as ClassroomId, 
                   scheduled_date as ScheduledDate, time_of_day as TimeOfDay, status as Status, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM experience_schedules
            WHERE id = @Id AND tenant_id = @TenantId";

        return await connection.QuerySingleOrDefaultAsync<ExperienceSchedule>(sql, new { Id = id, TenantId = _tenantContext.TenantId });
    }

    public async Task<ExperienceSchedule> CreateAsync(ExperienceSchedule schedule)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        schedule.TenantId = _tenantContext.TenantId;
        
        var sql = @"
            INSERT INTO experience_schedules (id, tenant_id, experience_id, classroom_id, scheduled_date, time_of_day, status, created_at, updated_at)
            VALUES (@Id, @TenantId, @ExperienceId, @ClassroomId, @ScheduledDate, @TimeOfDay, @Status, @CreatedAt, @UpdatedAt)
            RETURNING id, tenant_id as TenantId, experience_id as ExperienceId, classroom_id as ClassroomId, 
                      scheduled_date as ScheduledDate, time_of_day as TimeOfDay, status as Status, 
                      created_at as CreatedAt, updated_at as UpdatedAt";

        return await connection.QuerySingleAsync<ExperienceSchedule>(sql, schedule);
    }

    public async Task UpdateAsync(ExperienceSchedule schedule)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        schedule.UpdatedAt = DateTime.UtcNow;
        
        var sql = @"
            UPDATE experience_schedules
            SET scheduled_date = @ScheduledDate,
                time_of_day = @TimeOfDay,
                status = @Status,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";

        await connection.ExecuteAsync(sql, schedule);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var sql = "DELETE FROM experience_schedules WHERE id = @Id AND tenant_id = @TenantId";
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = _tenantContext.TenantId });
    }
}

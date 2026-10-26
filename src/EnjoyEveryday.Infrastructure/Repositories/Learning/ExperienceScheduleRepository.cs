using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
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

    private static readonly string[] CreateScheduleParams =
    [
        "Id", "TenantId", "ExperienceId", "ClassroomId", "ScheduledDate", "TimeOfDay",
        "Status", "PrimaryTeacherId", "PlannedStartTime", "PlannedEndTime", "PreparationNotes",
        "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] UpdateScheduleParams =
    [
        "Id", "TenantId", "ScheduledDate", "TimeOfDay", "Status",
        "PrimaryTeacherId", "PlannedStartTime", "PlannedEndTime", "PreparationNotes", "UpdatedAt"
    ];

    public async Task<IEnumerable<ExperienceSchedule>> GetByClassroomAndDateRangeAsync(Guid classroomId, DateOnly startDate, DateOnly endDate)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<ExperienceSchedule>(
            "public.fn_get_experience_schedules_by_classroom_and_date_range",
            new
            {
                TenantId = _tenantContext.TenantId,
                ClassroomId = classroomId,
                StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                EndDate = endDate.ToDateTime(TimeOnly.MinValue)
            });
    }

    public async Task<IEnumerable<ExperienceSchedule>> GetByDateAsync(DateOnly date)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<ExperienceSchedule>(
            "public.fn_get_experience_schedules_by_date",
            new
            {
                TenantId = _tenantContext.TenantId,
                Date = date.ToDateTime(TimeOnly.MinValue)
            });
    }

    public async Task<IEnumerable<ExperienceSchedule>> GetFilteredSchedulesAsync(Guid tenantId, DateOnly date, Guid? organizationId = null, Guid? branchId = null, Guid? classroomId = null)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<ExperienceSchedule>(
            "public.fn_get_filtered_experience_schedules",
            new
            {
                TenantId = tenantId,
                Date = date.ToDateTime(TimeOnly.MinValue),
                OrgId = organizationId,
                BranchId = branchId,
                ClassroomId = classroomId
            });
    }

    public async Task<ExperienceSchedule?> GetByIdAsync(Guid id)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleFunctionAsync<ExperienceSchedule>(
            "public.fn_get_experience_schedule_by_id",
            new { TenantId = _tenantContext.TenantId, Id = id });
    }

    public async Task<ExperienceSchedule> CreateAsync(ExperienceSchedule schedule)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        schedule.TenantId = _tenantContext.TenantId;

        var created = await connection.QuerySingleFunctionAsync<ExperienceSchedule>(
            "public.fn_create_experience_schedule",
            schedule,
            parameterNames: CreateScheduleParams);

        return created ?? schedule;
    }

    public async Task UpdateAsync(ExperienceSchedule schedule)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        schedule.UpdatedAt = DateTime.UtcNow;

        await connection.ExecuteProcedureAsync(
            "public.sp_update_experience_schedule",
            schedule,
            parameterNames: UpdateScheduleParams);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_experience_schedule",
            new { TenantId = _tenantContext.TenantId, Id = id });
    }
}

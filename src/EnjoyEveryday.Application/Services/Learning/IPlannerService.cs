using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Learning;

namespace EnjoyEveryday.Application.Services;

public interface IPlannerService
{
    Task<IEnumerable<ExperienceSchedule>> GetMonthlyScheduleAsync(Guid classroomId, int year, int month);
    Task<PlanningCalendarSummaryDto> GetMonthlyCalendarSummaryAsync(Guid classroomId, int year, int month, CancellationToken cancellationToken = default);
    Task<OrderMissingMaterialsResultDto> OrderMissingMaterialsForScheduleAsync(OrderMissingMaterialsRequest request, CancellationToken cancellationToken = default);
    Task<ExperienceSchedule?> GetScheduleAsync(Guid scheduleId);
    Task<IEnumerable<ExperienceSchedule>> GetSchedulesForDateAsync(DateOnly date);
    Task<ExperienceSchedule> ScheduleExperienceAsync(Guid experienceId, Guid classroomId, DateOnly scheduledDate, string timeOfDay, Guid? primaryTeacherId = null, TimeSpan? startTime = null, TimeSpan? endTime = null, string? notes = null);
    Task UpdateScheduleAsync(ExperienceSchedule schedule);
    Task ConfirmScheduleAsync(Guid scheduleId);
    Task CompleteScheduleAsync(Guid scheduleId, string? notes = null);
    Task CancelScheduleAsync(Guid scheduleId);
    Task DeleteScheduleAsync(Guid id);
    bool IsDaycareClosedForPlanning(DateOnly scheduledDate, out string reason);
    Task<TodayActivitiesFeedDto> GetTodayActivitiesFeedAsync(DateOnly date, Guid? branchId = null, Guid? classroomId = null, CancellationToken cancellationToken = default);
}

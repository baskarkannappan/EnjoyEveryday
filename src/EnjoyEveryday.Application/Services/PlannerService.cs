using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;

namespace EnjoyEveryday.Application.Services;

public class PlannerService
{
    private readonly IExperienceScheduleRepository _scheduleRepository;

    public PlannerService(IExperienceScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    public async Task<IEnumerable<ExperienceSchedule>> GetMonthlyScheduleAsync(Guid classroomId, int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return await _scheduleRepository.GetByClassroomAndDateRangeAsync(classroomId, startDate, endDate);
    }

    public async Task<ExperienceSchedule> ScheduleExperienceAsync(Guid experienceId, Guid classroomId, DateOnly scheduledDate, string timeOfDay)
    {
        var schedule = new ExperienceSchedule
        {
            ExperienceId = experienceId,
            ClassroomId = classroomId,
            ScheduledDate = scheduledDate,
            TimeOfDay = timeOfDay,
            Status = "Scheduled"
        };

        return await _scheduleRepository.CreateAsync(schedule);
    }

    public async Task DeleteScheduleAsync(Guid id)
    {
        await _scheduleRepository.DeleteAsync(id);
    }
}

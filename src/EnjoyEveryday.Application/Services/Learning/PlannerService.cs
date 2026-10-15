using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class PlannerService
{
    private readonly IExperienceScheduleRepository _scheduleRepository;
    private readonly IUserContext _userContext;

    public PlannerService(IExperienceScheduleRepository scheduleRepository, IUserContext userContext)
    {
        _scheduleRepository = scheduleRepository;
        _userContext = userContext;
    }

    public async Task<IEnumerable<ExperienceSchedule>> GetMonthlyScheduleAsync(Guid classroomId, int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return await _scheduleRepository.GetByClassroomAndDateRangeAsync(classroomId, startDate, endDate);
    }

    public async Task<ExperienceSchedule?> GetScheduleAsync(Guid scheduleId)
    {
        return await _scheduleRepository.GetByIdAsync(scheduleId);
    }

    public async Task<ExperienceSchedule> ScheduleExperienceAsync(Guid experienceId, Guid classroomId, DateOnly scheduledDate, string timeOfDay, Guid? primaryTeacherId = null, TimeSpan? startTime = null, TimeSpan? endTime = null, string? notes = null)
    {
        if (!_userContext.HasPermission("experience.schedule")) throw new UnauthorizedAccessException("Requires experience.schedule permission.");

        // Basic validation
        if (startTime.HasValue && endTime.HasValue && endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        // Conflict Validation
        var existingSchedules = await _scheduleRepository.GetByDateAsync(scheduledDate);
        foreach (var existing in existingSchedules)
        {
            if (existing.Status == "Cancelled") continue;

            // Simple time overlap check if both have explicit times
            bool timeOverlaps = false;
            if (startTime.HasValue && endTime.HasValue && existing.PlannedStartTime.HasValue && existing.PlannedEndTime.HasValue)
            {
                timeOverlaps = startTime < existing.PlannedEndTime && endTime > existing.PlannedStartTime;
            }

            if (timeOverlaps)
            {
                if (existing.ClassroomId == classroomId)
                    throw new InvalidOperationException($"Classroom already has an experience scheduled at this time.");

                if (primaryTeacherId.HasValue && existing.PrimaryTeacherId == primaryTeacherId)
                    throw new InvalidOperationException($"Teacher is already assigned to another experience at this time.");
            }
        }

        var schedule = new ExperienceSchedule
        {
            ExperienceId = experienceId,
            ClassroomId = classroomId,
            ScheduledDate = scheduledDate,
            TimeOfDay = timeOfDay,
            PrimaryTeacherId = primaryTeacherId,
            PlannedStartTime = startTime,
            PlannedEndTime = endTime,
            PreparationNotes = notes,
            Status = "Planned"
        };

        return await _scheduleRepository.CreateAsync(schedule);
    }

        public async Task UpdateScheduleAsync(ExperienceSchedule schedule)
    {
        if (!_userContext.HasPermission("experience.schedule")) throw new UnauthorizedAccessException("Requires experience.schedule permission.");
        if (schedule.PlannedStartTime.HasValue && schedule.PlannedEndTime.HasValue && schedule.PlannedEndTime <= schedule.PlannedStartTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }
        await _scheduleRepository.UpdateAsync(schedule);
    }

    public async Task ConfirmScheduleAsync(Guid scheduleId)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule != null && schedule.Status == "Planned")
        {
            schedule.Status = "Confirmed";
            await _scheduleRepository.UpdateAsync(schedule);
        }
    }

    public async Task CancelScheduleAsync(Guid scheduleId)
    {
        if (!_userContext.HasPermission("experience.schedule")) throw new UnauthorizedAccessException("Requires experience.schedule permission.");
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule != null)
        {
            schedule.Status = "Cancelled";
            await _scheduleRepository.UpdateAsync(schedule);
        }
    }

    public async Task DeleteScheduleAsync(Guid id)
    {
        if (!_userContext.HasPermission("experience.schedule")) throw new UnauthorizedAccessException("Requires experience.schedule permission.");
        await _scheduleRepository.DeleteAsync(id);
    }
}


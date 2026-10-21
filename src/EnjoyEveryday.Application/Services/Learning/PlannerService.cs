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

    public async Task<IEnumerable<ExperienceSchedule>> GetSchedulesForDateAsync(DateOnly date)
    {
        return await _scheduleRepository.GetByDateAsync(date);
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
        await ValidateScheduleConflictsAsync(classroomId, scheduledDate, timeOfDay, primaryTeacherId, startTime, endTime, excludeScheduleId: null);

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

        await ValidateScheduleConflictsAsync(
            schedule.ClassroomId, 
            schedule.ScheduledDate, 
            schedule.TimeOfDay, 
            schedule.PrimaryTeacherId, 
            schedule.PlannedStartTime, 
            schedule.PlannedEndTime, 
            excludeScheduleId: schedule.Id);

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

    public async Task CompleteScheduleAsync(Guid scheduleId, string? notes = null)
    {
        if (!_userContext.HasPermission("experience.schedule") && !_userContext.HasPermission("experience.execute")) 
            throw new UnauthorizedAccessException("Requires experience.schedule or experience.execute permission.");

        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule != null)
        {
            schedule.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(notes))
            {
                schedule.PreparationNotes = string.IsNullOrWhiteSpace(schedule.PreparationNotes)
                    ? $"Completed: {notes}"
                    : $"{schedule.PreparationNotes}\nCompleted: {notes}";
            }
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

    private async Task ValidateScheduleConflictsAsync(
        Guid classroomId,
        DateOnly scheduledDate,
        string timeOfDay,
        Guid? primaryTeacherId,
        TimeSpan? startTime,
        TimeSpan? endTime,
        Guid? excludeScheduleId = null)
    {
        var existingSchedules = await _scheduleRepository.GetByDateAsync(scheduledDate);
        foreach (var existing in existingSchedules)
        {
            if (excludeScheduleId.HasValue && existing.Id == excludeScheduleId.Value) continue;
            if (string.Equals(existing.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) continue;

            bool timeOverlaps = false;
            if (startTime.HasValue && endTime.HasValue && existing.PlannedStartTime.HasValue && existing.PlannedEndTime.HasValue)
            {
                timeOverlaps = startTime < existing.PlannedEndTime && endTime > existing.PlannedStartTime;
            }
            else
            {
                // If explicit time range is not specified on both, match by TimeOfDay ("Morning", "Mid-Day", "Afternoon", etc.)
                timeOverlaps = string.IsNullOrWhiteSpace(timeOfDay) || 
                               string.IsNullOrWhiteSpace(existing.TimeOfDay) || 
                               string.Equals(timeOfDay.Trim(), existing.TimeOfDay.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            if (timeOverlaps)
            {
                if (existing.ClassroomId == classroomId)
                {
                    throw new InvalidOperationException($"Classroom and its students already have an experience scheduled for this time ({timeOfDay}). A classroom or student can be present in only one planned schedule event at a time.");
                }

                if (primaryTeacherId.HasValue && existing.PrimaryTeacherId.HasValue && existing.PrimaryTeacherId == primaryTeacherId)
                {
                    throw new InvalidOperationException($"Teacher is already assigned to another experience during this time ({timeOfDay}). A teacher can be present in only one planned schedule event at a time.");
                }
            }
        }
    }
}


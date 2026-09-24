using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ExecutionService
{
    private readonly IExperienceScheduleRepository _scheduleRepository;
    private readonly IExperienceFeedbackRepository _feedbackRepository;
    private readonly ITenantContext _tenantContext;

    public ExecutionService(
        IExperienceScheduleRepository scheduleRepository,
        IExperienceFeedbackRepository feedbackRepository,
        ITenantContext tenantContext)
    {
        _scheduleRepository = scheduleRepository;
        _feedbackRepository = feedbackRepository;
        _tenantContext = tenantContext;
    }

    public async Task StartExperienceAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule == null || schedule.TenantId != tenantId)
            throw new KeyNotFoundException("Schedule not found.");

        schedule.Status = "Started";
        await _scheduleRepository.UpdateAsync(schedule);
    }

    public async Task CompleteExperienceAsync(Guid scheduleId, Guid teacherId, string rating, string? notes, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule == null || schedule.TenantId != tenantId)
            throw new KeyNotFoundException("Schedule not found.");

        schedule.Status = "Completed";
        await _scheduleRepository.UpdateAsync(schedule);

        var feedback = new ExperienceFeedback
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExperienceScheduleId = scheduleId,
            TeacherId = teacherId,
            Rating = rating,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _feedbackRepository.AddAsync(feedback, cancellationToken);
    }
}

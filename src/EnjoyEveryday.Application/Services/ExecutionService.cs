using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ExecutionService
{
    private readonly IExperienceScheduleRepository _scheduleRepository;
    private readonly IExperienceFeedbackRepository _feedbackRepository;
    private readonly IExperienceSessionRepository _sessionRepository;
    private readonly ITenantContext _tenantContext;

    public ExecutionService(
        IExperienceScheduleRepository scheduleRepository,
        IExperienceFeedbackRepository feedbackRepository,
        IExperienceSessionRepository sessionRepository,
        ITenantContext tenantContext)
    {
        _scheduleRepository = scheduleRepository;
        _feedbackRepository = feedbackRepository;
        _sessionRepository = sessionRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> StartExperienceAsync(Guid scheduleId, Guid teacherId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule == null || schedule.TenantId != tenantId)
            throw new KeyNotFoundException("Schedule not found.");

        schedule.Status = "Started";
        await _scheduleRepository.UpdateAsync(schedule);

        var session = new ExperienceSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExperienceId = schedule.ExperienceId,
            ExperienceScheduleId = schedule.Id,
            ClassroomId = schedule.ClassroomId,
            SessionDate = DateTime.UtcNow.Date,
            StartTime = DateTime.UtcNow.TimeOfDay,
            Status = "Active",
            PrimaryTeacherId = teacherId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _sessionRepository.CreateSessionAsync(session, cancellationToken);
        
        return session.Id;
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

    public async Task CompleteExperienceWithParticipationsAsync(Guid scheduleId, Guid teacherId, string rating, string? notes, IEnumerable<ExperienceParticipation> participations, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetSessionByScheduleIdAsync(scheduleId, cancellationToken);
        if (session != null)
        {
            session.Status = "Completed";
            session.EndTime = DateTime.UtcNow.TimeOfDay;
            await _sessionRepository.UpdateSessionAsync(session, cancellationToken);
            await _sessionRepository.SaveParticipationsAsync(session.Id, participations, cancellationToken);
        }

        await CompleteExperienceAsync(scheduleId, teacherId, rating, notes, cancellationToken);
    }

    public async Task<IEnumerable<ExperienceSession>> GetClassroomExperienceHistoryAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _sessionRepository.GetSessionsForClassroomAsync(tenantId, classroomId, cancellationToken);
    }
}

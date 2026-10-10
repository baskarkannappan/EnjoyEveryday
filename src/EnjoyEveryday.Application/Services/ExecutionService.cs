using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Data;
using Dapper;

namespace EnjoyEveryday.Application.Services;

public class ExecutionService
{
    private readonly IExperienceScheduleRepository _scheduleRepository;
    private readonly IExperienceFeedbackRepository _feedbackRepository;
    private readonly IExperienceSessionRepository _sessionRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IUserContext _userContext;

    public ExecutionService(
        IExperienceScheduleRepository scheduleRepository,
        IExperienceFeedbackRepository feedbackRepository,
        IExperienceSessionRepository sessionRepository,
        ITenantContext tenantContext,
        IAuditService auditService,
        IDbConnectionFactory dbConnectionFactory,
        IUserContext userContext)
    {
        _scheduleRepository = scheduleRepository;
        _feedbackRepository = feedbackRepository;
        _sessionRepository = sessionRepository;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _dbConnectionFactory = dbConnectionFactory;
        _userContext = userContext;
    }

    public async Task<Guid> StartExperienceAsync(Guid scheduleId, Guid teacherId, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("experience.execute")) throw new UnauthorizedAccessException("Requires experience.execute permission.");
        var tenantId = _tenantContext.TenantId;
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule == null || schedule.TenantId != tenantId)
            throw new KeyNotFoundException("Schedule not found.");

        if (schedule.Status == "Started" || schedule.Status == "Active" || schedule.Status == "Completed")
            throw new InvalidOperationException($"Cannot start schedule in {schedule.Status} status.");

        // Check if teacher is assigned to this schedule, or is assigned to this classroom
        bool isAuthorized = false;
        if (schedule.PrimaryTeacherId == teacherId) 
        {
            isAuthorized = true;
        }
        else
        {
            using var conn = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            var teacherCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM classroom_teachers WHERE tenant_id = @TenantId AND classroom_id = @ClassroomId AND user_id = @TeacherId", new { TenantId = tenantId, ClassroomId = schedule.ClassroomId, TeacherId = teacherId });
            if (teacherCount > 0) isAuthorized = true;
        }

        if (!isAuthorized)
            throw new UnauthorizedAccessException("Teacher is not authorized to start this experience session in this classroom.");

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
        
        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = teacherId,
            Action = "Session.Started",
            EntityType = "ExperienceSession",
            EntityId = session.Id,
            Details = new { schedule.ExperienceId, schedule.ClassroomId }
        }, cancellationToken);
        
        return session.Id;
    }

    public async Task CompleteExperienceAsync(Guid scheduleId, Guid teacherId, int stars, string rating, string? notes, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("experience.execute")) throw new UnauthorizedAccessException("Requires experience.execute permission.");
        var tenantId = _tenantContext.TenantId;
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule == null || schedule.TenantId != tenantId)
            throw new KeyNotFoundException("Schedule not found.");
            
        if (schedule.Status == "Completed")
            throw new InvalidOperationException("Schedule is already completed.");

        schedule.Status = "Completed";
        await _scheduleRepository.UpdateAsync(schedule);

        var feedback = new ExperienceFeedback
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExperienceScheduleId = scheduleId,
            TeacherId = teacherId,
            Stars = stars,
            Rating = rating,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _feedbackRepository.AddAsync(feedback, cancellationToken);

        var session = await _sessionRepository.GetSessionByScheduleIdAsync(scheduleId, cancellationToken);
        if (session != null)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = teacherId,
                Action = "Session.Completed",
                EntityType = "ExperienceSession",
                EntityId = session.Id,
                Details = new { schedule.ExperienceId, Stars = stars }
            }, cancellationToken);
        }
    }

    public async Task CompleteExperienceWithParticipationsAsync(Guid scheduleId, Guid teacherId, int stars, string rating, string? notes, IEnumerable<ExperienceParticipation> participations, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("experience.execute")) throw new UnauthorizedAccessException("Requires experience.execute permission.");
        var session = await _sessionRepository.GetSessionByScheduleIdAsync(scheduleId, cancellationToken);
        if (session != null)
        {
            session.Status = "Completed";
            session.EndTime = DateTime.UtcNow.TimeOfDay;
            await _sessionRepository.UpdateSessionAsync(session, cancellationToken);
            await _sessionRepository.SaveParticipationsAsync(session.Id, participations, cancellationToken);
        }

        await CompleteExperienceAsync(scheduleId, teacherId, stars, rating, notes, cancellationToken);
    }

    public async Task<IEnumerable<ExperienceSession>> GetClassroomExperienceHistoryAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _sessionRepository.GetSessionsForClassroomAsync(tenantId, classroomId, cancellationToken);
    }
}

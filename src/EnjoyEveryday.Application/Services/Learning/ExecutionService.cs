using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.Shared.Audit;

namespace EnjoyEveryday.Application.Services;

public class ExecutionService
{
    private readonly IExperienceScheduleRepository _scheduleRepository;
    private readonly IExperienceFeedbackRepository _feedbackRepository;
    private readonly IExperienceSessionRepository _sessionRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;
    private readonly IUserContext _userContext;
    private readonly IEndOfDayService? _endOfDayService;

    public ExecutionService(
        IExperienceScheduleRepository scheduleRepository,
        IExperienceFeedbackRepository feedbackRepository,
        IExperienceSessionRepository sessionRepository,
        ITeacherRepository teacherRepository,
        ITenantContext tenantContext,
        IAuditService auditService,
        IUserContext userContext)
        : this(scheduleRepository, feedbackRepository, sessionRepository, teacherRepository, tenantContext, auditService, userContext, null)
    {
    }

    public ExecutionService(
        IExperienceScheduleRepository scheduleRepository,
        IExperienceFeedbackRepository feedbackRepository,
        IExperienceSessionRepository sessionRepository,
        ITeacherRepository teacherRepository,
        ITenantContext tenantContext,
        IAuditService auditService,
        IUserContext userContext,
        IEndOfDayService? endOfDayService)
    {
        _scheduleRepository = scheduleRepository;
        _feedbackRepository = feedbackRepository;
        _sessionRepository = sessionRepository;
        _teacherRepository = teacherRepository;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _userContext = userContext;
        _endOfDayService = endOfDayService;
    }

    public bool IsDaycareClosedForActivity(DateOnly? scheduleDate, out string reason)
    {
        reason = string.Empty;
        var targetDate = scheduleDate ?? DateOnly.FromDateTime(DateTime.Today);
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (_endOfDayService != null && _endOfDayService.IsDaycareClosedForDate(targetDate, branchId: null, includeTimeOfDayCheckForToday: true, out reason))
        {
            return true;
        }

        if (targetDate != today)
        {
            reason = $"Cannot start or end an activity for {targetDate:MMM d, yyyy}. Activities can only be started or ended on an active operating day.";
            return true;
        }

        if (targetDate.DayOfWeek == DayOfWeek.Saturday || targetDate.DayOfWeek == DayOfWeek.Sunday)
        {
            reason = $"The daycare is closed on {targetDate:dddd, MMM d, yyyy} (weekend non-operating day). Cannot start or end any activity.";
            return true;
        }

        var now = DateTime.Now;
        if (now.Hour >= 19 || now.Hour < 7)
        {
            reason = "The daycare is closed for the day (operating hours are 7:00 AM to 7:00 PM). Cannot start or end any activity.";
            return true;
        }

        return false;
    }

    public async Task<Guid> StartExperienceAsync(Guid scheduleId, Guid teacherId, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("experience.execute") && !_userContext.HasPermission("classroom.execute")) 
            throw new UnauthorizedAccessException("Requires experience.execute permission.");
        var tenantId = _tenantContext.TenantId;
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule == null || schedule.TenantId != tenantId)
            throw new KeyNotFoundException("Schedule not found.");

        if (IsDaycareClosedForActivity(schedule.ScheduledDate, out var closedReason))
            throw new InvalidOperationException($"Cannot start activity when daycare is closed: {closedReason}");

        if (schedule.Status == "Started" || schedule.Status == "Active" || schedule.Status == "Completed")
            throw new InvalidOperationException($"Cannot start schedule in {schedule.Status} status.");

        // Check if user is an admin / manager, or teacher assigned to this schedule, or teacher assigned to this classroom, or schedule has no primary teacher assigned
        bool isAuthorized = false;
        if (_userContext.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || 
                                       r.Equals("DaycareAdmin", StringComparison.OrdinalIgnoreCase) || 
                                       r.Equals("OrgAdmin", StringComparison.OrdinalIgnoreCase) || 
                                       r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) || 
                                       r.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase)) ||
            _userContext.HasPermission("organization.admin") ||
            _userContext.HasPermission("experience.manage"))
        {
            isAuthorized = true;
        }
        else if (schedule.PrimaryTeacherId == teacherId || 
                 schedule.PrimaryTeacherId == _userContext.UserId || 
                 !schedule.PrimaryTeacherId.HasValue) 
        {
            isAuthorized = true;
        }
        else
        {
            if (await _teacherRepository.IsTeacherAssignedToClassroomAsync(tenantId, schedule.ClassroomId, teacherId, _userContext.UserId, cancellationToken))
            {
                isAuthorized = true;
            }
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
        if (!_userContext.HasPermission("experience.execute") && !_userContext.HasPermission("classroom.execute")) 
            throw new UnauthorizedAccessException("Requires experience.execute permission.");
        var tenantId = _tenantContext.TenantId;
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule == null || schedule.TenantId != tenantId)
            throw new KeyNotFoundException("Schedule not found.");

        if (IsDaycareClosedForActivity(schedule.ScheduledDate, out var closedReason))
            throw new InvalidOperationException($"Cannot end or complete activity when daycare is closed: {closedReason}");

        if (schedule.Status == "Completed")
        {
            // Idempotent completion: schedule is already marked completed
            var existingSession = await _sessionRepository.GetSessionByScheduleIdAsync(scheduleId, cancellationToken);
            if (existingSession != null && existingSession.Status != "Completed")
            {
                existingSession.Status = "Completed";
                existingSession.EndTime ??= DateTime.UtcNow.TimeOfDay;
                await _sessionRepository.UpdateSessionAsync(existingSession, cancellationToken);
            }
            return;
        }

        schedule.Status = "Completed";
        await _scheduleRepository.UpdateAsync(schedule);

        var feedback = new ExperienceFeedback
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExperienceScheduleId = scheduleId,
            ExperienceId = schedule.ExperienceId,
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
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule != null && IsDaycareClosedForActivity(schedule.ScheduledDate, out var closedReason))
            throw new InvalidOperationException($"Cannot end or complete activity when daycare is closed: {closedReason}");

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

    public async Task<IEnumerable<Guid>> GetLoggedChildIdsForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _sessionRepository.GetLoggedChildIdsForSessionAsync(tenantId, sessionId, cancellationToken);
    }
}

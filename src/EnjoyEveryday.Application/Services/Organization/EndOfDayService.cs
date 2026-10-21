using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class EndOfDayPendingSummary
{
    public int ActiveTeachersCount { get; set; }
    public List<string> ActiveTeachersNames { get; set; } = new();
    public int ActiveChildrenCount { get; set; }
    public List<string> ActiveChildrenNames { get; set; } = new();
    public int PlannedActivitiesCount { get; set; }
    public List<string> PlannedActivitiesTitles { get; set; } = new();
    public int PlannedEventsCount { get; set; }
    public List<string> PlannedEventsTitles { get; set; } = new();

    public int TotalPendingCount => ActiveTeachersCount + ActiveChildrenCount + PlannedActivitiesCount + PlannedEventsCount;
}

public class EndOfDayProcessReport
{
    public DateTime ProcessDate { get; set; }
    public TimeSpan ProcessTime { get; set; }
    public int TeachersCheckedOutCount { get; set; }
    public List<string> TeachersCheckedOutNames { get; set; } = new();
    public int ChildrenCheckedOutCount { get; set; }
    public List<string> ChildrenCheckedOutNames { get; set; } = new();
    public int ActivitiesClosedCount { get; set; }
    public List<string> ActivitiesClosedTitles { get; set; } = new();
    public int EventsClosedCount { get; set; }
    public List<string> EventsClosedTitles { get; set; } = new();
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IEndOfDayService
{
    Task<EndOfDayPendingSummary> GetPendingSummaryAsync(Guid? branchId = null, DateTime? date = null, CancellationToken cancellationToken = default);
    Task<EndOfDayProcessReport> ExecuteEndOfDayProcessAsync(Guid? branchId = null, DateTime? date = null, TimeSpan? checkoutTime = null, CancellationToken cancellationToken = default);
}

public class EndOfDayService : IEndOfDayService
{
    private readonly IEndOfDayRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;

    public EndOfDayService(IEndOfDayRepository repository, ITenantContext tenantContext, IUserContext userContext, IAuditService auditService)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
    }

    public async Task<EndOfDayPendingSummary> GetPendingSummaryAsync(Guid? branchId = null, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var day = (date ?? DateTime.Today).Date;

        var teachers = await _repository.GetPendingTeachersAsync(tenantId, day, branchId, cancellationToken);
        var children = await _repository.GetPendingChildrenAsync(tenantId, day, branchId, cancellationToken);
        var activities = await _repository.GetPendingActivitiesAsync(tenantId, day, branchId, cancellationToken);
        var events = await _repository.GetPendingEventsAsync(tenantId, day, branchId, cancellationToken);

        return new EndOfDayPendingSummary
        {
            ActiveTeachersCount = teachers.Count,
            ActiveTeachersNames = teachers.Select(x => x.Name).Distinct().ToList(),
            ActiveChildrenCount = children.Count,
            ActiveChildrenNames = children.Select(x => x.Name).Distinct().ToList(),
            PlannedActivitiesCount = activities.Count,
            PlannedActivitiesTitles = activities.Select(x => x.Name).Distinct().ToList(),
            PlannedEventsCount = events.Count,
            PlannedEventsTitles = events.Select(x => x.Name).Distinct().ToList()
        };
    }

    public async Task<EndOfDayProcessReport> ExecuteEndOfDayProcessAsync(Guid? branchId = null, DateTime? date = null, TimeSpan? checkoutTime = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var day = (date ?? DateTime.Today).Date;
        var time = checkoutTime ?? DateTime.Now.TimeOfDay;
        var userId = _userContext.UserId;

        var report = new EndOfDayProcessReport { ProcessDate = day, ProcessTime = time, Success = true };

        var teachers = await _repository.GetPendingTeachersAsync(tenantId, day, branchId, cancellationToken);
        if (teachers.Count > 0)
        {
            await _repository.CloseTeachersAsync(teachers.Select(x => x.Id).ToArray(), time, cancellationToken);
            report.TeachersCheckedOutCount = teachers.Count;
            report.TeachersCheckedOutNames = teachers.Select(x => x.Name).Distinct().ToList();
        }

        var children = await _repository.GetPendingChildrenAsync(tenantId, day, branchId, cancellationToken);
        if (children.Count > 0)
        {
            await _repository.CloseChildrenAsync(children.Select(x => x.Id).ToArray(), time, userId, cancellationToken);
            report.ChildrenCheckedOutCount = children.Count;
            report.ChildrenCheckedOutNames = children.Select(x => x.Name).Distinct().ToList();
        }

        var activities = await _repository.GetPendingActivitiesAsync(tenantId, day, branchId, cancellationToken);
        if (activities.Count > 0)
        {
            await _repository.CloseActivitiesAsync(tenantId, day, activities.Select(x => x.Id).ToArray(), time, cancellationToken);
            report.ActivitiesClosedCount = activities.Count;
            report.ActivitiesClosedTitles = activities.Select(x => x.Name).Distinct().ToList();
        }

        var events = await _repository.GetPendingEventsAsync(tenantId, day, branchId, cancellationToken);
        if (events.Count > 0)
        {
            await _repository.CloseEventsAsync(events.Select(x => x.Id).ToArray(), cancellationToken);
            report.EventsClosedCount = events.Count;
            report.EventsClosedTitles = events.Select(x => x.Name).Distinct().ToList();
        }

        report.Message = $"End of day process executed. Checked out {report.TeachersCheckedOutCount} educators, {report.ChildrenCheckedOutCount} children, and auto-closed {report.ActivitiesClosedCount + report.EventsClosedCount} activities & events.";

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = userId,
            Action = "Daycare.EndOfDayProcess",
            EntityType = "DaycareOperations",
            EntityId = branchId ?? tenantId,
            Details = report.Message
        });

        return report;
    }
}

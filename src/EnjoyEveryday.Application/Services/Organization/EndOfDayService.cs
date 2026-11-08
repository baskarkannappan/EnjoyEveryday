using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Configuration;

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
    bool IsDayClosedByEndOfDay(Guid? branchId = null, DateTime? date = null);
    bool IsDaycareClosedForDate(DateTime date, Guid? branchId, bool includeTimeOfDayCheckForToday, out string reason);
    bool IsDaycareClosedForDate(DateOnly date, Guid? branchId, bool includeTimeOfDayCheckForToday, out string reason);
    bool IsEnforcementEnabled();
    bool IsBypassedForTesting();
    void SetRuntimeBypass(bool? bypass);
}

public class EndOfDayService : IEndOfDayService
{
    private readonly IEndOfDayRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly IConfiguration? _configuration;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Guid TenantId, DateOnly Date), bool> _closedDates = new();
    private static bool? _runtimeBypassOverride = null;

    public EndOfDayService(
        IEndOfDayRepository repository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        IConfiguration? configuration = null)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _configuration = configuration;
    }

    public static bool? RuntimeBypassOverride
    {
        get => _runtimeBypassOverride;
        set => _runtimeBypassOverride = value;
    }

    public void SetRuntimeBypass(bool? bypass)
    {
        _runtimeBypassOverride = bypass;
    }

    public bool IsBypassedForTesting()
    {
        return !IsEnforcementEnabled();
    }

    public bool IsEnforcementEnabled()
    {
        if (_runtimeBypassOverride.HasValue)
        {
            return !_runtimeBypassOverride.Value;
        }

        // 1. Check environment variable override
        var envOverride = Environment.GetEnvironmentVariable("BYPASS_DAYCARE_CLOSED")
                       ?? Environment.GetEnvironmentVariable("Bypass_daycare_closed");
        if (!string.IsNullOrWhiteSpace(envOverride) && (envOverride == "1" || envOverride.Equals("true", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (_configuration == null) return true;

        // 2. Check configuration key "Bypass_daycare_closed" or "BypassDaycareClosed"
        var bypassConfig = _configuration["Bypass_daycare_closed"] 
                        ?? _configuration["BypassDaycareClosed"]
                        ?? _configuration["DaycareOperations:Bypass_daycare_closed"];
        if (!string.IsNullOrWhiteSpace(bypassConfig) && (bypassConfig == "1" || bypassConfig.Equals("true", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // 3. Check flat boolean key "EnforceDaycareClosed" (default: true)
        if (bool.TryParse(_configuration["EnforceDaycareClosed"], out var enforceFlat) && !enforceFlat)
        {
            return false;
        }

        // 4. Check configuration section "DaycareClosedEnforcement:Enabled" (default: true)
        if (bool.TryParse(_configuration["DaycareClosedEnforcement:Enabled"], out var enabledSection) && !enabledSection)
        {
            return false;
        }

        return true;
    }

    private bool IsWeekendAllowed()
    {
        if (_runtimeBypassOverride.HasValue && _runtimeBypassOverride.Value) return true;
        if (_configuration == null) return false;
        return string.Equals(_configuration["DaycareClosedEnforcement:AllowWeekends"], "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_configuration["AllowWeekendOperations"], "true", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsAfterHoursAllowed()
    {
        if (_runtimeBypassOverride.HasValue && _runtimeBypassOverride.Value) return true;
        if (_configuration == null) return false;
        return string.Equals(_configuration["DaycareClosedEnforcement:AllowAfterHours"], "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_configuration["AllowAfterHoursOperations"], "true", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsPastDatesAllowed()
    {
        if (_runtimeBypassOverride.HasValue && _runtimeBypassOverride.Value) return true;
        if (_configuration == null) return false;
        return string.Equals(_configuration["DaycareClosedEnforcement:AllowPastDates"], "true", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsDayClosedByEndOfDay(Guid? branchId = null, DateTime? date = null)
    {
        var tenantId = _tenantContext.TenantId;
        var day = DateOnly.FromDateTime((date ?? DateTime.Today).Date);
        return _closedDates.ContainsKey((tenantId, day));
    }

    public bool IsDaycareClosedForDate(DateTime date, Guid? branchId, bool includeTimeOfDayCheckForToday, out string reason)
    {
        return IsDaycareClosedForDate(DateOnly.FromDateTime(date.Date), branchId, includeTimeOfDayCheckForToday, out reason);
    }

    public bool IsDaycareClosedForDate(DateOnly date, Guid? branchId, bool includeTimeOfDayCheckForToday, out string reason)
    {
        reason = string.Empty;

        // If enforcement is disabled via config, env, or runtime test bypass, all features remain accessible.
        if (!IsEnforcementEnabled())
        {
            return false;
        }

        // 1. Non-working days (Weekends: Saturday & Sunday)
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            if (!IsWeekendAllowed())
            {
                reason = $"The daycare is closed on {date:dddd, MMM d, yyyy} (weekend non-operating day).";
                return true;
            }
        }

        // 2. Explicitly closed via End-of-Day process
        var tenantId = _tenantContext?.TenantId ?? Guid.Empty;
        if (tenantId != Guid.Empty && _closedDates.ContainsKey((tenantId, date)))
        {
            reason = $"The daycare is already closed for {date:MMM d, yyyy} (End of Day process has been completed).";
            return true;
        }

        // 3. Past dates are closed for live operations
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (date < today)
        {
            if (!IsPastDatesAllowed())
            {
                reason = $"The daycare day for {date:MMM d, yyyy} has already ended and is closed.";
                return true;
            }
        }

        // 4. Operating hours check if checking today's live state (Closed after 7:00 PM / 19:00 or before 7:00 AM)
        if (includeTimeOfDayCheckForToday && date == today)
        {
            var now = DateTime.Now;
            if (now.Hour >= 19 || now.Hour < 7)
            {
                if (!IsAfterHoursAllowed())
                {
                    reason = "The daycare is closed for the day (operating hours are 7:00 AM to 7:00 PM).";
                    return true;
                }
            }
        }

        return false;
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
        var time = checkoutTime ?? new TimeSpan(19, 0, 0);
        var userId = (_userContext?.UserId != null && _userContext.UserId != Guid.Empty)
            ? _userContext.UserId
            : Guid.Parse("50000000-0000-0000-0000-000000000002");

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

        _closedDates[(tenantId, DateOnly.FromDateTime(day))] = true;

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

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class DailyOperationsService : IDailyOperationsService
{
    private readonly IDailyOperationsRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly IConfiguration? _configuration;
    private readonly ILogger<DailyOperationsService> _logger;

    public DailyOperationsService(
        IDailyOperationsRepository repository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        ILogger<DailyOperationsService> logger,
        IConfiguration? configuration = null)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _logger = logger;
        _configuration = configuration;
    }

    public bool IsBypassedForTesting()
    {
        var envOverride = Environment.GetEnvironmentVariable("BYPASS_DAYCARE_CLOSED")
                       ?? Environment.GetEnvironmentVariable("Bypass_daycare_closed");
        if (!string.IsNullOrWhiteSpace(envOverride) && (envOverride == "1" || envOverride.Equals("true", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (_configuration == null) return false;

        var bypassConfig = _configuration["Bypass_daycare_closed"]
                        ?? _configuration["BypassDaycareClosed"]
                        ?? _configuration["DaycareOperations:Bypass_daycare_closed"];
        return !string.IsNullOrWhiteSpace(bypassConfig) && (bypassConfig == "1" || bypassConfig.Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    private DateOnly ResolveDate(DateOnly? date) => date ?? DateOnly.FromDateTime(DateTime.Today);

    private Guid ResolveUserId()
    {
        return (_userContext?.UserId != null && _userContext.UserId != Guid.Empty)
            ? _userContext.UserId
            : Guid.Parse("50000000-0000-0000-0000-000000000002");
    }

    public async Task<BranchDailyStatusDto?> GetBranchDailyStatusAsync(Guid branchId, DateOnly? date = null, CancellationToken ct = default)
    {
        var targetDate = ResolveDate(date);
        return await _repository.GetBranchDailyStatusAsync(_tenantContext.TenantId, branchId, targetDate, ct);
    }

    public async Task<ClassroomDailyStatusDto?> GetClassroomDailyStatusAsync(Guid classroomId, DateOnly? date = null, CancellationToken ct = default)
    {
        var targetDate = ResolveDate(date);
        return await _repository.GetClassroomDailyStatusAsync(_tenantContext.TenantId, classroomId, targetDate, ct);
    }

    public async Task<DailyOperationExecutionResult> OpenBranchDayAsync(Guid branchId, DateOnly? date = null, CancellationToken ct = default)
    {
        var targetDate = ResolveDate(date);
        var userId = ResolveUserId();

        var result = await _repository.OpenBranchDayAsync(_tenantContext.TenantId, branchId, targetDate, userId, ct);

        if (result.Success)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = _tenantContext.TenantId,
                UserId = userId,
                Action = "Branch.OpenDay",
                EntityType = "BranchDailyOperations",
                EntityId = branchId,
                Details = $"Branch opened for day {targetDate:yyyy-MM-dd} by user {userId}"
            }, ct);
        }

        return result;
    }

    public async Task<DailyOperationExecutionResult> OpenClassroomDayAsync(Guid classroomId, DateOnly? date = null, CancellationToken ct = default)
    {
        var targetDate = ResolveDate(date);
        var userId = ResolveUserId();

        var result = await _repository.OpenClassroomDayAsync(_tenantContext.TenantId, classroomId, targetDate, userId, ct);

        if (result.Success)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = _tenantContext.TenantId,
                UserId = userId,
                Action = "Classroom.OpenDay",
                EntityType = "ClassroomDailyOperations",
                EntityId = classroomId,
                Details = $"Classroom opened for day {targetDate:yyyy-MM-dd} by user {userId}"
            }, ct);
        }

        return result;
    }

    public async Task<DailyOperationExecutionResult> CloseClassroomDayAsync(Guid classroomId, DateOnly? date = null, TimeSpan? checkoutTime = null, CancellationToken ct = default)
    {
        var targetDate = ResolveDate(date);
        var userId = ResolveUserId();
        var time = checkoutTime ?? DateTime.Now.TimeOfDay;

        var result = await _repository.CloseClassroomDayAsync(_tenantContext.TenantId, classroomId, targetDate, userId, time, ct);

        if (result.Success)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = _tenantContext.TenantId,
                UserId = userId,
                Action = "Classroom.CloseDay",
                EntityType = "ClassroomDailyOperations",
                EntityId = classroomId,
                Details = $"Classroom closed for day {targetDate:yyyy-MM-dd}. Auto-checked out {result.AffectedChildrenCount} children, completed {result.AffectedActivitiesCount} activities."
            }, ct);
        }

        return result;
    }

    public async Task<DailyOperationExecutionResult> CloseBranchDayAsync(Guid branchId, DateOnly? date = null, TimeSpan? checkoutTime = null, CancellationToken ct = default)
    {
        var targetDate = ResolveDate(date);
        var userId = ResolveUserId();
        var time = checkoutTime ?? DateTime.Now.TimeOfDay;

        var result = await _repository.CloseBranchDayAsync(_tenantContext.TenantId, branchId, targetDate, userId, time, ct);

        if (result.Success)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = _tenantContext.TenantId,
                UserId = userId,
                Action = "Branch.CloseDay",
                EntityType = "BranchDailyOperations",
                EntityId = branchId,
                Details = $"Branch closed for day {targetDate:yyyy-MM-dd}. Clocked out {result.AffectedTeachersCount} staff members."
            }, ct);
        }

        return result;
    }

    public async Task<bool> IsBranchOpenAsync(Guid branchId, DateOnly? date = null, CancellationToken ct = default)
    {
        if (IsBypassedForTesting()) return true;
        var status = await GetBranchDailyStatusAsync(branchId, date, ct);
        return status?.IsOpen == true;
    }

    public async Task<bool> IsClassroomOpenAsync(Guid classroomId, DateOnly? date = null, CancellationToken ct = default)
    {
        if (IsBypassedForTesting()) return true;
        var status = await GetClassroomDailyStatusAsync(classroomId, date, ct);
        return status != null && status.IsBranchOpen && status.IsOpen;
    }

    public async Task<bool> CanPerformBranchOperationsAsync(Guid branchId, DateOnly? date = null, CancellationToken ct = default)
    {
        return await IsBranchOpenAsync(branchId, date, ct);
    }

    public async Task<bool> CanPerformClassroomOperationsAsync(Guid classroomId, DateOnly? date = null, CancellationToken ct = default)
    {
        return await IsClassroomOpenAsync(classroomId, date, ct);
    }

    public async Task ValidateBranchOperationsAllowedAsync(Guid branchId, DateOnly? date = null, string operationName = "operation", CancellationToken ct = default)
    {
        if (IsBypassedForTesting()) return;

        var targetDate = ResolveDate(date);
        var status = await GetBranchDailyStatusAsync(branchId, targetDate, ct);
        if (status == null || !status.IsOpen)
        {
            var state = status?.Status ?? "Closed";
            throw new InvalidOperationException($"Cannot perform {operationName}: Branch facility is currently {state} for {targetDate:yyyy-MM-dd}. The Branch Admin must open the facility for operations first.");
        }
    }

    public async Task ValidateClassroomOperationsAllowedAsync(Guid classroomId, DateOnly? date = null, string operationName = "operation", CancellationToken ct = default)
    {
        if (IsBypassedForTesting()) return;

        var targetDate = ResolveDate(date);
        var status = await GetClassroomDailyStatusAsync(classroomId, targetDate, ct);
        if (status == null)
        {
            throw new InvalidOperationException($"Cannot perform {operationName}: Classroom daily operational status could not be resolved.");
        }

        if (!status.IsBranchOpen)
        {
            throw new InvalidOperationException($"Cannot perform {operationName}: Parent branch facility is currently closed for {targetDate:yyyy-MM-dd}. The Branch Admin must open the facility for operations first.");
        }

        if (!status.IsOpen)
        {
            var state = status.Status ?? "Closed";
            throw new InvalidOperationException($"Cannot perform {operationName}: Classroom is currently {state} for {targetDate:yyyy-MM-dd}. The classroom must be opened for daily operations first.");
        }
    }
}

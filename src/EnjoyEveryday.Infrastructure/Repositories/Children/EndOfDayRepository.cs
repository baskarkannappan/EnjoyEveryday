using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class EndOfDayRepository : IEndOfDayRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EndOfDayRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<EndOfDayPendingItem>> GetPendingTeachersAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        var items = await c.QueryFunctionAsync<EndOfDayPendingItem>(
            "fn_get_end_of_day_pending_teachers",
            new { TenantId = tenantId, Date = DateOnly.FromDateTime(date), BranchId = branchId },
            cancellationToken: ct);
        return items.ToList();
    }

    public async Task CloseTeachersAsync(Guid[] ids, TimeSpan clockOut, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        await c.ExecuteProcedureAsync(
            "sp_close_end_of_day_teachers",
            new { ClockOut = clockOut, Ids = ids },
            cancellationToken: ct);
    }

    public async Task<List<EndOfDayPendingItem>> GetPendingChildrenAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        var items = await c.QueryFunctionAsync<EndOfDayPendingItem>(
            "fn_get_end_of_day_pending_children",
            new { TenantId = tenantId, Date = DateOnly.FromDateTime(date), BranchId = branchId },
            cancellationToken: ct);
        return items.ToList();
    }

    public async Task CloseChildrenAsync(Guid[] ids, TimeSpan departure, Guid userId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        await c.ExecuteProcedureAsync(
            "sp_close_end_of_day_children",
            new { Departure = departure, UserId = userId, Ids = ids },
            cancellationToken: ct);
    }

    public async Task<List<EndOfDayPendingItem>> GetPendingActivitiesAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        var items = await c.QueryFunctionAsync<EndOfDayPendingItem>(
            "fn_get_end_of_day_pending_activities",
            new { TenantId = tenantId, Date = DateOnly.FromDateTime(date), BranchId = branchId },
            cancellationToken: ct);
        return items.ToList();
    }

    public async Task CloseActivitiesAsync(Guid tenantId, DateTime date, Guid[] scheduleIds, TimeSpan endTime, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        await c.ExecuteProcedureAsync(
            "sp_close_end_of_day_activities",
            new { TenantId = tenantId, ScheduleIds = scheduleIds, EndTime = endTime },
            cancellationToken: ct);
    }

    public async Task<List<EndOfDayPendingItem>> GetPendingEventsAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        var items = await c.QueryFunctionAsync<EndOfDayPendingItem>(
            "fn_get_end_of_day_pending_events",
            new { TenantId = tenantId, Date = DateOnly.FromDateTime(date), BranchId = branchId },
            cancellationToken: ct);
        return items.ToList();
    }

    public async Task CloseEventsAsync(Guid[] ids, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        await c.ExecuteProcedureAsync(
            "sp_close_end_of_day_events",
            new { Ids = ids },
            cancellationToken: ct);
    }
}

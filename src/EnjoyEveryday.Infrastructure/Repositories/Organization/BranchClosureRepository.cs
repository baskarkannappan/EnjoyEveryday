using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class BranchClosureRepository : IBranchClosureRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BranchClosureRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<BranchClosure?> GetActiveClosureAsync(Guid tenantId, Guid branchId, DateTimeOffset atTime, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<BranchClosure>(
            "public.fn_get_active_branch_closure",
            new { TenantId = tenantId, BranchId = branchId, AtTime = atTime.ToUniversalTime() },
            cancellationToken: cancellationToken);
    }

    public async Task<BranchClosure?> GetByIdAsync(Guid tenantId, Guid closureId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<BranchClosure>(
            "public.fn_get_branch_closure_by_id",
            new { TenantId = tenantId, Id = closureId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<BranchClosure>> GetHistoryAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<BranchClosure>(
            "public.fn_get_branch_closure_history",
            new { TenantId = tenantId, OrganizationId = organizationId, BranchId = branchId },
            cancellationToken: cancellationToken);
    }

    public async Task<BranchClosure> CreateClosureAsync(BranchClosure closure, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleFunctionAsync<BranchClosure>(
            "public.fn_create_branch_closure",
            new
            {
                closure.TenantId,
                closure.OrganizationId,
                closure.BranchId,
                closure.Status,
                closure.Reason,
                closure.Description,
                closure.ParentAnnouncement,
                EffectiveStartAt = closure.EffectiveStartAt.ToUniversalTime(),
                EffectiveEndAt = closure.EffectiveEndAt?.ToUniversalTime(),
                closure.CreatedBy,
                closure.CreatedByRole,
                closure.AffectedClassroomCount,
                closure.CancelledSessionCount
            },
            cancellationToken: cancellationToken);
        return created ?? closure;
    }

    public async Task MarkReopenedAsync(Guid tenantId, Guid closureId, Guid reopenedBy, DateTimeOffset reopenedAt, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_reopen_branch_closure",
            new { TenantId = tenantId, ClosureId = closureId, ReopenedBy = reopenedBy, ReopenedAt = reopenedAt.ToUniversalTime() },
            cancellationToken: cancellationToken);
    }
}


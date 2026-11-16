using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IBranchClosureRepository
{
    Task<BranchClosure?> GetActiveClosureAsync(Guid tenantId, Guid branchId, DateTimeOffset atTime, CancellationToken cancellationToken = default);
    Task<BranchClosure?> GetByIdAsync(Guid tenantId, Guid closureId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BranchClosure>> GetHistoryAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, CancellationToken cancellationToken = default);
    Task<BranchClosure> CreateClosureAsync(BranchClosure closure, CancellationToken cancellationToken = default);
    Task MarkReopenedAsync(Guid tenantId, Guid closureId, Guid reopenedBy, DateTimeOffset reopenedAt, CancellationToken cancellationToken = default);
}


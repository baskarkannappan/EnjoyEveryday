using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IDailyOperationsRepository
{
    Task<BranchDailyStatusDto?> GetBranchDailyStatusAsync(Guid tenantId, Guid branchId, DateOnly date, CancellationToken ct = default);
    Task<ClassroomDailyStatusDto?> GetClassroomDailyStatusAsync(Guid tenantId, Guid classroomId, DateOnly date, CancellationToken ct = default);
    Task<DailyOperationExecutionResult> OpenBranchDayAsync(Guid tenantId, Guid branchId, DateOnly date, Guid userId, CancellationToken ct = default);
    Task<DailyOperationExecutionResult> OpenClassroomDayAsync(Guid tenantId, Guid classroomId, DateOnly date, Guid userId, CancellationToken ct = default);
    Task<DailyOperationExecutionResult> CloseClassroomDayAsync(Guid tenantId, Guid classroomId, DateOnly date, Guid userId, TimeSpan checkoutTime, CancellationToken ct = default);
    Task<DailyOperationExecutionResult> CloseBranchDayAsync(Guid tenantId, Guid branchId, DateOnly date, Guid userId, TimeSpan checkoutTime, CancellationToken ct = default);
}


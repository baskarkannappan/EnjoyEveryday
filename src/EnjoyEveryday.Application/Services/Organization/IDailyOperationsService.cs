using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public interface IDailyOperationsService
{
    Task<BranchDailyStatusDto?> GetBranchDailyStatusAsync(Guid branchId, DateOnly? date = null, CancellationToken ct = default);
    Task<ClassroomDailyStatusDto?> GetClassroomDailyStatusAsync(Guid classroomId, DateOnly? date = null, CancellationToken ct = default);
    
    Task<DailyOperationExecutionResult> OpenBranchDayAsync(Guid branchId, DateOnly? date = null, CancellationToken ct = default);
    Task<DailyOperationExecutionResult> OpenClassroomDayAsync(Guid classroomId, DateOnly? date = null, CancellationToken ct = default);
    
    Task<DailyOperationExecutionResult> CloseClassroomDayAsync(Guid classroomId, DateOnly? date = null, TimeSpan? checkoutTime = null, CancellationToken ct = default);
    Task<DailyOperationExecutionResult> CloseBranchDayAsync(Guid branchId, DateOnly? date = null, TimeSpan? checkoutTime = null, CancellationToken ct = default);

    Task<bool> CanPerformClassroomOperationsAsync(Guid classroomId, DateOnly? date = null, CancellationToken ct = default);
    bool IsBypassedForTesting();
}

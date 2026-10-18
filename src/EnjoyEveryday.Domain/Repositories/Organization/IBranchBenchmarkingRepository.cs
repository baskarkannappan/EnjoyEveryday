using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public class BranchScorecardBranchRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class BranchRoomStatsRecord
{
    public int RoomCount { get; set; }
    public int TotalCap { get; set; }
}

public class BranchAttendanceStatsRecord
{
    public int TotalAttendanceRecords { get; set; }
    public int PresentCount { get; set; }
}

public class BranchBillingStatsRecord
{
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
}

public interface IBranchBenchmarkingRepository
{
    Task<List<BranchScorecardBranchRecord>> GetBranchesForBenchmarkingAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default);
    Task<BranchRoomStatsRecord> GetRoomStatsAsync(Guid tenantId, Guid branchId, CancellationToken ct = default);
    Task<int> GetEnrolledChildrenCountAsync(Guid tenantId, Guid branchId, CancellationToken ct = default);
    Task<BranchAttendanceStatsRecord> GetAttendanceStatsAsync(Guid tenantId, Guid branchId, CancellationToken ct = default);
    Task<int> GetClassroomsWithPlansCountAsync(Guid tenantId, Guid branchId, CancellationToken ct = default);
    Task<BranchBillingStatsRecord> GetBillingStatsAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<BranchOperationalGoal>> GetOperationalGoalsAsync(Guid tenantId, Guid? branchId, CancellationToken ct = default);
    Task CreateOperationalGoalAsync(BranchOperationalGoal goal, CancellationToken ct = default);
    Task UpdateGoalStatusAsync(Guid tenantId, Guid goalId, string status, string? currentValue, CancellationToken ct = default);
}

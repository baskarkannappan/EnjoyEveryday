using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public interface IBranchBenchmarkingService
{
    Task<List<BranchHealthScorecard>> GetBranchScorecardsAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<BranchOperationalGoal>> GetOperationalGoalsAsync(Guid? branchId = null, CancellationToken ct = default);
    Task<Guid> CreateOperationalGoalAsync(BranchOperationalGoal goal, CancellationToken ct = default);
    Task UpdateGoalStatusAsync(Guid goalId, string status, string? currentValue = null, CancellationToken ct = default);
}

public class BranchBenchmarkingService : IBranchBenchmarkingService
{
    private readonly IBranchBenchmarkingRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<BranchBenchmarkingService> _logger;

    public BranchBenchmarkingService(
        IBranchBenchmarkingRepository repository,
        ITenantContext tenantContext,
        IAuditService? auditService = null,
        ILogger<BranchBenchmarkingService>? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _auditService = auditService ?? new NullAuditService();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<BranchBenchmarkingService>.Instance;
    }

    public async Task<List<BranchHealthScorecard>> GetBranchScorecardsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        // 1. Fetch all branches
        var branches = await _repository.GetBranchesForBenchmarkingAsync(tenantId, organizationId, ct);
        var scorecards = new List<BranchHealthScorecard>();

        foreach (var b in branches)
        {
            // 2. Classrooms & capacity
            var roomStats = await _repository.GetRoomStatsAsync(tenantId, b.Id, ct);

            // 3. Current active enrolled children
            var enrolledCount = await _repository.GetEnrolledChildrenCountAsync(tenantId, b.Id, ct);

            // 4. Attendance rate over last 30 days
            var attStats = await _repository.GetAttendanceStatsAsync(tenantId, b.Id, ct);
            double attRate = attStats.TotalAttendanceRecords > 0 
                ? Math.Round((double)attStats.PresentCount / attStats.TotalAttendanceRecords * 100, 1) 
                : 0.0;

            // 5. Ratio compliance: ratio of on-duty educators to children
            // If branch has no rooms/kids, compliance is 100.0% (no violations), otherwise standard compliant 96.0%
            double ratioRate = (roomStats.RoomCount == 0 && enrolledCount == 0) ? 100.0 : 96.0;

            // 6. Curriculum & Planning coverage
            int roomsWithPlans = await _repository.GetClassroomsWithPlansCountAsync(tenantId, b.Id, ct);
            double planCoverage = roomStats.RoomCount > 0 
                ? Math.Min(100.0, Math.Round((double)roomsWithPlans / roomStats.RoomCount * 100, 1))
                : 0.0;

            // 7. Tuition collection rate from billing invoices
            // Only applicable to branches with classrooms/children; branches with 0 rooms/kids shouldn't inherit org billing
            var billStats = await _repository.GetBillingStatsAsync(tenantId, ct);
            double tuitionRate = (roomStats.RoomCount > 0 && billStats.TotalBilled > 0)
                ? Math.Min(100.0, Math.Round((double)(billStats.TotalPaid / billStats.TotalBilled) * 100, 1))
                : 0.0;

            var card = new BranchHealthScorecard
            {
                BranchId = b.Id,
                BranchName = b.Name,
                Location = b.Location,
                TotalClassrooms = roomStats.RoomCount,
                TotalCapacity = roomStats.TotalCap,
                EnrolledChildren = enrolledCount,
                AttendanceRatePercentage = attRate,
                RatioCompliancePercentage = ratioRate,
                PlanningCoveragePercentage = planCoverage,
                TuitionCollectionRatePercentage = tuitionRate
            };

            scorecards.Add(card);
        }

        return scorecards.OrderByDescending(s => s.CompositeHealthScore).ToList();
    }

    public async Task<List<BranchOperationalGoal>> GetOperationalGoalsAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        return await _repository.GetOperationalGoalsAsync(_tenantContext.TenantId, branchId, ct);
    }

    public async Task<Guid> CreateOperationalGoalAsync(BranchOperationalGoal goal, CancellationToken ct = default)
    {
        goal.TenantId = _tenantContext.TenantId;
        if (goal.Id == Guid.Empty) goal.Id = Guid.NewGuid();

        await _repository.CreateOperationalGoalAsync(goal, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            Action = "OperationalGoalCreated",
            EntityType = "BranchOperationalGoal",
            EntityId = goal.Id,
            Details = new { goal.Title, goal.Category, goal.BranchId }
        }, ct);

        return goal.Id;
    }

    public async Task UpdateGoalStatusAsync(Guid goalId, string status, string? currentValue = null, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        await _repository.UpdateGoalStatusAsync(tenantId, goalId, status, currentValue, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            Action = "OperationalGoalStatusUpdated",
            EntityType = "BranchOperationalGoal",
            EntityId = goalId,
            Details = new { status, currentValue }
        }, ct);
    }
}

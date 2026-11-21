using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class BranchOperationalStatusTests
{
    private static readonly Guid TenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OrgAId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid OrgBId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid BranchAId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchBId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid BranchCId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid Classroom1Id = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid Classroom2Id = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid Classroom3Id = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private static readonly Guid Classroom4Id = Guid.Parse("40000000-0000-0000-0000-000000000004");
    private static readonly Guid Classroom6Id = Guid.Parse("40000000-0000-0000-0000-000000000006");
    private static readonly Guid AdminUserId = Guid.Parse("50000000-0000-0000-0000-000000000002");

    private static (BranchOperationalStatusService Service, InMemoryBranchRepo Branches, InMemoryClassroomRepo Classrooms, InMemoryClosureRepo Closures) CreateSut()
    {
        var branches = new InMemoryBranchRepo();
        var classrooms = new InMemoryClassroomRepo();
        var closures = new InMemoryClosureRepo(branches);

        branches.AddSeed(new Branch { Id = BranchAId, TenantId = TenantId, OrganizationId = OrgAId, Name = "Branch A", IsActive = true, OperationalStatus = "Open" });
        branches.AddSeed(new Branch { Id = BranchBId, TenantId = TenantId, OrganizationId = OrgAId, Name = "Branch B", IsActive = true, OperationalStatus = "Open" });
        branches.AddSeed(new Branch { Id = BranchCId, TenantId = TenantId, OrganizationId = OrgBId, Name = "Branch C", IsActive = true, OperationalStatus = "Open" });

        classrooms.AddSeed(new Classroom { Id = Classroom1Id, TenantId = TenantId, BranchId = BranchAId, Name = "Classroom 1", IsActive = true, Status = "Active" });
        classrooms.AddSeed(new Classroom { Id = Classroom2Id, TenantId = TenantId, BranchId = BranchAId, Name = "Classroom 2", IsActive = true, Status = "Active" });
        classrooms.AddSeed(new Classroom { Id = Classroom3Id, TenantId = TenantId, BranchId = BranchAId, Name = "Classroom 3", IsActive = true, Status = "Active" });
        classrooms.AddSeed(new Classroom { Id = Classroom4Id, TenantId = TenantId, BranchId = BranchBId, Name = "Classroom 4", IsActive = true, Status = "Active" });
        classrooms.AddSeed(new Classroom { Id = Classroom6Id, TenantId = TenantId, BranchId = BranchCId, Name = "Classroom 6", IsActive = true, Status = "Active" });

        var service = new BranchOperationalStatusService(branches, classrooms, closures);
        return (service, branches, classrooms, closures);
    }

    [Test]
    public async Task Scenario01_CloseBranchWithThreeClassrooms_MakesAllThreeUnavailable()
    {
        var (service, _, _, _) = CreateSut();

        var closure = await service.CloseBranchAsync(new CloseBranchRequest
        {
            TenantId = TenantId,
            OrganizationId = OrgAId,
            BranchId = BranchAId,
            Reason = "Maintenance",
            EffectiveStartAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            InitiatedBy = AdminUserId,
            InitiatedByRole = "OrganizationAdmin"
        });

        Assert.That(closure.AffectedClassroomCount, Is.EqualTo(3));

        var c1 = await service.GetClassroomAvailabilityAsync(TenantId, Classroom1Id);
        var c2 = await service.GetClassroomAvailabilityAsync(TenantId, Classroom2Id);
        var c3 = await service.GetClassroomAvailabilityAsync(TenantId, Classroom3Id);

        Assert.That(c1.IsEffectivelyAvailable, Is.False);
        Assert.That(c1.UnavailableReason, Is.EqualTo("BRANCH_CLOSED"));
        Assert.That(c2.IsEffectivelyAvailable, Is.False);
        Assert.That(c3.IsEffectivelyAvailable, Is.False);
    }

    [Test]
    public async Task Scenario02_03_04_06_07_12_EnsureBranchAndClassroomOperational_ThrowsBranchClosedException()
    {
        var (service, _, _, _) = CreateSut();

        await service.CloseBranchAsync(new CloseBranchRequest
        {
            TenantId = TenantId,
            OrganizationId = OrgAId,
            BranchId = BranchAId,
            Reason = "Emergency",
            EffectiveStartAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            InitiatedBy = AdminUserId,
            InitiatedByRole = "OrganizationAdmin"
        });

        var branchEx = Assert.ThrowsAsync<BranchClosedException>(async () =>
            await service.EnsureBranchOperationalAsync(TenantId, BranchAId, "teacher attendance"));
        Assert.That(branchEx!.ErrorCode, Is.EqualTo("BRANCH_CLOSED"));

        var classroomEx = Assert.ThrowsAsync<BranchClosedException>(async () =>
            await service.EnsureClassroomOperationalAsync(TenantId, Classroom1Id, "child attendance"));
        Assert.That(classroomEx!.ErrorCode, Is.EqualTo("BRANCH_CLOSED"));
    }

    [Test]
    public async Task Scenario09_IndependentClassroomInactiveStatus_PreservedAfterBranchReopens()
    {
        var (service, _, classrooms, _) = CreateSut();
        var c2 = await classrooms.GetByIdAsync(TenantId, Classroom2Id);
        c2!.IsActive = false;
        c2.Status = "Inactive";

        var closure = await service.CloseBranchAsync(new CloseBranchRequest
        {
            TenantId = TenantId,
            OrganizationId = OrgAId,
            BranchId = BranchAId,
            Reason = "PublicHoliday",
            EffectiveStartAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            InitiatedBy = AdminUserId,
            InitiatedByRole = "OrganizationAdmin"
        });

        await service.ReopenBranchAsync(new ReopenBranchRequest
        {
            TenantId = TenantId,
            BranchId = BranchAId,
            ClosureId = closure.Id,
            ReopenedBy = AdminUserId,
            ReopenedAt = DateTimeOffset.UtcNow
        });

        var c1After = await service.GetClassroomAvailabilityAsync(TenantId, Classroom1Id);
        var c2After = await service.GetClassroomAvailabilityAsync(TenantId, Classroom2Id);

        Assert.That(c1After.IsEffectivelyAvailable, Is.True);
        Assert.That(c2After.IsEffectivelyAvailable, Is.False);
        Assert.That(c2After.UnavailableReason, Is.EqualTo("CLASSROOM_INACTIVE"));
    }

    [Test]
    public async Task Scenario10_11_MultiBranchAndMultiOrgIsolation_UnaffectedBranchesRemainOpen()
    {
        var (service, _, _, _) = CreateSut();

        await service.CloseBranchAsync(new CloseBranchRequest
        {
            TenantId = TenantId,
            OrganizationId = OrgAId,
            BranchId = BranchAId,
            Reason = "Weather",
            EffectiveStartAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            InitiatedBy = AdminUserId,
            InitiatedByRole = "OrganizationAdmin"
        });

        var branchBClassroom = await service.GetClassroomAvailabilityAsync(TenantId, Classroom4Id);
        var orgBClassroom = await service.GetClassroomAvailabilityAsync(TenantId, Classroom6Id);

        Assert.That(branchBClassroom.IsEffectivelyAvailable, Is.True);
        Assert.That(orgBClassroom.IsEffectivelyAvailable, Is.True);
    }

    [Test]
    public async Task Scenario13_14_ReopenPreservesClosureHistory()
    {
        var (service, _, _, _) = CreateSut();

        var closure = await service.CloseBranchAsync(new CloseBranchRequest
        {
            TenantId = TenantId,
            OrganizationId = OrgAId,
            BranchId = BranchAId,
            Reason = "Maintenance",
            EffectiveStartAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            InitiatedBy = AdminUserId,
            InitiatedByRole = "OrganizationAdmin"
        });

        await service.ReopenBranchAsync(new ReopenBranchRequest
        {
            TenantId = TenantId,
            BranchId = BranchAId,
            ClosureId = closure.Id,
            ReopenedBy = AdminUserId,
            ReopenedAt = DateTimeOffset.UtcNow
        });

        var history = (await service.GetClosureHistoryAsync(TenantId, OrgAId, BranchAId)).ToList();
        Assert.That(history, Has.Count.EqualTo(1));
        Assert.That(history[0].Status, Is.EqualTo("Reopened"));
        Assert.That(history[0].ReopenedAt, Is.Not.Null);
    }

    private sealed class InMemoryBranchRepo : IBranchRepository
    {
        private readonly List<Branch> _items = new();
        public void AddSeed(Branch b) => _items.Add(b);
        public Task<Branch?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default) => Task.FromResult(_items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == id));
        public Task<IEnumerable<Branch>> GetByOrganizationIdAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default) => Task.FromResult<IEnumerable<Branch>>(_items.Where(x => x.TenantId == tenantId && x.OrganizationId == organizationId));
        public Task<IEnumerable<Branch>> GetAssignedForUserAsync(Guid userId, Guid organizationId, CancellationToken ct = default) => Task.FromResult<IEnumerable<Branch>>(_items.Where(x => x.OrganizationId == organizationId));
        public Task<Branch> AddAsync(Branch branch, CancellationToken ct = default) { _items.Add(branch); return Task.FromResult(branch); }
        public Task UpdateAsync(Branch branch, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryClassroomRepo : IClassroomRepository
    {
        private readonly List<Classroom> _items = new();
        public void AddSeed(Classroom c) => _items.Add(c);
        public Task<Classroom?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default) => Task.FromResult(_items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == id));
        public Task<IEnumerable<Classroom>> GetByBranchIdAsync(Guid tenantId, Guid branchId, CancellationToken ct = default) => Task.FromResult<IEnumerable<Classroom>>(_items.Where(x => x.TenantId == tenantId && x.BranchId == branchId));
        public Task<IEnumerable<Classroom>> GetAllAsync(Guid tenantId, CancellationToken ct = default) => Task.FromResult<IEnumerable<Classroom>>(_items.Where(x => x.TenantId == tenantId));
        public Task<Classroom> AddAsync(Classroom classroom, CancellationToken ct = default) { _items.Add(classroom); return Task.FromResult(classroom); }
        public Task UpdateAsync(Classroom classroom, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryClosureRepo : IBranchClosureRepository
    {
        private readonly List<BranchClosure> _items = new();
        private readonly InMemoryBranchRepo _branches;

        public InMemoryClosureRepo(InMemoryBranchRepo branches) => _branches = branches;

        public Task<BranchClosure?> GetActiveClosureAsync(Guid tenantId, Guid branchId, DateTimeOffset atTime, CancellationToken ct = default)
        {
            var active = _items.FirstOrDefault(x => x.TenantId == tenantId && x.BranchId == branchId && x.ReopenedAt == null && x.EffectiveStartAt <= atTime && (x.EffectiveEndAt == null || x.EffectiveEndAt > atTime));
            return Task.FromResult(active);
        }

        public Task<BranchClosure?> GetByIdAsync(Guid tenantId, Guid closureId, CancellationToken ct = default) => Task.FromResult(_items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == closureId));

        public Task<IEnumerable<BranchClosure>> GetHistoryAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, CancellationToken ct = default)
        {
            var q = _items.Where(x => x.TenantId == tenantId && (organizationId == null || x.OrganizationId == organizationId) && (branchId == null || x.BranchId == branchId));
            return Task.FromResult(q);
        }

        public async Task<BranchClosure> CreateClosureAsync(BranchClosure closure, CancellationToken ct = default)
        {
            closure.Id = Guid.NewGuid();
            closure.CreatedAt = DateTimeOffset.UtcNow;
            closure.UpdatedAt = DateTimeOffset.UtcNow;
            _items.Add(closure);
            var b = await _branches.GetByIdAsync(closure.TenantId, closure.BranchId, ct);
            if (b != null)
            {
                b.OperationalStatus = closure.Status;
                b.ActiveClosureId = closure.Id;
            }
            return closure;
        }

        public async Task MarkReopenedAsync(Guid tenantId, Guid closureId, Guid reopenedBy, DateTimeOffset reopenedAt, CancellationToken ct = default)
        {
            var c = _items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == closureId);
            if (c != null)
            {
                c.Status = "Reopened";
                c.ReopenedBy = reopenedBy;
                c.ReopenedAt = reopenedAt;
                var b = await _branches.GetByIdAsync(tenantId, c.BranchId, ct);
                if (b != null)
                {
                    b.OperationalStatus = "Open";
                    b.ActiveClosureId = null;
                }
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Domain.Repositories;

public class BranchReadinessData
{
    public bool BranchExists { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool BranchActive { get; set; }
    public int LeadTeacherCount { get; set; }
    public List<ClassroomReadinessData> Classrooms { get; set; } = new();
}

public class ClassroomReadinessData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Status { get; set; }
    public int Capacity { get; set; }
    public int CurrentEnrolled { get; set; }
    public int AssignedTeacherCount { get; set; }
}

public interface IClassroomReadinessRepository
{
    Task<BranchReadinessData> GetBranchReadinessDataAsync(Guid tenantId, Guid branchId, CancellationToken ct = default);
}


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Domain.Repositories;

public class BoardChildAttendanceSummaryRecord
{
    public int CheckedIn { get; set; }
    public int CheckedOut { get; set; }
    public int Absent { get; set; }
}

public class BoardStaffSummaryRecord
{
    public int ExpectedStaff { get; set; }
    public int PresentStaff { get; set; }
    public int AbsentStaff { get; set; }
}

public class BoardFinancialSummaryRecord
{
    public decimal Outstanding { get; set; }
    public int OverdueCount { get; set; }
}

public class PendingTeacherHandoverRecord
{
    public Guid Id { get; set; }
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string ShiftType { get; set; } = string.Empty;
    public string OutgoingTeacherName { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public string? SummaryNotes { get; set; }
    public string? OutstandingTasks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BoardClassroomRecord
{
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int EnrolledChildren { get; set; }
    public int PresentChildren { get; set; }
    public int ExpectedChildren { get; set; }
    public int PresentStaff { get; set; }
}

public class BoardRhythmScheduleRecord
{
    public Guid Id { get; set; }
    public bool IsCelebration { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TimeDisplay { get; set; } = string.Empty;
    public string ScopeDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public string? PreparationNotes { get; set; }
}

public class BoardTeacherChildCareRecord
{
    public Guid ChildId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool IsCheckedIn { get; set; }
    public TimeSpan? ArrivalTime { get; set; }
    public int CareEventsCount { get; set; }
    public string LastRecordedCareType { get; set; } = "None";
    public DateTime? LastRecordedTime { get; set; }
}

public interface IDaycareOperationsBoardRepository
{
    Task<Guid?> GetDefaultOrganizationIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<string?> GetOrganizationNameAsync(Guid tenantId, Guid orgId, CancellationToken ct = default);
    Task<List<BoardClassroomRecord>> GetClassroomsAsync(Guid tenantId, Guid orgId, DateTime date, Guid? branchId, Guid? classroomId, CancellationToken ct = default);
    Task<BoardChildAttendanceSummaryRecord> GetChildAttendanceSummaryAsync(Guid tenantId, Guid orgId, DateTime date, Guid? branchId, Guid? classroomId, CancellationToken ct = default);
    Task<BoardStaffSummaryRecord> GetStaffSummaryAsync(Guid tenantId, Guid orgId, DateTime date, CancellationToken ct = default);
    Task<List<BoardRhythmScheduleRecord>> GetExperienceSchedulesAsync(Guid tenantId, Guid orgId, DateTime date, Guid? branchId, Guid? classroomId, CancellationToken ct = default);
    Task<List<BoardRhythmScheduleRecord>> GetEventsAsync(Guid tenantId, DateTime date, CancellationToken ct = default);
    Task<List<BoardTeacherChildCareRecord>> GetTeacherChildrenCareStatusAsync(Guid tenantId, Guid orgId, DateTime date, DateTime startOfDay, DateTime endOfDay, Guid? branchId, Guid? classroomId, CancellationToken ct = default);
    Task<int> GetPendingEnrollmentsCountAsync(Guid tenantId, Guid orgId, CancellationToken ct = default);
    Task<BoardFinancialSummaryRecord> GetFinancialSummaryAsync(Guid tenantId, Guid orgId, CancellationToken ct = default);
    Task<List<PendingTeacherHandoverRecord>> GetPendingTeacherHandoversAsync(Guid tenantId, Guid orgId, Guid? branchId, Guid? classroomId, CancellationToken ct = default);
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public class BranchBriefRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RatioRuleRecord
{
    public string AgeGroup { get; set; } = string.Empty;
    public int MaxRatio { get; set; }
    public string? Description { get; set; }
}

public class RosterClassroomRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RoomType { get; set; }
    public int? Capacity { get; set; }
}

public class ChildAttendanceSummaryRecord
{
    public Guid ClassroomId { get; set; }
    public int PresentCount { get; set; }
}

public class TeacherAttendanceSummaryRecord
{
    public Guid TeacherId { get; set; }
    public string? Status { get; set; }
    public TimeSpan? ClockInTime { get; set; }
    public TimeSpan? ClockOutTime { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid? ClassroomId { get; set; }
}

public class TeacherAssignmentSummaryRecord
{
    public Guid Id { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
}

public class StaffLeaveSummaryRecord
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CoverageStatus { get; set; }
    public Guid? SubstituteTeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? SubstituteTeacherName { get; set; }
}

public interface IStaffRosterRepository
{
    Task<BranchBriefRecord?> GetBranchBriefAsync(Guid branchId, CancellationToken ct = default);
    Task<List<RatioRuleRecord>> GetStatutoryRatioRulesAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<RosterClassroomRecord>> GetBranchClassroomsAsync(Guid branchId, CancellationToken ct = default);
    Task<List<ChildAttendanceSummaryRecord>> GetChildAttendanceCountsAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken ct = default);
    Task<List<TeacherAttendanceSummaryRecord>> GetTeacherAttendanceAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken ct = default);
    Task<List<TeacherAssignmentSummaryRecord>> GetBranchTeachersAsync(Guid tenantId, Guid branchId, CancellationToken ct = default);
    Task<List<StaffLeaveSummaryRecord>> GetActiveStaffLeavesAsync(Guid tenantId, DateTime date, CancellationToken ct = default);
    Task InsertStaffLeaveAsync(StaffLeave leave, CancellationToken ct = default);
    Task AssignSubstituteCoverageAsync(Guid tenantId, Guid leaveId, Guid substituteTeacherId, CancellationToken ct = default);
    Task<Guid?> GetTeacherUserIdAsync(Guid tenantId, Guid teacherId, CancellationToken ct = default);
    Task DispatchTeacherToClassroomAsync(Guid tenantId, Guid classroomId, Guid userId, CancellationToken ct = default);
}

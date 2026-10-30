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

public class ActivityCoverageNeedRecord
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public Guid OrganizerId { get; set; }
    public string? OrganizerName { get; set; }
    public Guid LeaveId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
}

public class CoverCandidateRecord
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public bool IsSameClassroom { get; set; }
    public bool IsOnLeave { get; set; }
    public string? BusyWith { get; set; }
    public int CoversThisWeek { get; set; }
    public bool IsAvailable { get; set; }
    public string? Reason { get; set; }
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
    Task<List<ActivityCoverageNeedRecord>> GetActivityCoverageNeedsAsync(Guid tenantId, Guid branchId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<CoverCandidateRecord>> GetCoverCandidatesAsync(Guid tenantId, Guid branchId, Guid eventId, CancellationToken ct = default);
    Task ReassignEventOrganizerAsync(Guid tenantId, Guid branchId, Guid eventId, Guid newUserId, Guid? reassignedBy, CancellationToken ct = default);
}

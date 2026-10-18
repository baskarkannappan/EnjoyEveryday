using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public enum RatioComplianceStatus
{
    Compliant,
    AtRisk,
    Breached
}

public class ClassroomRatioCompliance
{
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = "Toddler";
    public string StatutoryRule { get; set; } = "1:5";
    public int MaxAllowedRatio { get; set; } = 5;
    public int ChildrenPresent { get; set; }
    public int TeachersPresent { get; set; }
    public double CurrentRatio => TeachersPresent > 0 ? Math.Round((double)ChildrenPresent / TeachersPresent, 1) : ChildrenPresent;
    public RatioComplianceStatus Status { get; set; } = RatioComplianceStatus.Compliant;
    public int MissingTeachersCount { get; set; }
}

public class TeacherShiftItem
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid? ClassroomId { get; set; }
    public string ClassroomName { get; set; } = "Float / Support";
    public string ShiftType { get; set; } = "Core Shift";
    public TimeSpan StartTime { get; set; } = new TimeSpan(8, 30, 0);
    public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0);
    public bool IsClockedIn { get; set; }
    public TimeSpan? ClockInTime { get; set; }
    public TimeSpan? ClockOutTime { get; set; }
    public bool IsOnLeave { get; set; }
    public string? LeaveNote { get; set; }
}

public class StaffLeaveItem
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = "Vacation";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Approved";
    public string CoverageStatus { get; set; } = "Uncovered"; // Covered, Uncovered, AgencyTempNeeded
    public Guid? SubstituteTeacherId { get; set; }
    public string? SubstituteTeacherName { get; set; }
}

public class StaffRosterOverview
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int TotalTeachersScheduled { get; set; }
    public int TotalTeachersClockedIn { get; set; }
    public int TotalLeavesToday { get; set; }
    public int UnderstaffedRoomsCount { get; set; }
    public List<ClassroomRatioCompliance> Ratios { get; set; } = new();
    public List<TeacherShiftItem> Shifts { get; set; } = new();
    public List<StaffLeaveItem> Leaves { get; set; } = new();
    public List<TeacherShiftItem> AvailableFloatTeachers { get; set; } = new();
}

public interface IStaffRosterService
{
    Task<StaffRosterOverview> GetRosterOverviewAsync(Guid branchId, DateTime date, CancellationToken ct = default);
    Task<Guid> SubmitStaffLeaveAsync(StaffLeave leave, CancellationToken ct = default);
    Task AssignSubstituteCoverageAsync(Guid leaveId, Guid substituteTeacherId, CancellationToken ct = default);
    Task DispatchFloatTeacherAsync(Guid teacherId, Guid classroomId, CancellationToken ct = default);
}

public class StaffRosterService : IStaffRosterService
{
    private readonly IStaffRosterRepository _rosterRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext? _userContext;
    private readonly IAuditService _auditService;

    public StaffRosterService(
        IStaffRosterRepository rosterRepository,
        ITenantContext tenantContext,
        IUserContext? userContext = null,
        IAuditService? auditService = null)
    {
        _rosterRepository = rosterRepository ?? throw new ArgumentNullException(nameof(rosterRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext;
        _auditService = auditService ?? new NullAuditService();
    }

    public async Task<StaffRosterOverview> GetRosterOverviewAsync(Guid branchId, DateTime date, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        // 1. Fetch branch
        var branch = await _rosterRepository.GetBranchBriefAsync(branchId, ct);
        string branchName = branch?.Name ?? "Main Branch";

        // 2. Fetch statutory ratio rules
        var rawRules = await _rosterRepository.GetStatutoryRatioRulesAsync(tenantId, ct);
        var ratioDict = rawRules.ToDictionary(r => r.AgeGroup, r => r.MaxRatio, StringComparer.OrdinalIgnoreCase);

        // 3. Fetch classrooms for branch
        var classrooms = await _rosterRepository.GetBranchClassroomsAsync(branchId, ct);

        // 4. Fetch children attendance for today
        var childAttendance = await _rosterRepository.GetChildAttendanceCountsAsync(tenantId, branchId, date, ct);
        var childCounts = childAttendance.ToDictionary(r => r.ClassroomId, r => r.PresentCount);

        // 5. Fetch teacher attendance & clock-in
        var teacherAttRows = await _rosterRepository.GetTeacherAttendanceAsync(tenantId, branchId, date, ct);

        // Group teachers present by classroom
        var teachersByRoom = teacherAttRows
            .Where(r => r.ClassroomId != null)
            .GroupBy(r => r.ClassroomId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // 6. Fetch all teachers for branch
        var allTeachers = await _rosterRepository.GetBranchTeachersAsync(tenantId, branchId, ct);

        // 7. Fetch active staff leaves covering today
        var leavesRows = await _rosterRepository.GetActiveStaffLeavesAsync(tenantId, date, ct);
        var leaves = leavesRows.Select(l => new StaffLeaveItem
        {
            Id = l.Id,
            TeacherId = l.TeacherId,
            TeacherName = l.TeacherName ?? "Teacher",
            LeaveType = l.LeaveType,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            Reason = l.Reason,
            Status = l.Status,
            CoverageStatus = l.CoverageStatus ?? "Uncovered",
            SubstituteTeacherId = l.SubstituteTeacherId,
            SubstituteTeacherName = l.SubstituteTeacherName
        }).ToList();

        var onLeaveTeacherIds = new HashSet<Guid>(leaves.Select(l => l.TeacherId));

        // 8. Build Classroom Ratio Compliance
        var ratios = new List<ClassroomRatioCompliance>();
        int understaffedCount = 0;

        foreach (var c in classrooms)
        {
            Guid cId = c.Id;
            string cName = c.Name;
            string roomType = c.RoomType ?? "Toddler";

            string ageGroup = roomType.Contains("Infant", StringComparison.OrdinalIgnoreCase) ? "Infant" :
                              roomType.Contains("Pre", StringComparison.OrdinalIgnoreCase) ? "Preschool" :
                              roomType.Contains("School", StringComparison.OrdinalIgnoreCase) ? "SchoolAge" : "Toddler";

            int maxRatio = ratioDict.TryGetValue(ageGroup, out var rVal) ? rVal : 5;
            int children = childCounts.TryGetValue(cId, out var chCount) ? chCount : 0;
            int teachers = teachersByRoom.TryGetValue(cId, out var tCount) ? tCount : 0;

            // Default minimum of 1 teacher if children are present
            var status = RatioComplianceStatus.Compliant;
            int missing = 0;

            if (children > 0 && teachers == 0)
            {
                status = RatioComplianceStatus.Breached;
                missing = (int)Math.Ceiling((double)children / maxRatio);
                understaffedCount++;
            }
            else if (teachers > 0)
            {
                int maxAllowedChildren = teachers * maxRatio;
                if (children > maxAllowedChildren)
                {
                    status = RatioComplianceStatus.Breached;
                    missing = (int)Math.Ceiling((double)(children - maxAllowedChildren) / maxRatio);
                    understaffedCount++;
                }
                else if (children == maxAllowedChildren)
                {
                    status = RatioComplianceStatus.AtRisk;
                }
            }

            ratios.Add(new ClassroomRatioCompliance
            {
                ClassroomId = cId,
                ClassroomName = cName,
                AgeGroup = ageGroup,
                StatutoryRule = $"1:{maxRatio} ({ageGroup})",
                MaxAllowedRatio = maxRatio,
                ChildrenPresent = children,
                TeachersPresent = teachers,
                Status = status,
                MissingTeachersCount = missing
            });
        }

        // 9. Build Shift Roster Items
        var shifts = new List<TeacherShiftItem>();
        int shiftIndex = 0;

        foreach (var t in allTeachers)
        {
            Guid tId = t.Id;
            string tName = t.TeacherName;
            Guid? cId = t.ClassroomId;
            string cName = t.ClassroomName ?? "Float / Relief";

            var att = teacherAttRows.FirstOrDefault(a => a.TeacherId == tId);
            bool isClockedIn = att != null;
            TimeSpan? clockIn = att?.ClockInTime;
            TimeSpan? clockOut = att?.ClockOutTime;

            bool isOnLeave = onLeaveTeacherIds.Contains(tId);
            string? leaveNote = isOnLeave ? leaves.FirstOrDefault(l => l.TeacherId == tId)?.LeaveType : null;

            // Assign standard staggered shifts
            string shiftType;
            TimeSpan start, end;
            if (shiftIndex % 3 == 0)
            {
                shiftType = "Early Open (7:00 AM - 3:30 PM)";
                start = new TimeSpan(7, 0, 0);
                end = new TimeSpan(15, 30, 0);
            }
            else if (shiftIndex % 3 == 1)
            {
                shiftType = "Core Shift (8:30 AM - 5:00 PM)";
                start = new TimeSpan(8, 30, 0);
                end = new TimeSpan(17, 0, 0);
            }
            else
            {
                shiftType = "Late Close (9:30 AM - 6:00 PM)";
                start = new TimeSpan(9, 30, 0);
                end = new TimeSpan(18, 0, 0);
            }
            shiftIndex++;

            shifts.Add(new TeacherShiftItem
            {
                TeacherId = tId,
                TeacherName = tName,
                ClassroomId = cId,
                ClassroomName = cName,
                ShiftType = shiftType,
                StartTime = start,
                EndTime = end,
                IsClockedIn = isClockedIn,
                ClockInTime = clockIn,
                ClockOutTime = clockOut,
                IsOnLeave = isOnLeave,
                LeaveNote = leaveNote
            });
        }

        var floatTeachers = shifts.Where(s => s.ClassroomId == null || s.ClassroomName.Contains("Float", StringComparison.OrdinalIgnoreCase)).ToList();

        return new StaffRosterOverview
        {
            BranchId = branchId,
            BranchName = branchName,
            Date = date,
            TotalTeachersScheduled = shifts.Count,
            TotalTeachersClockedIn = teacherAttRows.Count,
            TotalLeavesToday = leaves.Count,
            UnderstaffedRoomsCount = understaffedCount,
            Ratios = ratios,
            Shifts = shifts,
            Leaves = leaves,
            AvailableFloatTeachers = floatTeachers
        };
    }

    public async Task<Guid> SubmitStaffLeaveAsync(StaffLeave leave, CancellationToken ct = default)
    {
        leave.TenantId = _tenantContext.TenantId;
        if (leave.Id == Guid.Empty) leave.Id = Guid.NewGuid();

        await _rosterRepository.InsertStaffLeaveAsync(leave, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext?.UserId,
            Action = "StaffLeaveSubmitted",
            EntityType = "StaffLeave",
            EntityId = leave.Id,
            Details = new { leave.TeacherId, leave.LeaveType, leave.StartDate, leave.EndDate }
        }, ct);

        return leave.Id;
    }

    public async Task AssignSubstituteCoverageAsync(Guid leaveId, Guid substituteTeacherId, CancellationToken ct = default)
    {
        await _rosterRepository.AssignSubstituteCoverageAsync(_tenantContext.TenantId, leaveId, substituteTeacherId, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext?.UserId,
            Action = "StaffLeaveSubstituteAssigned",
            EntityType = "StaffLeave",
            EntityId = leaveId,
            Details = new { SubstituteTeacherId = substituteTeacherId }
        }, ct);
    }

    public async Task DispatchFloatTeacherAsync(Guid teacherId, Guid classroomId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = await _rosterRepository.GetTeacherUserIdAsync(tenantId, teacherId, ct);

        if (!userId.HasValue || userId.Value == Guid.Empty) return;

        await _rosterRepository.DispatchTeacherToClassroomAsync(tenantId, classroomId, userId.Value, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext?.UserId,
            Action = "FloatTeacherDispatched",
            EntityType = "ClassroomTeacher",
            EntityId = classroomId,
            Details = new { TeacherId = teacherId, ClassroomId = classroomId }
        }, ct);
    }
}

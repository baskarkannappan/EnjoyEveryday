using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Data;
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
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext? _userContext;
    private readonly IAuditService _auditService;

    public StaffRosterService(
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext,
        IUserContext? userContext = null,
        IAuditService? auditService = null)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService ?? new NullAuditService();
    }

    private sealed class BranchRow { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; }
    private sealed class RatioRuleRow { public string AgeGroup { get; set; } = string.Empty; public int MaxRatio { get; set; } public string? Description { get; set; } }
    private sealed class ClassroomRow { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; public string? RoomType { get; set; } public int? Capacity { get; set; } }
    private sealed class ChildAttendanceRow { public Guid ClassroomId { get; set; } public int PresentCount { get; set; } }
    private sealed class TeacherAttendanceRow { public Guid TeacherId { get; set; } public string? Status { get; set; } public TimeSpan? ClockInTime { get; set; } public TimeSpan? ClockOutTime { get; set; } public string TeacherName { get; set; } = string.Empty; public Guid? ClassroomId { get; set; } }
    private sealed class TeacherAssignmentRow { public Guid Id { get; set; } public string TeacherName { get; set; } = string.Empty; public Guid? ClassroomId { get; set; } public string? ClassroomName { get; set; } }
    private sealed class StaffLeaveRow { public Guid Id { get; set; } public Guid TeacherId { get; set; } public string LeaveType { get; set; } = string.Empty; public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public string? Reason { get; set; } public string Status { get; set; } = string.Empty; public string? CoverageStatus { get; set; } public Guid? SubstituteTeacherId { get; set; } public string? TeacherName { get; set; } public string? SubstituteTeacherName { get; set; } }

    public async Task<StaffRosterOverview> GetRosterOverviewAsync(Guid branchId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        // 1. Fetch branch
        var branch = await conn.QueryFirstOrDefaultAsync<BranchRow>(
            "SELECT id AS Id, name AS Name FROM branches WHERE id = @BranchId", new { BranchId = branchId });

        string branchName = branch?.Name ?? "Main Branch";

        // 2. Fetch statutory ratio rules
        var rulesSql = @"
            SELECT age_group AS AgeGroup, max_children_per_educator AS MaxRatio, description AS Description
            FROM classroom_statutory_ratios
            WHERE (tenant_id IS NULL OR tenant_id = @TenantId) AND is_active = TRUE";

        var rawRules = (await conn.QueryAsync<RatioRuleRow>(rulesSql, new { TenantId = _tenantContext.TenantId })).ToList();
        var ratioDict = rawRules.ToDictionary(r => r.AgeGroup, r => r.MaxRatio, StringComparer.OrdinalIgnoreCase);

        // 3. Fetch classrooms for branch
        var roomsSql = @"
            SELECT id AS Id, name AS Name, COALESCE(classroom_type, age_group, 'Classroom') AS RoomType, capacity AS Capacity
            FROM classrooms
            WHERE branch_id = @BranchId
            ORDER BY name";

        var classrooms = (await conn.QueryAsync<ClassroomRow>(roomsSql, new { BranchId = branchId })).ToList();

        // 4. Fetch children attendance for today
        var childAttSql = @"
            SELECT classroom_id AS ClassroomId, COUNT(*)::int AS PresentCount
            FROM child_attendance
            WHERE tenant_id = @TenantId AND branch_id = @BranchId AND attendance_date = @Date
              AND (status = 'Present' OR status = 'Late')
              AND departure_time IS NULL
            GROUP BY classroom_id";

        var childCounts = (await conn.QueryAsync<ChildAttendanceRow>(childAttSql, new { TenantId = _tenantContext.TenantId, BranchId = branchId, Date = date.Date }))
            .ToDictionary(r => r.ClassroomId, r => r.PresentCount);

        // 5. Fetch teacher attendance & clock-in
        var teacherAttSql = @"
            SELECT ta.teacher_id AS TeacherId, ta.status AS Status, ta.clock_in_time AS ClockInTime, ta.clock_out_time AS ClockOutTime,
                   t.first_name || ' ' || t.last_name AS TeacherName,
                   ct.classroom_id AS ClassroomId
            FROM teacher_attendance ta
            JOIN teachers t ON ta.teacher_id = t.id
            LEFT JOIN classroom_teachers ct ON t.user_id = ct.user_id
            WHERE ta.tenant_id = @TenantId AND ta.branch_id = @BranchId AND ta.attendance_date = @Date
              AND (ta.status = 'Present' OR ta.status = 'Late')
              AND ta.clock_out_time IS NULL";

        var teacherAttRows = (await conn.QueryAsync<TeacherAttendanceRow>(teacherAttSql, new { TenantId = _tenantContext.TenantId, BranchId = branchId, Date = date.Date })).ToList();

        // Group teachers present by classroom
        var teachersByRoom = teacherAttRows
            .Where(r => r.ClassroomId != null)
            .GroupBy(r => r.ClassroomId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // 6. Fetch all teachers for branch
        var allTeachersSql = @"
            SELECT t.id AS Id, t.first_name || ' ' || t.last_name AS TeacherName,
                   ct.classroom_id AS ClassroomId, cr.name AS ClassroomName
            FROM teachers t
            JOIN user_branch_assignments uba ON t.user_id = uba.user_id
            LEFT JOIN classroom_teachers ct ON t.user_id = ct.user_id
            LEFT JOIN classrooms cr ON ct.classroom_id = cr.id
            WHERE uba.branch_id = @BranchId AND t.tenant_id = @TenantId
            ORDER BY t.first_name";

        var allTeachers = (await conn.QueryAsync<TeacherAssignmentRow>(allTeachersSql, new { TenantId = _tenantContext.TenantId, BranchId = branchId })).ToList();

        // 7. Fetch active staff leaves covering today
        var leavesSql = @"
            SELECT sl.id AS Id, sl.teacher_id AS TeacherId, sl.leave_type AS LeaveType, sl.start_date AS StartDate, sl.end_date AS EndDate, sl.reason AS Reason, sl.status AS Status, sl.coverage_status AS CoverageStatus,
                   sl.substitute_teacher_id AS SubstituteTeacherId,
                   t.first_name || ' ' || t.last_name AS TeacherName,
                   sub.first_name || ' ' || sub.last_name AS SubstituteTeacherName
            FROM staff_leaves sl
            JOIN teachers t ON sl.teacher_id = t.id
            LEFT JOIN teachers sub ON sl.substitute_teacher_id = sub.id
            WHERE sl.tenant_id = @TenantId
              AND sl.start_date <= @Date AND sl.end_date >= @Date
              AND sl.status != 'Cancelled' AND sl.status != 'Rejected'
            ORDER BY sl.start_date DESC";

        var leavesRows = (await conn.QueryAsync<StaffLeaveRow>(leavesSql, new { TenantId = _tenantContext.TenantId, Date = date.Date })).ToList();
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
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        leave.TenantId = _tenantContext.TenantId;
        if (leave.Id == Guid.Empty) leave.Id = Guid.NewGuid();

        var sql = @"
            INSERT INTO staff_leaves (
                id, tenant_id, teacher_id, leave_type, start_date, end_date, reason, status, coverage_status, substitute_teacher_id, reviewed_by_user_id, created_at
            )
            VALUES (
                @Id, @TenantId, @TeacherId, @LeaveType, @StartDate, @EndDate, @Reason, @Status, @CoverageStatus, @SubstituteTeacherId, @ReviewedByUserId, NOW()
            )";

        await conn.ExecuteAsync(sql, leave);

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
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            UPDATE staff_leaves
            SET substitute_teacher_id = @SubstituteTeacherId,
                coverage_status = 'Covered'
            WHERE tenant_id = @TenantId AND id = @LeaveId";

        await conn.ExecuteAsync(sql, new { TenantId = _tenantContext.TenantId, LeaveId = leaveId, SubstituteTeacherId = substituteTeacherId });

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
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var userId = await conn.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT user_id FROM teachers WHERE id = @TeacherId AND tenant_id = @TenantId",
            new { TeacherId = teacherId, TenantId = _tenantContext.TenantId });

        if (!userId.HasValue || userId.Value == Guid.Empty) return;

        var sql = @"
            INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)
            VALUES (gen_random_uuid(), @TenantId, @ClassroomId, @UserId, FALSE)
            ON CONFLICT (classroom_id, user_id) DO NOTHING";

        await conn.ExecuteAsync(sql, new { TenantId = _tenantContext.TenantId, ClassroomId = classroomId, UserId = userId.Value });

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext?.UserId,
            Action = "FloatTeacherDispatched",
            EntityType = "ClassroomTeacher",
            EntityId = classroomId,
            Details = new { TeacherId = teacherId, ClassroomId = classroomId }
        }, ct);
    }
}

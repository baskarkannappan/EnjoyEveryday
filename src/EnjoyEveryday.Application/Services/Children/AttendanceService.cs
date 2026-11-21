using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Constants;
using EnjoyEveryday.Domain.Constants;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Attendance;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IChildRepository? _childRepository;
    private readonly ITeacherRepository? _teacherRepository;
    private readonly IBranchRepository? _branchRepository;
    private readonly IUserContext? _userContext;
    private readonly ITenantPreferencesService? _tenantPreferencesService;
    private readonly IEndOfDayService? _endOfDayService;
    private readonly IDailyOperationsService? _dailyOpsService;
    private readonly IBranchOperationalStatusService? _branchOperationalStatusService;
    private readonly IMonthlyRegisterService _monthlyRegisterService;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IClassroomRepository classroomRepository,
        ITenantContext tenantContext)
        : this(attendanceRepository, classroomRepository, tenantContext, null, null, null, null, null, null, null)
    {
    }

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IClassroomRepository classroomRepository,
        ITenantContext tenantContext,
        IChildRepository? childRepository,
        ITeacherRepository? teacherRepository,
        IBranchRepository? branchRepository,
        IUserContext? userContext)
        : this(attendanceRepository, classroomRepository, tenantContext, childRepository, teacherRepository, branchRepository, userContext, null, null, null)
    {
    }

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IClassroomRepository classroomRepository,
        ITenantContext tenantContext,
        IChildRepository? childRepository,
        ITeacherRepository? teacherRepository,
        IBranchRepository? branchRepository,
        IUserContext? userContext,
        ITenantPreferencesService? tenantPreferencesService,
        IEndOfDayService? endOfDayService = null,
        IDailyOperationsService? dailyOpsService = null,
        IBranchOperationalStatusService? branchOperationalStatusService = null,
        IMonthlyRegisterService? monthlyRegisterService = null)
    {
        _attendanceRepository = attendanceRepository;
        _classroomRepository = classroomRepository;
        _tenantContext = tenantContext;
        _childRepository = childRepository;
        _teacherRepository = teacherRepository;
        _branchRepository = branchRepository;
        _userContext = userContext;
        _tenantPreferencesService = tenantPreferencesService;
        _endOfDayService = endOfDayService;
        _dailyOpsService = dailyOpsService;
        _branchOperationalStatusService = branchOperationalStatusService;
        _monthlyRegisterService = monthlyRegisterService ?? new MonthlyRegisterService(attendanceRepository, tenantContext);
    }

    public bool IsDaycareClosedForAttendance(DateTime date, Guid? branchId, out string reason)
    {
        reason = string.Empty;
        var day = DateOnly.FromDateTime(date.Date);

        if (_endOfDayService != null && _endOfDayService.IsDaycareClosedForDate(day, branchId, includeTimeOfDayCheckForToday: true, out reason))
        {
            return true;
        }

        return false;
    }

    public async Task<IEnumerable<ChildAttendance>> GetChildAttendanceAsync(Guid branchId, DateTime date)
    {
        return await _attendanceRepository.GetChildAttendanceByDateAsync(_tenantContext.TenantId, branchId, date);
    }
    
    public async Task<IEnumerable<TeacherAttendance>> GetTeacherAttendanceAsync(Guid branchId, DateTime date)
    {
        return await _attendanceRepository.GetTeacherAttendanceByDateAsync(_tenantContext.TenantId, branchId, date);
    }
    
    public async Task MarkChildAttendanceAsync(ChildAttendance attendance)
    {
        if (IsDaycareClosedForAttendance(attendance.AttendanceDate, attendance.BranchId, out var closedReason))
        {
            throw new InvalidOperationException($"Cannot record child attendance when daycare is closed: {closedReason}");
        }

        if (_branchOperationalStatusService != null && attendance.BranchId != Guid.Empty)
        {
            await _branchOperationalStatusService.EnsureBranchOperationalAsync(_tenantContext.TenantId, attendance.BranchId, DailyOperationReasons.ChildAttendance, new DateTimeOffset(DateTime.SpecifyKind(attendance.AttendanceDate.Date, DateTimeKind.Utc), TimeSpan.Zero));
        }

        if (_dailyOpsService != null && attendance.BranchId != Guid.Empty)
        {
            await _dailyOpsService.ValidateBranchOperationsAllowedAsync(attendance.BranchId, DateOnly.FromDateTime(attendance.AttendanceDate), DailyOperationReasons.ChildAttendance);
            if (attendance.ClassroomId != Guid.Empty)
            {
                await _dailyOpsService.ValidateClassroomOperationsAllowedAsync(attendance.ClassroomId, DateOnly.FromDateTime(attendance.AttendanceDate), DailyOperationReasons.ChildAttendance);
            }
        }

        attendance.TenantId = _tenantContext.TenantId;
        if (attendance.Id == Guid.Empty) attendance.Id = Guid.NewGuid();
        await _attendanceRepository.RecordChildAttendanceAsync(attendance);
    }

    public async Task MarkTeacherAttendanceAsync(TeacherAttendance attendance)
    {
        if (IsDaycareClosedForAttendance(attendance.AttendanceDate, attendance.BranchId, out var closedReason))
        {
            throw new InvalidOperationException($"Cannot record staff/teacher attendance when daycare is closed: {closedReason}");
        }

        if (_branchOperationalStatusService != null && attendance.BranchId != Guid.Empty)
        {
            await _branchOperationalStatusService.EnsureBranchOperationalAsync(_tenantContext.TenantId, attendance.BranchId, DailyOperationReasons.TeacherAttendance, new DateTimeOffset(DateTime.SpecifyKind(attendance.AttendanceDate.Date, DateTimeKind.Utc), TimeSpan.Zero));
        }

        if (_dailyOpsService != null && attendance.BranchId != Guid.Empty)
        {
            await _dailyOpsService.ValidateBranchOperationsAllowedAsync(attendance.BranchId, DateOnly.FromDateTime(attendance.AttendanceDate), DailyOperationReasons.TeacherAttendance);
        }

        attendance.TenantId = _tenantContext.TenantId;
        if (attendance.Id == Guid.Empty) attendance.Id = Guid.NewGuid();
        await _attendanceRepository.RecordTeacherAttendanceAsync(attendance);
    }
    
    public async Task<AttendanceSummary> GetOrganizationSummaryAsync(DateTime date)
    {
        return await _attendanceRepository.GetOrganizationSummaryAsync(_tenantContext.TenantId, date);
    }

    public async Task<IEnumerable<Guid>> GetPresentChildIdsForClassroomAsync(Guid classroomId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _attendanceRepository.GetPresentChildIdsByClassroomAsync(_tenantContext.TenantId, classroomId, date, cancellationToken);
    }

    public async Task<BranchAttendanceRosterDto> GetBranchAttendanceRosterAsync(Guid branchId, DateTime date, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var branchName = "Downtown";
        if (_branchRepository != null)
        {
            var branch = await _branchRepository.GetByIdAsync(tenantId, branchId, ct);
            if (branch != null) branchName = branch.Name;
        }

        var classrooms = (await _classroomRepository.GetByBranchIdAsync(tenantId, branchId, ct)).ToList();
        var classroomMap = classrooms.ToDictionary(c => c.Id);

        var childAtt = (await _attendanceRepository.GetChildAttendanceByDateAsync(tenantId, branchId, date)).ToList();
        var teacherAtt = (await _attendanceRepository.GetTeacherAttendanceByDateAsync(tenantId, branchId, date)).ToList();

        var allChildren = _childRepository != null 
            ? (await _childRepository.GetAllAsync(tenantId, ct)).Where(c => c.ClassroomId.HasValue && classroomMap.ContainsKey(c.ClassroomId.Value)).ToList() 
            : new List<Child>();

        var teacherProfiles = _teacherRepository != null
            ? (await _teacherRepository.GetAllAsync(tenantId, ct)).ToList()
            : new List<Teacher>();

        var deduplicatedChildAtt = childAtt
            .GroupBy(a => a.ChildId)
            .Select(g => g.OrderByDescending(a => a.Status == AttendanceStatuses.CheckedOut ? 2 : (a.Status == AttendanceStatuses.Present ? 1 : 0))
                          .ThenByDescending(a => a.DepartureTime ?? a.ArrivalTime ?? TimeSpan.Zero)
                          .First())
            .ToList();

        var classRows = classrooms.Select(c => {
            var expectedC = allChildren.Count(ch => ch.ClassroomId == c.Id);
            var presentC = deduplicatedChildAtt.Count(a => a.ClassroomId == c.Id && AttendanceStatuses.IsPresentOrLate(a.Status));
            var pendingC = deduplicatedChildAtt.Count(a => a.ClassroomId == c.Id && a.Status == AttendanceStatuses.NotMarked);

            var expectedT = 2;
            var presentT = teacherAtt.Count(a => AttendanceStatuses.IsPresentOrLate(a.Status));
            if (presentT > expectedT) presentT = expectedT;

            string status;
            if (pendingC > 0)
            {
                status = "Needs marking";
            }
            else if (expectedT > 0 && presentT < expectedT)
            {
                status = "Staffing attention";
            }
            else
            {
                status = "Marked";
            }

            return new ClassroomAttendanceRosterDto {
                Id = c.Id,
                Name = c.Name,
                ChildrenRatio = $"{presentC} / {expectedC}",
                TeachersRatio = $"{presentT} / {expectedT}",
                Pending = pendingC.ToString(),
                Status = status
            };
        }).ToList();

        var personRows = new List<PersonAttendanceRosterDto>();
        foreach (var child in allChildren)
        {
            var cr = classrooms.FirstOrDefault(c => c.Id == child.ClassroomId);
            var att = deduplicatedChildAtt.FirstOrDefault(a => a.ChildId == child.Id);
            personRows.Add(new PersonAttendanceRosterDto {
                Id = child.Id,
                Name = $"{child.FirstName} {child.LastName}",
                Initials = $"{child.FirstName.FirstOrDefault()}{child.LastName.FirstOrDefault()}",
                Room = cr?.Name ?? "Unknown",
                ClassroomId = cr?.Id ?? Guid.Empty,
                Status = att?.Status ?? "Not marked",
                In = att?.ArrivalTime?.ToString(@"hh\:mm") ?? "—",
                Out = att?.DepartureTime?.ToString(@"hh\:mm") ?? "—",
                PickupPersonName = att?.PickupPersonName,
                PickupPersonRelationship = att?.PickupPersonRelationship,
                PickupVerificationMethod = att?.PickupVerificationMethod,
                PickupNotes = att?.PickupNotes,
                Notes = att?.Notes,
                Type = "children",
                AttendanceId = att?.Id
            });
        }

        foreach (var profile in teacherProfiles)
        {
            Guid? assignedBranchId = null;
            Guid? assignedClassroomId = null;

            if (!string.IsNullOrWhiteSpace(profile.DraftData))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(profile.DraftData);
                    if (doc.RootElement.TryGetProperty("AssignedBranch", out var bToken) || doc.RootElement.TryGetProperty("assignedBranch", out bToken))
                    {
                        if (Guid.TryParse(bToken.GetString(), out var parsedBranch)) assignedBranchId = parsedBranch;
                    }
                    if (doc.RootElement.TryGetProperty("AssignedClassroom", out var cToken) || doc.RootElement.TryGetProperty("assignedClassroom", out cToken))
                    {
                        if (Guid.TryParse(cToken.GetString(), out var parsedClassroom)) assignedClassroomId = parsedClassroom;
                    }
                }
                catch { }
            }

            if (!assignedBranchId.HasValue && assignedClassroomId.HasValue && classroomMap.TryGetValue(assignedClassroomId.Value, out var cRoom))
            {
                assignedBranchId = cRoom.BranchId;
            }

            if (!assignedClassroomId.HasValue && _teacherRepository != null)
            {
                foreach (var c in classrooms)
                {
                    if (await _teacherRepository.IsTeacherAssignedToClassroomAsync(tenantId, c.Id, profile.Id, profile.UserId, ct))
                    {
                        assignedClassroomId = c.Id;
                        assignedBranchId = branchId;
                        break;
                    }
                }
            }

            bool belongsToBranch = assignedBranchId.HasValue && assignedBranchId.Value == branchId;
            var att = teacherAtt.FirstOrDefault(a => a.TeacherId == profile.Id || (profile.UserId.HasValue && a.TeacherId == profile.UserId.Value));
            if (!belongsToBranch && att != null)
            {
                belongsToBranch = true;
            }

            if (!belongsToBranch) continue;

            string roomDisplay = "Teacher";
            if (assignedClassroomId.HasValue && classroomMap.TryGetValue(assignedClassroomId.Value, out var mappedRoom))
            {
                roomDisplay = mappedRoom.Name;
            }

            personRows.Add(new PersonAttendanceRosterDto {
                Id = profile.Id,
                Name = profile.FullName,
                Initials = $"{profile.FirstName.FirstOrDefault()}{profile.LastName.FirstOrDefault()}",
                Room = roomDisplay,
                ClassroomId = assignedClassroomId ?? Guid.Empty,
                Status = att?.Status ?? "Not marked",
                In = att?.ClockInTime?.ToString(@"hh\:mm") ?? "—",
                Out = att?.ClockOutTime?.ToString(@"hh\:mm") ?? "—",
                Type = "teachers",
                AttendanceId = att?.Id
            });
        }

        return new BranchAttendanceRosterDto {
            BranchId = branchId,
            BranchName = branchName,
            Date = date,
            Classrooms = classRows,
            People = personRows
        };
    }

    public async Task BulkMarkAttendanceAsync(BulkMarkAttendanceRequest request, Guid recordedByUserId, CancellationToken ct = default)
    {
        if (IsDaycareClosedForAttendance(request.Date, request.BranchId, out var closedReason))
        {
            throw new InvalidOperationException($"Cannot perform attendance when daycare is closed: {closedReason}");
        }

        if (_branchOperationalStatusService != null && request.BranchId != Guid.Empty)
        {
            await _branchOperationalStatusService.EnsureBranchOperationalAsync(_tenantContext.TenantId, request.BranchId, "bulk attendance", new DateTimeOffset(DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc), TimeSpan.Zero), ct);
        }

        if (_dailyOpsService != null && request.BranchId != Guid.Empty)
        {
            await _dailyOpsService.ValidateBranchOperationsAllowedAsync(request.BranchId, DateOnly.FromDateTime(request.Date), "attendance", ct);
        }

        var tenantId = _tenantContext.TenantId;
        foreach (var item in request.Items)
        {
            if (item.Type == "children")
            {
                var att = new ChildAttendance
                {
                    Id = item.AttendanceId ?? Guid.NewGuid(),
                    TenantId = tenantId,
                    BranchId = request.BranchId,
                    ClassroomId = item.ClassroomId,
                    ChildId = item.Id,
                    AttendanceDate = request.Date,
                    Status = item.Status,
                    RecordedByUserId = recordedByUserId
                };
                if (item.Status == AttendanceStatuses.Present && item.In == "—") att.ArrivalTime = DateTime.Now.TimeOfDay;
                await _attendanceRepository.RecordChildAttendanceAsync(att);
            }
            else
            {
                var att = new TeacherAttendance
                {
                    Id = item.AttendanceId ?? Guid.NewGuid(),
                    TenantId = tenantId,
                    BranchId = request.BranchId,
                    TeacherId = item.Id,
                    AttendanceDate = request.Date,
                    Status = item.Status,
                    RecordedByUserId = recordedByUserId
                };
                if (item.Status == AttendanceStatuses.Present && item.In == "—") att.ClockInTime = DateTime.Now.TimeOfDay;
                await _attendanceRepository.RecordTeacherAttendanceAsync(att);
            }
        }
    }

    public async Task<IEnumerable<AttendanceAttentionAlertDto>> GetAttendanceNeedsAttentionAsync(DateTime date, CancellationToken ct = default)
    {
        return await _attendanceRepository.GetAttendanceNeedsAttentionAsync(_tenantContext.TenantId, date, ct);
    }

    public async Task<IEnumerable<ClassroomAttendanceSummaryDto>> GetClassroomAttendanceSummariesAsync(
        DateTime date, 
        IEnumerable<Branch>? branches = null, 
        IEnumerable<Classroom>? classrooms = null, 
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var classroomList = classrooms != null 
            ? classrooms.ToList() 
            : (await _classroomRepository.GetAllAsync(tenantId, ct)).ToList();

        var branchMap = (branches != null ? branches.ToList() : new List<Branch>()).ToDictionary(b => b.Id);
        
        if (_branchRepository != null)
        {
            var missingBranchIds = classroomList.Select(c => c.BranchId).Where(id => !branchMap.ContainsKey(id)).Distinct();
            foreach (var bId in missingBranchIds)
            {
                var b = await _branchRepository.GetByIdAsync(tenantId, bId, ct);
                if (b != null) branchMap[b.Id] = b;
            }
        }

        var allChildren = _childRepository != null ? (await _childRepository.GetAllAsync(tenantId, ct)).ToList() : new List<Child>();
        var result = new List<ClassroomAttendanceSummaryDto>();

        var distinctBranchIds = classroomList.Select(c => c.BranchId).Distinct();
        foreach (var branchId in distinctBranchIds)
        {
            branchMap.TryGetValue(branchId, out var branch);
            var branchName = branch?.Name ?? "Main Campus";

            var childAtt = (await _attendanceRepository.GetChildAttendanceByDateAsync(tenantId, branchId, date, ct)).ToList();
            var teacherAtt = (await _attendanceRepository.GetTeacherAttendanceByDateAsync(tenantId, branchId, date, ct)).ToList();

            var branchClassrooms = classroomList.Where(c => c.BranchId == branchId);
            foreach (var room in branchClassrooms)
            {
                var roomChildrenCount = allChildren.Count(c => c.ClassroomId == room.Id);
                var expectedC = Math.Max(roomChildrenCount, childAtt.Count(x => x.ClassroomId == room.Id));
                var presentC = childAtt.Count(x => x.ClassroomId == room.Id && AttendanceStatuses.IsPresentOrLate(x.Status));
                var markedC = childAtt.Count(x => x.ClassroomId == room.Id && x.Status != AttendanceStatuses.NotMarked);
                var unmarkedC = Math.Max(0, expectedC - markedC);

                var expectedT = 2;
                var presentT = Math.Min(expectedT, teacherAtt.Count(x => AttendanceStatuses.IsPresentOrLate(x.Status)));

                string statusText = "Marked";
                string statusCss = "green";
                string statusCategory = "all";

                if (unmarkedC > 0)
                {
                    statusText = "Needs marking";
                    statusCss = "amber";
                    statusCategory = "pending";
                }
                else if (expectedT > 0 && presentT < expectedT)
                {
                    statusText = "Staffing attention";
                    statusCss = "red";
                    statusCategory = "attention";
                }

                result.Add(new ClassroomAttendanceSummaryDto
                {
                    ClassroomId = room.Id,
                    ClassroomName = room.Name,
                    AgeGroup = room.AgeGroup ?? string.Empty,
                    BranchId = branchId,
                    BranchName = branchName,
                    ExpectedChildren = expectedC,
                    PresentChildren = presentC,
                    ExpectedTeachers = expectedT,
                    PresentTeachers = presentT,
                    UnmarkedChildren = unmarkedC,
                    StatusText = statusText,
                    StatusCss = statusCss,
                    StatusCategory = statusCategory
                });
            }
        }

        return result;
    }

    public async Task<BranchLocationContextDto?> GetBranchLocationContextAsync(Guid branchId, CancellationToken ct = default)
    {
        return await _attendanceRepository.GetBranchLocationContextAsync(_tenantContext.TenantId, branchId, ct);
    }

    public async Task<IEnumerable<BranchLocationContextDto>> GetUserAccessibleBranchesAsync(bool isOrgAdmin, CancellationToken ct = default)
    {
        var userId = _userContext?.UserId ?? Guid.Empty;
        return await _attendanceRepository.GetUserAccessibleBranchesAsync(_tenantContext.TenantId, userId, isOrgAdmin, ct);
    }

    public Task<(List<ChildMonthlyRegisterRow> Rows, MonthlyRegisterSummary Summary)> GetMonthlyChildRegisterAsync(
        Guid branchId, int year, int month, Guid? classroomId = null, CancellationToken ct = default)
        => _monthlyRegisterService.GetMonthlyChildRegisterAsync(branchId, year, month, classroomId, ct);

    public Task<(List<TeacherMonthlyRegisterRow> Rows, MonthlyRegisterSummary Summary)> GetMonthlyTeacherRegisterAsync(
        Guid branchId, int year, int month, CancellationToken ct = default)
        => _monthlyRegisterService.GetMonthlyTeacherRegisterAsync(branchId, year, month, ct);
}
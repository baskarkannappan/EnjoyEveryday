using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Attendance;
using EnjoyEveryday.Domain.Repositories;

namespace EnjoyEveryday.Application.Services;

public interface IAttendanceService
{
    bool IsDaycareClosedForAttendance(DateTime date, Guid? branchId, out string reason);
    Task<IEnumerable<ChildAttendance>> GetChildAttendanceAsync(Guid branchId, DateTime date);
    Task<IEnumerable<TeacherAttendance>> GetTeacherAttendanceAsync(Guid branchId, DateTime date);
    Task MarkChildAttendanceAsync(ChildAttendance attendance);
    Task MarkTeacherAttendanceAsync(TeacherAttendance attendance);
    Task<AttendanceSummary> GetOrganizationSummaryAsync(DateTime date);
    Task<IEnumerable<Guid>> GetPresentChildIdsForClassroomAsync(Guid classroomId, DateTime date, CancellationToken cancellationToken = default);
    Task<BranchAttendanceRosterDto> GetBranchAttendanceRosterAsync(Guid branchId, DateTime date, CancellationToken ct = default);
    Task BulkMarkAttendanceAsync(BulkMarkAttendanceRequest request, Guid recordedByUserId, CancellationToken ct = default);
    Task<IEnumerable<AttendanceAttentionAlertDto>> GetAttendanceNeedsAttentionAsync(DateTime date, CancellationToken ct = default);
    Task<IEnumerable<ClassroomAttendanceSummaryDto>> GetClassroomAttendanceSummariesAsync(DateTime date, IEnumerable<Branch>? branches = null, IEnumerable<Classroom>? classrooms = null, CancellationToken ct = default);
    Task<BranchLocationContextDto?> GetBranchLocationContextAsync(Guid branchId, CancellationToken ct = default);
    Task<IEnumerable<BranchLocationContextDto>> GetUserAccessibleBranchesAsync(bool isOrgAdmin, CancellationToken ct = default);
    Task<(List<ChildMonthlyRegisterRow> Rows, MonthlyRegisterSummary Summary)> GetMonthlyChildRegisterAsync(Guid branchId, int year, int month, Guid? classroomId = null, CancellationToken ct = default);
    Task<(List<TeacherMonthlyRegisterRow> Rows, MonthlyRegisterSummary Summary)> GetMonthlyTeacherRegisterAsync(Guid branchId, int year, int month, CancellationToken ct = default);
}

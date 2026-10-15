using EnjoyEveryday.Domain.Entities;
namespace EnjoyEveryday.Domain.Repositories;

public class AttendanceSummary
{
    public int ExpectedChildren { get; set; }
    public int PresentChildren { get; set; }
    public int AbsentChildren { get; set; }
    public int LateChildren { get; set; }
    public int UnmarkedChildren => ExpectedChildren - PresentChildren - AbsentChildren - LateChildren;
    
    public int ExpectedTeachers { get; set; }
    public int PresentTeachers { get; set; }
    public int AbsentTeachers { get; set; }
    public int LateTeachers { get; set; }
    public int UnmarkedTeachers => ExpectedTeachers - PresentTeachers - AbsentTeachers - LateTeachers;
}

public interface IAttendanceRepository
{
    Task<IEnumerable<ChildAttendance>> GetChildAttendanceByDateAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeacherAttendance>> GetTeacherAttendanceByDateAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken cancellationToken = default);
    Task<ChildAttendance> RecordChildAttendanceAsync(ChildAttendance attendance, CancellationToken cancellationToken = default);
    Task<TeacherAttendance> RecordTeacherAttendanceAsync(TeacherAttendance attendance, CancellationToken cancellationToken = default);
    Task<AttendanceSummary> GetOrganizationSummaryAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default);
}
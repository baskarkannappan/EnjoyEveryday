import os

base_dir = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay'

def update_file(path, new_content):
    full_path = os.path.join(base_dir, path)
    with open(full_path, 'w', encoding='utf-8') as f:
        f.write(new_content.strip())

# 1. Update IAttendanceRepository.cs
update_file(r'src\EnjoyEveryday.Domain\Repositories\IAttendanceRepository.cs', '''
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
''')

# 2. Update AttendanceRepository.cs
update_file(r'src\EnjoyEveryday.Infrastructure\Repositories\AttendanceRepository.cs', '''
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    public AttendanceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    public async Task<IEnumerable<ChildAttendance>> GetChildAttendanceByDateAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = "SELECT * FROM child_attendance WHERE tenant_id = @TenantId AND branch_id = @BranchId AND attendance_date = @Date";
        return await connection.QueryAsync<ChildAttendance>(sql, new { TenantId = tenantId, BranchId = branchId, Date = date });
    }
    public async Task<IEnumerable<TeacherAttendance>> GetTeacherAttendanceByDateAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = "SELECT * FROM teacher_attendance WHERE tenant_id = @TenantId AND branch_id = @BranchId AND attendance_date = @Date";
        return await connection.QueryAsync<TeacherAttendance>(sql, new { TenantId = tenantId, BranchId = branchId, Date = date });
    }
    public async Task<ChildAttendance> RecordChildAttendanceAsync(ChildAttendance attendance, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            INSERT INTO child_attendance (id, tenant_id, branch_id, classroom_id, child_id, attendance_date, status, arrival_time, departure_time, notes, recorded_by_user_id, created_at, updated_at)
            VALUES (@Id, @TenantId, @BranchId, @ClassroomId, @ChildId, @AttendanceDate, @Status, @ArrivalTime, @DepartureTime, @Notes, @RecordedByUserId, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE SET 
                status = EXCLUDED.status, arrival_time = EXCLUDED.arrival_time, departure_time = EXCLUDED.departure_time, notes = EXCLUDED.notes, updated_at = CURRENT_TIMESTAMP;";
        await connection.ExecuteAsync(sql, attendance);
        return attendance;
    }
    public async Task<TeacherAttendance> RecordTeacherAttendanceAsync(TeacherAttendance attendance, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            INSERT INTO teacher_attendance (id, tenant_id, branch_id, teacher_id, attendance_date, status, clock_in_time, clock_out_time, notes, recorded_by_user_id, created_at, updated_at)
            VALUES (@Id, @TenantId, @BranchId, @TeacherId, @AttendanceDate, @Status, @ClockInTime, @ClockOutTime, @Notes, @RecordedByUserId, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE SET 
                status = EXCLUDED.status, clock_in_time = EXCLUDED.clock_in_time, clock_out_time = EXCLUDED.clock_out_time, notes = EXCLUDED.notes, updated_at = CURRENT_TIMESTAMP;";
        await connection.ExecuteAsync(sql, attendance);
        return attendance;
    }
    
    public async Task<AttendanceSummary> GetOrganizationSummaryAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            SELECT 
                (SELECT COUNT(*) FROM children WHERE tenant_id = @TenantId AND status = 'Active') AS ExpectedChildren,
                (SELECT COUNT(*) FROM child_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Present') AS PresentChildren,
                (SELECT COUNT(*) FROM child_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Absent') AS AbsentChildren,
                (SELECT COUNT(*) FROM child_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Late') AS LateChildren,
                
                (SELECT COUNT(*) FROM teachers WHERE tenant_id = @TenantId) AS ExpectedTeachers,
                (SELECT COUNT(*) FROM teacher_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Present') AS PresentTeachers,
                (SELECT COUNT(*) FROM teacher_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Absent') AS AbsentTeachers,
                (SELECT COUNT(*) FROM teacher_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Late') AS LateTeachers
        ";
        return await connection.QuerySingleAsync<AttendanceSummary>(sql, new { TenantId = tenantId, Date = date });
    }
}
''')

# 3. Update AttendanceService.cs
update_file(r'src\EnjoyEveryday.Application\Services\AttendanceService.cs', '''
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class AttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly ITenantContext _tenantContext;

    public AttendanceService(IAttendanceRepository attendanceRepository, IClassroomRepository classroomRepository, ITenantContext tenantContext)
    {
        _attendanceRepository = attendanceRepository;
        _classroomRepository = classroomRepository;
        _tenantContext = tenantContext;
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
        attendance.TenantId = _tenantContext.TenantId;
        if (attendance.Id == Guid.Empty) attendance.Id = Guid.NewGuid();
        await _attendanceRepository.RecordChildAttendanceAsync(attendance);
    }

    public async Task MarkTeacherAttendanceAsync(TeacherAttendance attendance)
    {
        attendance.TenantId = _tenantContext.TenantId;
        if (attendance.Id == Guid.Empty) attendance.Id = Guid.NewGuid();
        await _attendanceRepository.RecordTeacherAttendanceAsync(attendance);
    }
    
    public async Task<AttendanceSummary> GetOrganizationSummaryAsync(DateTime date)
    {
        return await _attendanceRepository.GetOrganizationSummaryAsync(_tenantContext.TenantId, date);
    }
}
''')

# 4. Update AttendanceOverview.razor
update_file(r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\AttendanceOverview.razor', '''
@page "/management/attendance"
@using EnjoyEveryday.Application.Services
@using EnjoyEveryday.Domain.Repositories
@inject OrganizationService OrganizationService
@inject BranchService BranchService
@inject AttendanceService AttendanceService
@inject NavigationManager Nav
@rendermode InteractiveServer

<div class="content" style="padding: 34px; max-width: 1200px; margin: auto;">
    <h1>Organization Attendance</h1>
    <p>Real-time attendance summary for @SelectedDate.ToString("D")</p>

    @if (Summary != null)
    {
        <div style="display: flex; gap: 20px; margin-bottom: 40px; margin-top: 20px;">
            <div style="flex: 1; padding: 24px; border-radius: 12px; background: white; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
                <h3 style="margin-top: 0;">Children</h3>
                <div style="font-size: 2.5rem; font-weight: bold; color: #365844;">@Summary.PresentChildren <span style="font-size: 1rem; color: #666; font-weight: normal;">/ @Summary.ExpectedChildren present</span></div>
                <ul style="list-style: none; padding: 0; margin-top: 20px; display: flex; flex-direction: column; gap: 10px; color: #555;">
                    <li><strong>@Summary.LateChildren</strong> late</li>
                    <li><strong>@Summary.AbsentChildren</strong> absent</li>
                    <li><strong>@Summary.UnmarkedChildren</strong> not marked yet</li>
                </ul>
            </div>
            
            <div style="flex: 1; padding: 24px; border-radius: 12px; background: white; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
                <h3 style="margin-top: 0;">Teachers</h3>
                <div style="font-size: 2.5rem; font-weight: bold; color: #365844;">@Summary.PresentTeachers <span style="font-size: 1rem; color: #666; font-weight: normal;">/ @Summary.ExpectedTeachers present</span></div>
                <ul style="list-style: none; padding: 0; margin-top: 20px; display: flex; flex-direction: column; gap: 10px; color: #555;">
                    <li><strong>@Summary.LateTeachers</strong> late</li>
                    <li><strong>@Summary.AbsentTeachers</strong> absent</li>
                    <li><strong>@Summary.UnmarkedTeachers</strong> not marked yet</li>
                </ul>
            </div>
        </div>
    }

    <h2>Branches</h2>
    @if (Branches != null)
    {
        <div style="display:flex; gap: 20px; flex-wrap: wrap; margin-top:20px;">
            @foreach (var b in Branches)
            {
                <div style="padding: 24px; border-radius: 12px; cursor: pointer; width: 320px; background: white; box-shadow: 0 4px 12px rgba(0,0,0,0.05); transition: transform 0.2s;" @onclick="() => GoToBranch(b.Id)">
                    <h3 style="margin-top: 0;">@b.Name</h3>
                    <p style="color: #666; margin-bottom: 0;">@b.Location</p>
                    <div style="margin-top: 20px; color: #365844; font-weight: 600;">View Classrooms &rarr;</div>
                </div>
            }
        </div>
    }
</div>

@code {
    private IEnumerable<EnjoyEveryday.Domain.Entities.Branch> Branches = new List<EnjoyEveryday.Domain.Entities.Branch>();
    private AttendanceSummary? Summary;
    private DateTime SelectedDate = DateTime.Today;

    protected override async Task OnInitializedAsync()
    {
        Summary = await AttendanceService.GetOrganizationSummaryAsync(SelectedDate);
        
        var orgs = await OrganizationService.GetOrganizationsAsync();
        var org = orgs.FirstOrDefault();
        if (org != null)
        {
            Branches = await BranchService.GetBranchesAsync(org.Id);
        }
    }

    private void GoToBranch(Guid id)
    {
        Nav.NavigateTo($"/management/attendance/branches/{id}");
    }
}
''')

# 5. Update ClassroomAttendance.razor to include Teacher Attendance
update_file(r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\ClassroomAttendance.razor', '''
@page "/management/attendance/classrooms/{ClassroomId:guid}"
@using EnjoyEveryday.Application.Services
@using EnjoyEveryday.Domain.Entities
@inject AttendanceService AttendanceService
@inject EnjoyEveryday.Shared.Data.IDbConnectionFactory Db
@using Dapper
@rendermode InteractiveServer

<div class="content" style="padding: 34px; max-width: 900px; margin: auto;">
    <h1 style="margin-bottom: 8px;">Classroom Attendance</h1>
    <p style="color: #666; margin-bottom: 32px;">Date: @DateTime.Today.ToString("D")</p>

    <!-- TEACHERS SECTION -->
    <div style="background: white; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.05); padding: 24px; margin-bottom: 32px;">
        <h2 style="margin-top: 0; margin-bottom: 24px; color: #365844;">Teachers</h2>
        @if (Teachers != null && Teachers.Any())
        {
            <div style="display: flex; flex-direction: column; gap: 12px;">
                @foreach (var teacher in Teachers)
                {
                    var record = TeacherAttendanceRecords.FirstOrDefault(a => a.TeacherId == teacher.Id);
                    var status = record?.Status ?? "NotMarked";

                    <div style="display: flex; justify-content: space-between; align-items: center; padding: 16px; border: 1px solid #eee; border-radius: 8px; background: #fafafa;">
                        <div>
                            <strong style="font-size: 1.1em; display: block;">@teacher.FirstName @teacher.LastName</strong>
                            @if (record?.ClockInTime != null)
                            {
                                <small style="color: #666;">Clock In: @record.ClockInTime.Value.ToString(@"hh\:mm")</small>
                            }
                        </div>
                        
                        <div style="display: flex; gap: 8px;">
                            <button style="padding: 8px 16px; border-radius: 6px; border: 1px solid #ccc; background: @(status == "Present" ? "#d4edda" : "white"); font-weight: @(status == "Present" ? "bold" : "normal"); cursor: pointer;"
                                    @onclick="() => MarkTeacherStatus(teacher.Id, "Present")">Present</button>
                            <button style="padding: 8px 16px; border-radius: 6px; border: 1px solid #ccc; background: @(status == "Absent" ? "#f8d7da" : "white"); font-weight: @(status == "Absent" ? "bold" : "normal"); cursor: pointer;"
                                    @onclick="() => MarkTeacherStatus(teacher.Id, "Absent")">Absent</button>
                            <button style="padding: 8px 16px; border-radius: 6px; border: 1px solid #ccc; background: @(status == "Late" ? "#fff3cd" : "white"); font-weight: @(status == "Late" ? "bold" : "normal"); cursor: pointer;"
                                    @onclick="() => MarkTeacherStatus(teacher.Id, "Late")">Late</button>
                        </div>
                    </div>
                }
            </div>
        }
        else
        {
            <p style="color: #666;">No teachers assigned to this classroom today.</p>
        }
    </div>


    <!-- CHILDREN SECTION -->
    <div style="background: white; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.05); padding: 24px;">
        <h2 style="margin-top: 0; margin-bottom: 24px; color: #365844;">Children</h2>
        @if (Children != null && Children.Any())
        {
            <div style="display: flex; flex-direction: column; gap: 12px;">
                @foreach (var child in Children)
                {
                    var record = ChildAttendanceRecords.FirstOrDefault(a => a.ChildId == child.Id);
                    var status = record?.Status ?? "NotMarked";

                    <div style="display: flex; justify-content: space-between; align-items: center; padding: 16px; border: 1px solid #eee; border-radius: 8px; background: #fafafa;">
                        <div>
                            <strong style="font-size: 1.1em; display: block;">@child.FirstName @child.LastName</strong>
                            @if (record?.ArrivalTime != null)
                            {
                                <small style="color: #666;">Arrival: @record.ArrivalTime.Value.ToString(@"hh\:mm")</small>
                            }
                        </div>
                        
                        <div style="display: flex; gap: 8px;">
                            <button style="padding: 8px 16px; border-radius: 6px; border: 1px solid #ccc; background: @(status == "Present" ? "#d4edda" : "white"); font-weight: @(status == "Present" ? "bold" : "normal"); cursor: pointer;"
                                    @onclick="() => MarkChildStatus(child.Id, "Present")">Present</button>
                            <button style="padding: 8px 16px; border-radius: 6px; border: 1px solid #ccc; background: @(status == "Absent" ? "#f8d7da" : "white"); font-weight: @(status == "Absent" ? "bold" : "normal"); cursor: pointer;"
                                    @onclick="() => MarkChildStatus(child.Id, "Absent")">Absent</button>
                            <button style="padding: 8px 16px; border-radius: 6px; border: 1px solid #ccc; background: @(status == "Late" ? "#fff3cd" : "white"); font-weight: @(status == "Late" ? "bold" : "normal"); cursor: pointer;"
                                    @onclick="() => MarkChildStatus(child.Id, "Late")">Late</button>
                        </div>
                    </div>
                }
            </div>
        }
        else
        {
            <p style="color: #666;">No children enrolled in this classroom.</p>
        }
    </div>
</div>

@code {
    [Parameter] public Guid ClassroomId { get; set; }
    private IEnumerable<dynamic> Children = new List<dynamic>();
    private IEnumerable<dynamic> Teachers = new List<dynamic>();
    
    private List<ChildAttendance> ChildAttendanceRecords = new();
    private List<TeacherAttendance> TeacherAttendanceRecords = new();
    private Guid BranchId;

    protected override async Task OnInitializedAsync()
    {
        using var conn = await Db.CreateConnectionAsync();
        var classroom = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT branch_id as BranchId FROM classrooms WHERE id = @Id", new { Id = ClassroomId });
        if (classroom != null) {
            BranchId = classroom.branchid;
            Children = await conn.QueryAsync<dynamic>("SELECT id as Id, first_name as FirstName, last_name as LastName FROM children WHERE classroom_id = @Id AND status = 'Active'", new { Id = ClassroomId });
            
            // Assuming users assigned to the classroom are teachers.
            // Adjust based on the actual schema mapping if needed. For now we use the general users table.
            Teachers = await conn.QueryAsync<dynamic>(@"
                SELECT u.id as Id, u.first_name as FirstName, u.last_name as LastName 
                FROM users u
                WHERE u.tenant_id = (SELECT tenant_id FROM classrooms WHERE id = @Id)
                LIMIT 3", // Mocking 3 teachers per classroom for testing
                new { Id = ClassroomId });

            ChildAttendanceRecords = (await AttendanceService.GetChildAttendanceAsync(BranchId, DateTime.Today)).Where(a => a.ClassroomId == ClassroomId).ToList();
            TeacherAttendanceRecords = (await AttendanceService.GetTeacherAttendanceAsync(BranchId, DateTime.Today)).ToList();
        }
    }

    private async Task MarkChildStatus(Guid childId, string status)
    {
        var record = ChildAttendanceRecords.FirstOrDefault(a => a.ChildId == childId);
        if (record == null)
        {
            record = new ChildAttendance
            {
                BranchId = BranchId,
                ClassroomId = ClassroomId,
                ChildId = childId,
                AttendanceDate = DateTime.Today,
                Status = status,
                ArrivalTime = status == "Present" || status == "Late" ? DateTime.Now.TimeOfDay : null
            };
            ChildAttendanceRecords.Add(record);
        }
        else
        {
            record.Status = status;
            if (status == "Absent") record.ArrivalTime = null;
        }

        await AttendanceService.MarkChildAttendanceAsync(record);
    }
    
    private async Task MarkTeacherStatus(Guid teacherId, string status)
    {
        var record = TeacherAttendanceRecords.FirstOrDefault(a => a.TeacherId == teacherId);
        if (record == null)
        {
            record = new TeacherAttendance
            {
                BranchId = BranchId,
                TeacherId = teacherId,
                AttendanceDate = DateTime.Today,
                Status = status,
                ClockInTime = status == "Present" || status == "Late" ? DateTime.Now.TimeOfDay : null
            };
            TeacherAttendanceRecords.Add(record);
        }
        else
        {
            record.Status = status;
            if (status == "Absent") record.ClockInTime = null;
        }

        await AttendanceService.MarkTeacherAttendanceAsync(record);
    }
}
''')

print("Phase 2 updates generated!")

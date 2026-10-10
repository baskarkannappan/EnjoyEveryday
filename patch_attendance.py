import os

base_dir = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay'

def write_file(path, content):
    full_path = os.path.join(base_dir, path)
    os.makedirs(os.path.dirname(full_path), exist_ok=True)
    with open(full_path, 'w', encoding='utf-8') as f:
        f.write(content.strip())

# Domain Entities
write_file(r'src\EnjoyEveryday.Domain\Entities\ChildAttendance.cs', '''
namespace EnjoyEveryday.Domain.Entities;
public class ChildAttendance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid ChildId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan? ArrivalTime { get; set; }
    public TimeSpan? DepartureTime { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
''')

write_file(r'src\EnjoyEveryday.Domain\Entities\TeacherAttendance.cs', '''
namespace EnjoyEveryday.Domain.Entities;
public class TeacherAttendance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid TeacherId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan? ClockInTime { get; set; }
    public TimeSpan? ClockOutTime { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
''')

# Repository Interfaces
write_file(r'src\EnjoyEveryday.Domain\Repositories\IAttendanceRepository.cs', '''
using EnjoyEveryday.Domain.Entities;
namespace EnjoyEveryday.Domain.Repositories;
public interface IAttendanceRepository
{
    Task<IEnumerable<ChildAttendance>> GetChildAttendanceByDateAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeacherAttendance>> GetTeacherAttendanceByDateAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken cancellationToken = default);
    Task<ChildAttendance> RecordChildAttendanceAsync(ChildAttendance attendance, CancellationToken cancellationToken = default);
    Task<TeacherAttendance> RecordTeacherAttendanceAsync(TeacherAttendance attendance, CancellationToken cancellationToken = default);
}
''')

# Repositories
write_file(r'src\EnjoyEveryday.Infrastructure\Repositories\AttendanceRepository.cs', '''
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
}
''')

# Services
write_file(r'src\EnjoyEveryday.Application\Services\AttendanceService.cs', '''
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
}
''')

# Web Pages
write_file(r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\AttendanceOverview.razor', '''
@page "/management/attendance"
@using EnjoyEveryday.Application.Services
@inject OrganizationService OrganizationService
@inject BranchService BranchService
@inject NavigationManager Nav
@rendermode InteractiveServer

<div class="content" style="padding: 34px; max-width: 1200px; margin: auto;">
    <h1>Organization Attendance</h1>
    <p>Select a branch to view detailed attendance.</p>

    @if (Branches != null)
    {
        <div style="display:flex; gap: 20px; flex-wrap: wrap; margin-top:20px;">
            @foreach (var b in Branches)
            {
                <div style="padding: 20px; border: 1px solid #ddd; border-radius: 8px; cursor: pointer; width: 300px; background: white;" @onclick="() => GoToBranch(b.Id)">
                    <h3>@b.Name</h3>
                    <p>@b.Location</p>
                </div>
            }
        </div>
    }
</div>

@code {
    private IEnumerable<EnjoyEveryday.Domain.Entities.Branch> Branches = new List<EnjoyEveryday.Domain.Entities.Branch>();

    protected override async Task OnInitializedAsync()
    {
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

write_file(r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\BranchAttendance.razor', '''
@page "/management/attendance/branches/{BranchId:guid}"
@using EnjoyEveryday.Application.Services
@using EnjoyEveryday.Domain.Entities
@inject ClassroomService ClassroomService
@inject AttendanceService AttendanceService
@inject NavigationManager Nav
@rendermode InteractiveServer

<div class="content" style="padding: 34px; max-width: 1200px; margin: auto;">
    <h1>Branch Attendance</h1>
    <p>Date: @DateTime.Today.ToString("D")</p>

    @if (Classrooms != null)
    {
        <div style="display:flex; gap: 20px; flex-wrap: wrap; margin-top:20px;">
            @foreach (var c in Classrooms)
            {
                <div style="padding: 20px; border: 1px solid #ddd; border-radius: 8px; cursor: pointer; width: 300px; background: white;" @onclick="() => GoToClassroom(c.Id)">
                    <h3>@c.Description</h3>
                    <p>Capacity: @c.Capacity</p>
                    <p>Enrolled: @c.CurrentEnrollment</p>
                </div>
            }
        </div>
    }
</div>

@code {
    [Parameter] public Guid BranchId { get; set; }
    private IEnumerable<EnjoyEveryday.Domain.Entities.Classroom> Classrooms = new List<EnjoyEveryday.Domain.Entities.Classroom>();

    protected override async Task OnInitializedAsync()
    {
        Classrooms = await ClassroomService.GetClassroomsAsync(BranchId);
    }

    private void GoToClassroom(Guid id)
    {
        Nav.NavigateTo($"/management/attendance/classrooms/{id}");
    }
}
''')

write_file(r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\ClassroomAttendance.razor', '''
@page "/management/attendance/classrooms/{ClassroomId:guid}"
@using EnjoyEveryday.Application.Services
@using EnjoyEveryday.Domain.Entities
@inject AttendanceService AttendanceService
@inject EnjoyEveryday.Shared.Data.IDbConnectionFactory Db
@using Dapper
@rendermode InteractiveServer

<div class="content" style="padding: 34px; max-width: 800px; margin: auto; background: white; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
    <h1>Classroom Attendance</h1>
    <p style="color: #666; margin-bottom: 24px;">Date: @DateTime.Today.ToString("D")</p>

    @if (Children != null)
    {
        <div style="display: flex; flex-direction: column; gap: 12px;">
            @foreach (var child in Children)
            {
                var record = AttendanceRecords.FirstOrDefault(a => a.ChildId == child.Id);
                var status = record?.Status ?? "NotMarked";

                <div style="display: flex; justify-content: space-between; align-items: center; padding: 16px; border: 1px solid #eee; border-radius: 8px; background: #fafafa;">
                    <strong style="font-size: 1.1em;">@child.FirstName @child.LastName</strong>
                    
                    <div style="display: flex; gap: 8px;">
                        <button style="padding: 8px 16px; border-radius: 6px; border: 1px solid #ccc; background: @(status == "Present" ? "#d4edda" : "white"); cursor: pointer;"
                                @onclick="() => MarkStatus(child.Id, "Present")">Present</button>
                        <button style="padding: 8px 16px; border-radius: 6px; border: 1px solid #ccc; background: @(status == "Absent" ? "#f8d7da" : "white"); cursor: pointer;"
                                @onclick="() => MarkStatus(child.Id, "Absent")">Absent</button>
                        <button style="padding: 8px 16px; border-radius: 6px; border: 1px solid #ccc; background: @(status == "Late" ? "#fff3cd" : "white"); cursor: pointer;"
                                @onclick="() => MarkStatus(child.Id, "Late")">Late</button>
                    </div>
                </div>
            }
        </div>
    }
</div>

@code {
    [Parameter] public Guid ClassroomId { get; set; }
    private IEnumerable<dynamic> Children = new List<dynamic>();
    private List<ChildAttendance> AttendanceRecords = new();
    private Guid BranchId;

    protected override async Task OnInitializedAsync()
    {
        using var conn = await Db.CreateConnectionAsync();
        var classroom = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT branch_id as BranchId FROM classrooms WHERE id = @Id", new { Id = ClassroomId });
        if (classroom != null) {
            BranchId = classroom.branchid;
            Children = await conn.QueryAsync<dynamic>("SELECT id as Id, first_name as FirstName, last_name as LastName FROM children WHERE classroom_id = @Id", new { Id = ClassroomId });
            AttendanceRecords = (await AttendanceService.GetChildAttendanceAsync(BranchId, DateTime.Today)).Where(a => a.ClassroomId == ClassroomId).ToList();
        }
    }

    private async Task MarkStatus(Guid childId, string status)
    {
        var record = AttendanceRecords.FirstOrDefault(a => a.ChildId == childId);
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
            AttendanceRecords.Add(record);
        }
        else
        {
            record.Status = status;
            if (status == "Absent") record.ArrivalTime = null;
        }

        await AttendanceService.MarkChildAttendanceAsync(record);
    }
}
''')

print("Files generated!")

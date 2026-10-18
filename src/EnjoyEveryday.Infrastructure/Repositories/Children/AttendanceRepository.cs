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
            INSERT INTO child_attendance (
                id, tenant_id, branch_id, classroom_id, child_id, attendance_date, status, 
                arrival_time, departure_time, notes, recorded_by_user_id,
                pickup_person_name, pickup_person_relationship, pickup_verification_method, pickup_notes, authorized_pickup_id, departure_verified_by_user_id,
                created_at, updated_at
            )
            VALUES (
                @Id, @TenantId, @BranchId, @ClassroomId, @ChildId, @AttendanceDate, @Status, 
                @ArrivalTime, @DepartureTime, @Notes, @RecordedByUserId,
                @PickupPersonName, @PickupPersonRelationship, @PickupVerificationMethod, @PickupNotes, @AuthorizedPickupId, @DepartureVerifiedByUserId,
                CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
            )
            ON CONFLICT (id) DO UPDATE SET 
                status = EXCLUDED.status, 
                arrival_time = EXCLUDED.arrival_time, 
                departure_time = EXCLUDED.departure_time, 
                notes = EXCLUDED.notes,
                pickup_person_name = EXCLUDED.pickup_person_name,
                pickup_person_relationship = EXCLUDED.pickup_person_relationship,
                pickup_verification_method = EXCLUDED.pickup_verification_method,
                pickup_notes = EXCLUDED.pickup_notes,
                authorized_pickup_id = EXCLUDED.authorized_pickup_id,
                departure_verified_by_user_id = EXCLUDED.departure_verified_by_user_id,
                updated_at = CURRENT_TIMESTAMP;";
        await connection.ExecuteAsync(sql, attendance);
        return attendance;
    }
    public async Task<TeacherAttendance> RecordTeacherAttendanceAsync(TeacherAttendance attendance, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Ensure teacher_id satisfies fk_ta_teacher constraint
        var actualTeacherId = await connection.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT id FROM teachers WHERE id = @TeacherId OR user_id = @TeacherId LIMIT 1",
            new { attendance.TeacherId });

        if (!actualTeacherId.HasValue || actualTeacherId.Value == Guid.Empty)
        {
            // If the ID was a User ID, provision a corresponding teacher record so FK holds
            var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, tenant_id, first_name, last_name, display_name FROM users WHERE id = @UserId",
                new { UserId = attendance.TeacherId });

            if (user != null)
            {
                var newTeacherId = Guid.NewGuid();
                var tenantId = attendance.TenantId != Guid.Empty ? attendance.TenantId : (Guid)user.tenant_id;
                await connection.ExecuteAsync(@"
                    INSERT INTO teachers (id, tenant_id, user_id, first_name, last_name, display_name, status, created_at, updated_at)
                    VALUES (@Id, @TenantId, @UserId, @FirstName, @LastName, @DisplayName, 'Active', NOW(), NOW())
                    ON CONFLICT (id) DO NOTHING",
                    new {
                        Id = newTeacherId,
                        TenantId = tenantId,
                        UserId = (Guid)user.id,
                        FirstName = (string)(user.first_name ?? "Teacher"),
                        LastName = (string)(user.last_name ?? ""),
                        DisplayName = (string)(user.display_name ?? (user.first_name + " " + user.last_name))
                    });
                actualTeacherId = newTeacherId;
            }
        }

        if (actualTeacherId.HasValue && actualTeacherId.Value != Guid.Empty)
        {
            attendance.TeacherId = actualTeacherId.Value;
        }

        if (attendance.Id == Guid.Empty)
        {
            var existingId = await connection.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT id FROM teacher_attendance WHERE branch_id = @BranchId AND teacher_id = @TeacherId AND attendance_date = @AttendanceDate",
                new { attendance.BranchId, attendance.TeacherId, attendance.AttendanceDate });
            attendance.Id = existingId ?? Guid.NewGuid();
        }

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
                (SELECT COUNT(*) FROM children WHERE tenant_id = @TenantId AND is_active = true) AS ExpectedChildren,
                (SELECT COUNT(*) FROM child_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Present') AS PresentChildren,
                (SELECT COUNT(*) FROM child_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Absent') AS AbsentChildren,
                (SELECT COUNT(*) FROM child_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Late') AS LateChildren,
                
                (SELECT COUNT(*) FROM teachers WHERE tenant_id = @TenantId AND status != 'Inactive') AS ExpectedTeachers,
                (SELECT COUNT(*) FROM teacher_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Present') AS PresentTeachers,
                (SELECT COUNT(*) FROM teacher_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Absent') AS AbsentTeachers,
                (SELECT COUNT(*) FROM teacher_attendance WHERE tenant_id = @TenantId AND attendance_date = @Date AND status = 'Late') AS LateTeachers
        ";
        return await connection.QuerySingleAsync<AttendanceSummary>(sql, new { TenantId = tenantId, Date = date });
    }

    public async Task<IEnumerable<Guid>> GetPresentChildIdsByClassroomAsync(Guid tenantId, Guid classroomId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = "SELECT child_id FROM child_attendance WHERE tenant_id = @TenantId AND classroom_id = @ClassroomId AND attendance_date = @Date AND status IN ('Present', 'Late')";
        return await connection.QueryAsync<Guid>(sql, new { TenantId = tenantId, ClassroomId = classroomId, Date = date });
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ClassroomReadinessRepository : IClassroomReadinessRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ClassroomReadinessRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<BranchReadinessData> GetBranchReadinessDataAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        // 1. Branch details
        var branch = await conn.QueryFirstOrDefaultAsync<BranchRow>(
            @"SELECT id, name, is_active FROM branches WHERE tenant_id = @TenantId AND id = @BranchId",
            new { TenantId = tenantId, BranchId = branchId });

        if (branch == null)
        {
            return new BranchReadinessData
            {
                BranchExists = false,
                BranchActive = false,
                BranchName = string.Empty,
                LeadTeacherCount = 0,
                Classrooms = new List<ClassroomReadinessData>()
            };
        }

        // 2. Classrooms (excluding system virtual pool dummy rooms)
        var classrooms = (await conn.QueryAsync<ClassroomRow>(
            @"SELECT id, name, is_active, status, COALESCE(capacity, 15) as capacity 
              FROM classrooms 
              WHERE tenant_id = @TenantId AND branch_id = @BranchId
                AND (classroom_type IS NULL OR classroom_type != 'Pool')
                AND (code IS NULL OR code != 'POOL-WAITING')",
            new { TenantId = tenantId, BranchId = branchId })).ToList();

        // 3. Lead Teacher count in branch
        var leadTeacherCount = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(DISTINCT u.id)
              FROM users u
              JOIN (
                  SELECT uor.user_id
                  FROM user_organization_roles uor
                  JOIN roles r ON uor.role_id = r.id AND (r.name = 'LeadTeacher' OR r.name = 'Lead Teacher')
                  WHERE uor.tenant_id = @TenantId OR uor.organization_id = (SELECT organization_id FROM branches WHERE id = @BranchId)
                  UNION
                  SELECT ur.user_id
                  FROM user_roles ur
                  JOIN roles r ON ur.role_id = r.id AND (r.name = 'LeadTeacher' OR r.name = 'Lead Teacher')
              ) lead_users ON lead_users.user_id = u.id
              LEFT JOIN teachers t ON t.user_id = u.id
              WHERE u.is_active = true AND (t.status IS NULL OR t.status != 'Inactive')
                AND (
                    EXISTS (
                        SELECT 1 FROM user_branch_assignments uba
                        WHERE uba.user_id = u.id AND uba.branch_id = @BranchId AND uba.revoked_at IS NULL
                    )
                    OR EXISTS (
                        SELECT 1 FROM classroom_teachers ct
                        JOIN classrooms c ON ct.classroom_id = c.id
                        WHERE ct.user_id = u.id AND c.branch_id = @BranchId
                    )
                )",
            new { TenantId = tenantId, BranchId = branchId });

        if (leadTeacherCount == 0)
        {
            leadTeacherCount = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(DISTINCT ct.user_id)
                  FROM classroom_teachers ct
                  JOIN classrooms c ON ct.classroom_id = c.id
                  JOIN users u ON ct.user_id = u.id
                  LEFT JOIN teachers t ON t.user_id = u.id
                  WHERE c.branch_id = @BranchId AND ct.is_primary = true
                    AND u.is_active = true AND (t.status IS NULL OR t.status != 'Inactive')",
                new { TenantId = tenantId, BranchId = branchId });
        }

        // 4. Assigned teachers per classroom
        var teacherCounts = (await conn.QueryAsync<ClassroomCountRow>(
            @"SELECT ct.classroom_id as ClassroomId, COUNT(DISTINCT ct.user_id) as Count
              FROM classroom_teachers ct
              JOIN classrooms c ON ct.classroom_id = c.id
              JOIN users u ON ct.user_id = u.id
              LEFT JOIN teachers t ON t.user_id = u.id
              WHERE c.branch_id = @BranchId
                AND u.is_active = true AND (t.status IS NULL OR t.status != 'Inactive')
              GROUP BY ct.classroom_id",
            new { TenantId = tenantId, BranchId = branchId })).ToDictionary(r => r.ClassroomId, r => r.Count);

        // 5. Enrolled children per classroom
        var enrollmentCounts = (await conn.QueryAsync<ClassroomCountRow>(
            @"SELECT ch.classroom_id as ClassroomId, COUNT(*) as Count
              FROM children ch
              JOIN classrooms c ON ch.classroom_id = c.id
              WHERE c.branch_id = @BranchId
                AND ch.is_active = true AND (ch.enrollment_status IS NULL OR ch.enrollment_status != 'Withdrawn')
              GROUP BY ch.classroom_id",
            new { TenantId = tenantId, BranchId = branchId })).ToDictionary(r => r.ClassroomId, r => r.Count);

        var classroomDataList = classrooms.Select(c => new ClassroomReadinessData
        {
            Id = c.id,
            Name = c.name,
            IsActive = c.is_active,
            Status = c.status,
            Capacity = c.capacity > 0 ? c.capacity : 15,
            CurrentEnrolled = enrollmentCounts.TryGetValue(c.id, out var enc) ? enc : 0,
            AssignedTeacherCount = teacherCounts.TryGetValue(c.id, out var tc) ? tc : 0
        }).ToList();

        return new BranchReadinessData
        {
            BranchExists = true,
            BranchName = branch.name,
            BranchActive = branch.is_active,
            LeadTeacherCount = leadTeacherCount,
            Classrooms = classroomDataList
        };
    }

    private sealed class BranchRow
    {
        public Guid id { get; set; }
        public string name { get; set; } = string.Empty;
        public bool is_active { get; set; }
    }

    private sealed class ClassroomRow
    {
        public Guid id { get; set; }
        public string name { get; set; } = string.Empty;
        public bool is_active { get; set; }
        public string? status { get; set; }
        public int capacity { get; set; }
    }

    private sealed class ClassroomCountRow
    {
        public Guid ClassroomId { get; set; }
        public int Count { get; set; }
    }
}

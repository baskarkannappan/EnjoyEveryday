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

        var rows = (await conn.QueryAsync<BranchReadinessDbRow>(
            new CommandDefinition(
                "SELECT branch_exists, branch_name, branch_is_active, lead_teacher_count, classroom_id, classroom_name, classroom_is_active, classroom_status, capacity, current_enrolled, assigned_teacher_count FROM fn_get_branch_readiness(@tenantId, @branchId)",
                new { tenantId, branchId },
                cancellationToken: ct))).ToList();

        if (rows.Count == 0 || !rows[0].branch_exists)
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

        var first = rows[0];
        var classrooms = rows
            .Where(r => r.classroom_id.HasValue)
            .Select(r => new ClassroomReadinessData
            {
                Id = r.classroom_id!.Value,
                Name = r.classroom_name ?? string.Empty,
                IsActive = r.classroom_is_active,
                Status = r.classroom_status,
                Capacity = r.capacity > 0 ? r.capacity : 15,
                CurrentEnrolled = r.current_enrolled,
                AssignedTeacherCount = r.assigned_teacher_count
            }).ToList();

        return new BranchReadinessData
        {
            BranchExists = first.branch_exists,
            BranchName = first.branch_name ?? string.Empty,
            BranchActive = first.branch_is_active,
            LeadTeacherCount = first.lead_teacher_count,
            Classrooms = classrooms
        };
    }

    private sealed class BranchReadinessDbRow
    {
        public bool branch_exists { get; set; }
        public string? branch_name { get; set; }
        public bool branch_is_active { get; set; }
        public int lead_teacher_count { get; set; }
        public Guid? classroom_id { get; set; }
        public string? classroom_name { get; set; }
        public bool classroom_is_active { get; set; }
        public string? classroom_status { get; set; }
        public int capacity { get; set; }
        public int current_enrolled { get; set; }
        public int assigned_teacher_count { get; set; }
    }
}

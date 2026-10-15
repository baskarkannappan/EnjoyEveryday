using Dapper;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;
using System.Data;

namespace EnjoyEveryday.Application.Services;

public class OrganizationMetrics
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public int TotalBranches { get; set; }
    public int TotalClassrooms { get; set; }
    public int ActiveBranchCount { get; set; }
    public int ChildrenExperiencingToday { get; set; }
    public int ExperiencesThisWeek { get; set; }
    public int ExperiencesThisWeekCompleted { get; set; }
    public int ExperiencesThisWeekUpcoming { get; set; }
    public int TeacherCreatedExperiences { get; set; }
    public int TeacherCreatedExperiencesThisMonth { get; set; }
    public int TotalChildren { get; set; }
    public int TotalTeachers { get; set; }
}

public class BranchMetrics
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Classrooms { get; set; }
    public int Children { get; set; }
    public int Experiences { get; set; }
    public int ExperienceRhythm { get; set; }
    public int Teachers { get; set; }
    public string AttentionStatus { get; set; } = string.Empty;
    public string RecentInsight { get; set; } = string.Empty;
}

public class DashboardService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public DashboardService(
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
    }

    public async Task<OrganizationMetrics> GetOrganizationMetricsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            -- Org details
            SELECT id AS OrganizationId, name AS OrganizationName 
            FROM organizations 
            WHERE id = @OrganizationId AND tenant_id = @TenantId;

            -- Branch stats
            SELECT COUNT(*) AS TotalBranches, 
                   COUNT(*) FILTER (WHERE is_active = true) AS ActiveBranchCount,
                   (SELECT COUNT(*) FROM classrooms WHERE tenant_id = @TenantId) AS TotalClassrooms,
                   (SELECT COUNT(*) FROM children WHERE tenant_id = @TenantId) AS TotalChildren,
                   (SELECT COUNT(*) FROM teachers WHERE tenant_id = @TenantId) AS TotalTeachers
            FROM branches 
            WHERE organization_id = @OrganizationId AND tenant_id = @TenantId;
        ";

        using var multi = await connection.QueryMultipleAsync(sql, new { OrganizationId = organizationId, TenantId = tenantId });
        
        var org = await multi.ReadSingleOrDefaultAsync<OrganizationMetrics>() ?? new OrganizationMetrics();
        var branchStats = await multi.ReadSingleOrDefaultAsync<dynamic>();

        org.TotalBranches = Convert.ToInt32(branchStats?.totalbranches ?? 0);
        org.ActiveBranchCount = Convert.ToInt32(branchStats?.activebranchcount ?? 0);
        org.TotalClassrooms = Convert.ToInt32(branchStats?.totalclassrooms ?? 0);
        org.TotalChildren = Convert.ToInt32(branchStats?.totalchildren ?? 0);
        org.TotalTeachers = Convert.ToInt32(branchStats?.totalteachers ?? 0);
        
        var expStatsSql = @"
            SELECT 
                (SELECT COUNT(DISTINCT c.id) 
                 FROM children c 
                 JOIN experience_schedules es ON c.classroom_id = es.classroom_id 
                 WHERE es.tenant_id = @TenantId AND es.scheduled_date = CURRENT_DATE) AS ChildrenExperiencingToday,
                
                (SELECT COUNT(*) 
                 FROM experience_schedules 
                 WHERE tenant_id = @TenantId 
                 AND scheduled_date >= CURRENT_DATE - INTERVAL '7 days') AS ExperiencesThisWeek,
                
                (SELECT COUNT(*) 
                 FROM experience_schedules 
                 WHERE tenant_id = @TenantId 
                 AND scheduled_date >= CURRENT_DATE - INTERVAL '7 days' 
                 AND status IN ('Completed', 'Finished')) AS ExperiencesThisWeekCompleted,
                 
                (SELECT COUNT(*) 
                 FROM experience_schedules 
                 WHERE tenant_id = @TenantId 
                 AND scheduled_date >= CURRENT_DATE 
                 AND status NOT IN ('Completed', 'Finished')) AS ExperiencesThisWeekUpcoming,
                 
                (SELECT COUNT(*) 
                 FROM experiences 
                 WHERE tenant_id = @TenantId) AS TeacherCreatedExperiences,
                 
                (SELECT COUNT(*) 
                 FROM experiences 
                 WHERE tenant_id = @TenantId 
                 AND created_at >= date_trunc('month', CURRENT_DATE)) AS TeacherCreatedExperiencesThisMonth
        ";
        
        var expStats = await connection.QuerySingleOrDefaultAsync<dynamic>(expStatsSql, new { TenantId = tenantId });

        org.ChildrenExperiencingToday = Convert.ToInt32(expStats?.childrenexperiencingtoday ?? 0);
        org.ExperiencesThisWeek = Convert.ToInt32(expStats?.experiencesthisweek ?? 0);
        org.ExperiencesThisWeekCompleted = Convert.ToInt32(expStats?.experiencesthisweekcompleted ?? 0);
        org.ExperiencesThisWeekUpcoming = Convert.ToInt32(expStats?.experiencesthisweekupcoming ?? 0);
        org.TeacherCreatedExperiences = Convert.ToInt32(expStats?.teachercreatedexperiences ?? 0);
        org.TeacherCreatedExperiencesThisMonth = Convert.ToInt32(expStats?.teachercreatedexperiencesthismonth ?? 0);

        return org;
    }

    public async Task<IEnumerable<BranchMetrics>> GetBranchMetricsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT 
                b.id AS BranchId, 
                b.name AS BranchName, 
                b.location AS Location, 
                CASE WHEN b.is_active THEN 'Active' ELSE 'Inactive' END AS Status,
                (SELECT COUNT(*) FROM classrooms c WHERE c.branch_id = b.id AND c.tenant_id = @TenantId) AS Classrooms,
                (SELECT COUNT(*) FROM children ch JOIN classrooms c ON ch.classroom_id = c.id WHERE c.branch_id = b.id AND ch.tenant_id = @TenantId) AS Children,
                (SELECT COUNT(*) FROM experience_schedules es JOIN classrooms c ON es.classroom_id = c.id WHERE c.branch_id = b.id AND es.tenant_id = @TenantId) AS Experiences
            FROM branches b
            WHERE b.organization_id = @OrganizationId AND b.tenant_id = @TenantId
            ORDER BY b.name;
        ";

        var branches = await connection.QueryAsync<BranchMetrics>(sql, new { OrganizationId = organizationId, TenantId = tenantId });

        foreach (var branch in branches)
        {
            branch.ExperienceRhythm = branch.Experiences > 0 ? Math.Min(100, branch.Experiences * 10) : 0;
            branch.Teachers = branch.Classrooms * 2; // rough estimate
            branch.AttentionStatus = branch.Experiences < 5 ? "Needs support" : (branch.Experiences < 10 ? "Quiet this week" : "Healthy rhythm");
            branch.RecentInsight = branch.Experiences < 5 ? "Very few experiences scheduled." : "Activity is normal.";
        }

        return branches;
    }
}

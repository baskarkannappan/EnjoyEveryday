using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class OrganizationMetrics
{
    public int TotalBranches { get; set; }
    public int TotalClassrooms { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalChildren { get; set; }
}

public class BranchMetrics
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Classrooms { get; set; }
    public int Children { get; set; }
    public int Teachers { get; set; }
}

public class DashboardService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IChildRepository _childRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantContext _tenantContext;

    public DashboardService(
        IOrganizationRepository organizationRepository,
        IBranchRepository branchRepository,
        IClassroomRepository classroomRepository,
        IChildRepository childRepository,
        IUserRepository userRepository,
        ITenantContext tenantContext)
    {
        _organizationRepository = organizationRepository;
        _branchRepository = branchRepository;
        _classroomRepository = classroomRepository;
        _childRepository = childRepository;
        _userRepository = userRepository;
        _tenantContext = tenantContext;
    }

    public async Task<OrganizationMetrics> GetOrganizationMetricsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var branches = (await _branchRepository.GetByOrganizationIdAsync(tenantId, organizationId, cancellationToken)).ToList();
        
        int totalClassrooms = 0;
        int totalChildren = 0;

        var teachers = (await _userRepository.GetUsersByRoleAsync(tenantId, "Teacher", cancellationToken)).ToList();
        int totalTeachers = teachers.Count;

        foreach (var branch in branches)
        {
            var classrooms = (await _classroomRepository.GetByBranchIdAsync(tenantId, branch.Id, cancellationToken)).ToList();
            totalClassrooms += classrooms.Count;

            foreach (var classroom in classrooms)
            {
                var children = await _childRepository.GetByClassroomIdAsync(tenantId, classroom.Id, cancellationToken);
                totalChildren += children.Count();
            }
        }

        return new OrganizationMetrics
        {
            TotalBranches = branches.Count,
            TotalClassrooms = totalClassrooms,
            TotalTeachers = totalTeachers,
            TotalChildren = totalChildren
        };
    }

    public async Task<IEnumerable<BranchMetrics>> GetBranchMetricsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var branches = await _branchRepository.GetByOrganizationIdAsync(tenantId, organizationId, cancellationToken);
        
        var metricsList = new List<BranchMetrics>();

        var allTeachers = (await _userRepository.GetUsersByRoleAsync(tenantId, "Teacher", cancellationToken)).ToList();

        foreach (var branch in branches)
        {
            var classrooms = (await _classroomRepository.GetByBranchIdAsync(tenantId, branch.Id, cancellationToken)).ToList();
            
            int childCount = 0;
            foreach (var c in classrooms)
            {
                var children = await _childRepository.GetByClassroomIdAsync(tenantId, c.Id, cancellationToken);
                childCount += children.Count();
            }

            metricsList.Add(new BranchMetrics
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                Location = branch.Location ?? "",
                Classrooms = classrooms.Count,
                Teachers = allTeachers.Count / (!branches.Any() ? 1 : branches.Count()), // Rough distribution for now
                Children = childCount
            });
        }

        return metricsList;
    }
}

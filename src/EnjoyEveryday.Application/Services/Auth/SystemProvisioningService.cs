using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class SystemProvisioningService
{
    private readonly BranchService _branchService;
    private readonly ClassroomService _classroomService;
    private readonly OrganizationService _organizationService;
    private readonly ITenantContext _tenantContext;

    public SystemProvisioningService(
        BranchService branchService,
        ClassroomService classroomService,
        OrganizationService organizationService,
        ITenantContext tenantContext)
    {
        _branchService = branchService;
        _classroomService = classroomService;
        _organizationService = organizationService;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> EnsureDefaultBranchAsync()
    {
        var orgs = await _organizationService.GetOrganizationsAsync();
        var org = orgs.FirstOrDefault();
        if (org == null) throw new InvalidOperationException("No organization found for tenant.");

        var branches = await _branchService.GetBranchesAsync(org.Id);
        var defaultBranch = branches.FirstOrDefault(b => b.Name == "Default Branch");

        if (defaultBranch == null)
        {
            // If there's literally no branches, create it
            if (!branches.Any())
            {
                defaultBranch = await _branchService.CreateBranchAsync(org.Id, "Default Branch", "HQ");
            }
            else
            {
                // Just use the first available branch as default
                defaultBranch = branches.First();
            }
        }

        return defaultBranch.Id;
    }

    public async Task<Guid> EnsureWaitingPoolAsync()
    {
        var defaultBranchId = await EnsureDefaultBranchAsync();
        var classrooms = await _classroomService.GetClassroomsAsync(defaultBranchId);
        
        var pool = classrooms.FirstOrDefault(c => c.Code == "POOL-WAITING");
        if (pool == null)
        {
            pool = await _classroomService.CreateClassroomAsync(
                branchId: defaultBranchId,
                name: "Waiting Pool",
                ageGroup: "Any",
                capacity: 999,
                environment: "Virtual"
            );
            pool.Code = "POOL-WAITING";
            pool.Description = "System classroom for children awaiting assignment.";
            pool.ClassroomType = "Pool";
            await _classroomService.UpdateClassroomAsync(pool);
        }

        return pool.Id;
    }

    public async Task<Guid> EnsureTeacherPoolAsync()
    {
        var defaultBranchId = await EnsureDefaultBranchAsync();
        var classrooms = await _classroomService.GetClassroomsAsync(defaultBranchId);
        
        var pool = classrooms.FirstOrDefault(c => c.Code == "POOL-TEACHERS");
        if (pool == null)
        {
            pool = await _classroomService.CreateClassroomAsync(
                branchId: defaultBranchId,
                name: "Default Pool",
                ageGroup: "N/A",
                capacity: 999,
                environment: "Virtual"
            );
            pool.Code = "POOL-TEACHERS";
            pool.Description = "System classroom for unassigned teachers.";
            pool.ClassroomType = "Pool";
            await _classroomService.UpdateClassroomAsync(pool);
        }

        return pool.Id;
    }
}

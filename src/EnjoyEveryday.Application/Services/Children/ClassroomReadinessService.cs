using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ClassroomReadinessService : IClassroomReadinessService
{
    private readonly IClassroomReadinessRepository _readinessRepository;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IResourceAuthorizer _authorizer;
    private readonly IAccessScopeRepository _scopeRepo;

    public ClassroomReadinessService(
        IClassroomReadinessRepository readinessRepository,
        IUserContext userContext,
        ITenantContext tenantContext,
        IResourceAuthorizer authorizer,
        IAccessScopeRepository scopeRepo)
    {
        _readinessRepository = readinessRepository ?? throw new ArgumentNullException(nameof(readinessRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
        _scopeRepo = scopeRepo ?? throw new ArgumentNullException(nameof(scopeRepo));
    }

    public async Task<ClassroomReadinessCheckResult> ValidateBranchReadinessAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        await EnforceBranchAccessAsync(tenantId, branchId, ct);
        var branchData = await _readinessRepository.GetBranchReadinessDataAsync(tenantId, branchId, ct);

        return EvaluateInternal(branchId, branchData, null);
    }

    public async Task<ClassroomReadinessCheckResult> ValidateClassroomReadinessAsync(Guid tenantId, Guid branchId, Guid classroomId, CancellationToken ct = default)
    {
        await EnforceBranchAccessAsync(tenantId, branchId, ct);
        var branchData = await _readinessRepository.GetBranchReadinessDataAsync(tenantId, branchId, ct);

        return EvaluateInternal(branchId, branchData, classroomId);
    }

    public async Task<Dictionary<Guid, ClassroomReadinessCheckResult>> GetBranchClassroomsReadinessAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        await EnforceBranchAccessAsync(tenantId, branchId, ct);
        var branchData = await _readinessRepository.GetBranchReadinessDataAsync(tenantId, branchId, ct);

        var dict = new Dictionary<Guid, ClassroomReadinessCheckResult>();
        foreach (var cls in branchData.Classrooms)
        {
            var res = EvaluateInternal(branchId, branchData, cls.Id);
            dict[cls.Id] = res;
        }

        return dict;
    }

    public async Task EnsureClassroomReadyForEnrollmentAsync(Guid tenantId, Guid branchId, Guid classroomId, CancellationToken ct = default)
    {
        var result = await ValidateClassroomReadinessAsync(tenantId, branchId, classroomId, ct);
        if (!result.IsReady)
        {
            throw new ClassroomReadinessException(result);
        }
    }

    private async Task EnforceBranchAccessAsync(Guid tenantId, Guid branchId, CancellationToken ct)
    {
        if (!_userContext.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Branch Admin check: must belong to assigned branches
        var userRoles = (_userContext.Roles ?? Enumerable.Empty<string>()).ToList();
        var isOrgAdmin = userRoles.Any(r =>
            r.Equals(RoleTemplates.PlatformAdmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(RoleTemplates.OrganizationOwner, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(RoleTemplates.OrganizationAdmin, StringComparison.OrdinalIgnoreCase));

        var isBranchAdmin = !isOrgAdmin && userRoles.Any(r => r.Equals(RoleTemplates.BranchAdmin, StringComparison.OrdinalIgnoreCase));

        if (isBranchAdmin && _userContext.OrganizationId.HasValue)
        {
            var assignedBranches = await _scopeRepo.GetAssignedBranchIdsAsync(tenantId, _userContext.OrganizationId.Value, _userContext.UserId, ct);
            if (!assignedBranches.Contains(branchId))
            {
                throw new UnauthorizedAccessException("Branch Admin is not authorized for the specified branch.");
            }
        }
    }

    private static ClassroomReadinessCheckResult EvaluateInternal(
        Guid branchId,
        BranchReadinessData branchData,
        Guid? classroomId)
    {
        var missing = new List<string>();
        var actions = new List<ReadinessActionItem>();

        // 1. Branch Validation
        if (!branchData.BranchExists)
        {
            return new ClassroomReadinessCheckResult
            {
                IsReady = false,
                Status = "Inactive",
                BranchId = branchId,
                BranchExists = false,
                BranchActive = false,
                MissingRequirements = new List<string> { "The specified branch does not exist." }
            };
        }

        bool branchActive = branchData.BranchActive;
        if (!branchActive)
        {
            missing.Add("This branch is currently inactive. Please activate the branch before onboarding a new student.");
            actions.Add(new ReadinessActionItem("Branch Inactive", "Activate the branch to allow operations.", "/management/branch-profile", "Manage Branch"));
        }

        // 2. Classrooms Count Check
        bool hasClassrooms = branchData.Classrooms.Count > 0;
        if (!hasClassrooms)
        {
            missing.Add("No classrooms have been created for this branch. Please create a classroom before onboarding a new student.");
            actions.Add(new ReadinessActionItem("No Classroom", "Create a classroom before onboarding a new student.", "/classrooms", "Create Classroom"));
        }

        // 3. Lead Teacher in Branch Check
        bool hasLeadTeacher = branchData.LeadTeacherCount > 0;
        if (!hasLeadTeacher)
        {
            missing.Add("No Lead Teacher has been assigned to this branch. Please create or assign a Lead Teacher before onboarding a new student.");
            actions.Add(new ReadinessActionItem("No Lead Teacher", "Create or assign a Lead Teacher to this branch.", "/teachers/new", "Create Lead Teacher"));
        }

        // If no classroom specified, return branch-level status
        if (!classroomId.HasValue)
        {
            bool branchReady = branchActive && hasClassrooms && hasLeadTeacher;
            return new ClassroomReadinessCheckResult
            {
                IsReady = branchReady,
                Status = branchReady ? "Ready" : (!branchActive ? "Inactive" : "Setup Required"),
                BranchId = branchId,
                BranchName = branchData.BranchName,
                BranchExists = true,
                BranchActive = branchActive,
                HasClassrooms = hasClassrooms,
                HasLeadTeacher = hasLeadTeacher,
                MissingRequirements = missing,
                ActionItems = actions
            };
        }

        // 4. Specific Classroom Check
        var targetClassroom = branchData.Classrooms.FirstOrDefault(c => c.Id == classroomId.Value);
        if (targetClassroom == null)
        {
            missing.Add("The selected classroom was not found in this branch.");
            return new ClassroomReadinessCheckResult
            {
                IsReady = false,
                Status = "Setup Required",
                BranchId = branchId,
                BranchName = branchData.BranchName,
                BranchExists = true,
                BranchActive = branchActive,
                HasClassrooms = hasClassrooms,
                HasLeadTeacher = hasLeadTeacher,
                ClassroomId = classroomId.Value,
                ClassroomExists = false,
                ClassroomActive = false,
                MissingRequirements = missing,
                ActionItems = actions
            };
        }

        bool classroomActive = targetClassroom.IsActive && !string.Equals(targetClassroom.Status, "Inactive", StringComparison.OrdinalIgnoreCase);
        if (!classroomActive)
        {
            missing.Add("This classroom is currently inactive. Please activate the classroom or select another active classroom.");
            actions.Add(new ReadinessActionItem("Classroom Inactive", "Activate the classroom or select another active classroom.", "/classrooms", "Manage Classrooms"));
        }

        // 5. Active Teacher Assigned to Classroom Check
        bool hasAssignedTeacher = targetClassroom.AssignedTeacherCount > 0;
        if (!hasAssignedTeacher)
        {
            missing.Add("This classroom does not have any active Teachers assigned. Please assign at least one Teacher before onboarding a new student.");
            actions.Add(new ReadinessActionItem("No Teacher Assigned", "Assign at least one active Teacher to this classroom.", "/staff/roster", "Assign Teacher"));
        }

        // 6. Classroom Capacity & Availability Check
        int capacity = targetClassroom.Capacity > 0 ? targetClassroom.Capacity : 15;
        bool hasCapacity = targetClassroom.CurrentEnrolled < capacity;
        if (!hasCapacity)
        {
            missing.Add("This classroom has reached its maximum capacity. Please select another classroom or update its capacity if appropriate.");
            actions.Add(new ReadinessActionItem("Classroom Full", "Classroom is full. Select another room or adjust capacity.", "/classrooms", "View Classrooms"));
        }

        bool isReady = branchActive && hasClassrooms && hasLeadTeacher && classroomActive && hasAssignedTeacher && hasCapacity;
        
        string status = isReady ? "Ready" :
                        (!branchActive || !classroomActive) ? "Inactive" :
                        (!hasCapacity) ? "Full" : "Setup Required";

        return new ClassroomReadinessCheckResult
        {
            IsReady = isReady,
            Status = status,
            BranchId = branchId,
            BranchName = branchData.BranchName,
            BranchExists = true,
            BranchActive = branchActive,
            HasClassrooms = hasClassrooms,
            HasLeadTeacher = hasLeadTeacher,
            ClassroomId = targetClassroom.Id,
            ClassroomName = targetClassroom.Name,
            ClassroomExists = true,
            ClassroomActive = classroomActive,
            HasAssignedTeacher = hasAssignedTeacher,
            HasAvailableCapacity = hasCapacity,
            Capacity = capacity,
            CurrentEnrolled = targetClassroom.CurrentEnrolled,
            MissingRequirements = missing,
            ActionItems = actions
        };
    }
}

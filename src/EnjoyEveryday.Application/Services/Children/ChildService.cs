using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;

namespace EnjoyEveryday.Application.Services;

public class ChildService
{
    private readonly IChildRepository _childRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;
    private readonly IUserContext _userContext;
    private readonly IResourceAuthorizer _authorizer;
    private readonly IClassroomRepository? _classroomRepository;
    private readonly IClassroomReadinessService? _readinessService;
    private readonly IPlanQuotaService? _quotaService;

    public ChildService(
        IChildRepository childRepository, 
        ITenantContext tenantContext, 
        IAuditService auditService, 
        IUserContext userContext,
        IPlanQuotaService? quotaService = null)
        : this(childRepository, tenantContext, auditService, userContext, new NullResourceAuthorizer(), null, null, quotaService)
    {
    }

    public ChildService(
        IChildRepository childRepository,
        ITenantContext tenantContext,
        IAuditService auditService,
        IUserContext userContext,
        IResourceAuthorizer authorizer,
        IPlanQuotaService? quotaService = null)
        : this(childRepository, tenantContext, auditService, userContext, authorizer, null, null, quotaService)
    {
    }

    public ChildService(
        IChildRepository childRepository,
        ITenantContext tenantContext,
        IAuditService auditService,
        IUserContext userContext,
        IResourceAuthorizer authorizer,
        IClassroomRepository? classroomRepository,
        IClassroomReadinessService? readinessService,
        IPlanQuotaService? quotaService = null)
    {
        _childRepository = childRepository;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _userContext = userContext;
        _authorizer = authorizer;
        _classroomRepository = classroomRepository;
        _readinessService = readinessService;
        _quotaService = quotaService;
    }

    public async Task<IEnumerable<Child>> GetChildrenAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var orgId = _userContext.OrganizationId;
        var all = await _childRepository.GetAllAsync(tenantId, orgId, cancellationToken);
        // Per-child scope check: org admin sees the org, branch admin assigned branches, teachers assigned classrooms.
        return await _authorizer.FilterAccessibleChildrenAsync(all, Permissions.ChildView, cancellationToken);
    }

    public async Task<IEnumerable<Child>> GetChildrenByClassroomAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        await _authorizer.RequireClassroomAsync(classroomId, Permissions.ChildView, cancellationToken);
        var tenantId = _tenantContext.TenantId;
        return await _childRepository.GetByClassroomIdAsync(tenantId, classroomId, cancellationToken);
    }

    public async Task<Child?> GetChildByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Unknown id, other tenant and out-of-scope all fail identically (no existence disclosure).
        await _authorizer.RequireChildAsync(id, Permissions.ChildView, cancellationToken);
        var tenantId = _tenantContext.TenantId;
        return await _childRepository.GetByIdAsync(tenantId, id, cancellationToken);
    }

    public async Task<Child> CreateChildAsync(string firstName, string lastName, DateTime? dateOfBirth, Guid? classroomId, string? photoUrl = null, bool isOfficialAdmission = false, CancellationToken cancellationToken = default)
    {
        var hasManage = _userContext.HasPermission(Permissions.ChildManage);
        var hasEnrollment = _userContext.HasPermission(Permissions.ChildEnrollmentManage);
        if (!hasManage && !hasEnrollment) throw new UnauthorizedAccessException("Requires child.manage or child.enrollment.manage permission.");

        // Direct child creation bypassing admissions is prohibited when running in full application context
        if (!isOfficialAdmission && _readinessService != null)
        {
            throw new InvalidOperationException("Direct child creation is disabled. All new children must enter through the official Inquiry and Admission pipeline.");
        }

        if (_quotaService != null)
        {
            var orgId = _userContext.OrganizationId ?? Guid.Empty;
            if (orgId != Guid.Empty)
            {
                await _quotaService.ValidateCanAddChildAsync(orgId, cancellationToken);
            }
        }

        var tenantId = _tenantContext.TenantId;
        var perm = hasManage ? Permissions.ChildManage : Permissions.ChildEnrollmentManage;
        if (classroomId.HasValue)
        {
            await _authorizer.RequireClassroomAsync(classroomId.Value, perm, cancellationToken);
            if (_readinessService != null && _classroomRepository != null)
            {
                var room = await _classroomRepository.GetByIdAsync(tenantId, classroomId.Value, cancellationToken);
                if (room != null && room.BranchId != Guid.Empty && !string.Equals(room.ClassroomType, "Pool", StringComparison.OrdinalIgnoreCase) && !string.Equals(room.Code, "POOL-WAITING", StringComparison.OrdinalIgnoreCase))
                {
                    await _readinessService.EnsureClassroomReadyForEnrollmentAsync(tenantId, room.BranchId, classroomId.Value, cancellationToken);
                }
            }
        }
        else
            await _authorizer.RequireOrganizationPermissionAsync(perm, cancellationToken);

        var child = new Child
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            ClassroomId = classroomId,
            PhotoUrl = photoUrl,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _childRepository.AddAsync(child, cancellationToken);
    }

    public async Task UpdateChildAsync(Child child, CancellationToken cancellationToken = default)
    {
        var hasManage = _userContext.HasPermission(Permissions.ChildManage);
        var hasEnrollment = _userContext.HasPermission(Permissions.ChildEnrollmentManage);
        if (!hasManage && !hasEnrollment) throw new UnauthorizedAccessException("Requires child.manage or child.enrollment.manage permission.");

        // Body IDs are untrusted: scope is checked against the STORED child, not the submitted object.
        var perm = hasManage ? Permissions.ChildManage : Permissions.ChildEnrollmentManage;
        await _authorizer.RequireChildAsync(child.Id, perm, cancellationToken);
        if (child.TenantId != _tenantContext.TenantId) throw new UnauthorizedAccessException("Access denied.");
        var existingChild = await _childRepository.GetByIdAsync(_tenantContext.TenantId, child.Id, cancellationToken);
        if (existingChild != null && existingChild.ClassroomId != child.ClassroomId)
        {
            // Moving a child requires enrollment authority over the destination as well.
            if (child.ClassroomId.HasValue)
            {
                await _authorizer.RequireClassroomAsync(child.ClassroomId.Value, Permissions.ChildEnrollmentManage, cancellationToken);
                if (_readinessService != null && _classroomRepository != null)
                {
                    var room = await _classroomRepository.GetByIdAsync(_tenantContext.TenantId, child.ClassroomId.Value, cancellationToken);
                    if (room != null && room.BranchId != Guid.Empty && !string.Equals(room.ClassroomType, "Pool", StringComparison.OrdinalIgnoreCase) && !string.Equals(room.Code, "POOL-WAITING", StringComparison.OrdinalIgnoreCase))
                    {
                        await _readinessService.EnsureClassroomReadyForEnrollmentAsync(_tenantContext.TenantId, room.BranchId, child.ClassroomId.Value, cancellationToken);
                    }
                }
            }
            else
                await _authorizer.RequireOrganizationPermissionAsync(Permissions.ChildEnrollmentManage, cancellationToken);
        }
        if (existingChild != null && existingChild.ClassroomId != child.ClassroomId)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = _tenantContext.TenantId,
                Action = "ChildTransferred",
                EntityType = "Child",
                EntityId = child.Id,
                Details = new 
                { 
                    PreviousClassroomId = existingChild.ClassroomId, 
                    NewClassroomId = child.ClassroomId,
                    TransferDate = DateTimeOffset.UtcNow,
                    Reason = "Transferred via UI"
                }
            }, cancellationToken);
        }

        child.UpdatedAt = DateTimeOffset.UtcNow;
        await _childRepository.UpdateAsync(child, cancellationToken);
    }

    public async Task DeleteChildAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("child.manage")) throw new UnauthorizedAccessException("Requires child.manage permission.");
        await _authorizer.RequireChildAsync(id, Permissions.ChildEnrollmentManage, cancellationToken);
        var tenantId = _tenantContext.TenantId;
        await _childRepository.DeleteAsync(tenantId, id, cancellationToken);
    }

    public async Task SyncParentGuardianAsync(Guid childId, string parentEmail, string parentName, string relationship, string phone, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        await _childRepository.SyncParentGuardianAsync(tenantId, childId, parentEmail, parentName, relationship, phone, cancellationToken);
    }

    public async Task<IEnumerable<ChildGuardianDto>> GetChildGuardiansAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _childRepository.GetChildGuardiansAsync(tenantId, childId, cancellationToken);
    }

    public async Task AddOrUpdateGuardianAsync(Guid childId, string parentEmail, string parentName, string relationship, string phone, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        await _childRepository.SyncParentGuardianAsync(tenantId, childId, parentEmail, parentName, relationship, phone, cancellationToken);
    }

    public async Task RevokeGuardianAccessAsync(Guid childId, Guid userId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var revokedBy = _userContext.UserId;
        await _childRepository.RevokeChildGuardianAsync(tenantId, childId, userId, revokedBy, cancellationToken);
    }
}

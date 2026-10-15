using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class BranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    public BranchService(IBranchRepository branchRepository, ITenantContext tenantContext, IUserContext userContext)
    {
        _branchRepository = branchRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
    }

    public async Task<IEnumerable<Branch>> GetBranchesAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (_userContext.OrganizationId.HasValue && _userContext.OrganizationId != organizationId) 
            throw new UnauthorizedAccessException("Cannot access branches outside of active organization.");
        var tenantId = _tenantContext.TenantId;
        return await _branchRepository.GetByOrganizationIdAsync(tenantId, organizationId, cancellationToken);
    }

    public async Task<Branch?> GetBranchByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var branch = await _branchRepository.GetByIdAsync(tenantId, id, cancellationToken);
        if (branch != null && _userContext.OrganizationId.HasValue && branch.OrganizationId != _userContext.OrganizationId)
            throw new UnauthorizedAccessException("Cannot access branches outside of active organization.");
        return branch;
    }

    public async Task<Branch> CreateBranchAsync(Guid organizationId, string name, string? location, CancellationToken cancellationToken = default)
    {
        if (_userContext.OrganizationId.HasValue && _userContext.OrganizationId != organizationId) 
            throw new UnauthorizedAccessException("Cannot access branches outside of active organization.");
        if (!_userContext.HasPermission("branch.manage")) throw new UnauthorizedAccessException("Requires branch.manage permission.");
        var tenantId = _tenantContext.TenantId;
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = organizationId,
            Name = name,
            Location = location,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _branchRepository.AddAsync(branch, cancellationToken);
    }

    public async Task UpdateBranchAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("branch.manage")) throw new UnauthorizedAccessException("Requires branch.manage permission.");
        if (_userContext.OrganizationId.HasValue && _userContext.OrganizationId != branch.OrganizationId) 
            throw new UnauthorizedAccessException("Cannot access branches outside of active organization.");
        var tenantId = _tenantContext.TenantId;
        if (branch.TenantId != tenantId)
            throw new UnauthorizedAccessException("Cross-tenant update attempted.");

        branch.UpdatedAt = DateTimeOffset.UtcNow;
        await _branchRepository.UpdateAsync(branch, cancellationToken);
    }
}

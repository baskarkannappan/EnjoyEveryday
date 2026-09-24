using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class BranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly ITenantContext _tenantContext;

    public BranchService(IBranchRepository branchRepository, ITenantContext tenantContext)
    {
        _branchRepository = branchRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Branch>> GetBranchesAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _branchRepository.GetByOrganizationIdAsync(tenantId, organizationId, cancellationToken);
    }

    public async Task<Branch?> GetBranchByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _branchRepository.GetByIdAsync(tenantId, id, cancellationToken);
    }

    public async Task<Branch> CreateBranchAsync(Guid organizationId, string name, string? location, CancellationToken cancellationToken = default)
    {
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
        var tenantId = _tenantContext.TenantId;
        if (branch.TenantId != tenantId)
            throw new UnauthorizedAccessException("Cross-tenant update attempted.");

        branch.UpdatedAt = DateTimeOffset.UtcNow;
        await _branchRepository.UpdateAsync(branch, cancellationToken);
    }
}

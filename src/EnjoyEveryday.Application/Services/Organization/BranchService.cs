using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class BranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IPlanQuotaService? _quotaService;

    // Short-lived scoped cache to avoid duplicate branch lookups within a circuit/request
    private (Guid TenantId, Guid OrgId, DateTimeOffset ExpiresAt, IReadOnlyList<Branch> Items)? _orgBranchesCache;
    private readonly Dictionary<Guid, (Guid TenantId, DateTimeOffset ExpiresAt, Branch? Branch)> _branchByIdCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public BranchService(
        IBranchRepository branchRepository, 
        ITenantContext tenantContext, 
        IUserContext userContext,
        IPlanQuotaService? quotaService = null)
    {
        _branchRepository = branchRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _quotaService = quotaService;
    }

    public async Task<IEnumerable<Branch>> GetBranchesAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (_userContext.OrganizationId.HasValue && _userContext.OrganizationId != organizationId) 
            throw new UnauthorizedAccessException("Cannot access branches outside of active organization.");
        var tenantId = _tenantContext.TenantId;

        var cached = _orgBranchesCache;
        if (cached.HasValue && cached.Value.TenantId == tenantId && cached.Value.OrgId == organizationId && DateTimeOffset.UtcNow < cached.Value.ExpiresAt)
        {
            return cached.Value.Items;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            cached = _orgBranchesCache;
            if (cached.HasValue && cached.Value.TenantId == tenantId && cached.Value.OrgId == organizationId && DateTimeOffset.UtcNow < cached.Value.ExpiresAt)
            {
                return cached.Value.Items;
            }

            var items = (await _branchRepository.GetByOrganizationIdAsync(tenantId, organizationId, cancellationToken)).ToList();
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(10);
            _orgBranchesCache = (tenantId, organizationId, expiresAt, items);
            foreach (var b in items)
            {
                _branchByIdCache[b.Id] = (tenantId, expiresAt, b);
            }
            return items;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<Branch?> GetBranchByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        Branch? branch = null;
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_branchByIdCache.TryGetValue(id, out var entry) && entry.TenantId == tenantId && DateTimeOffset.UtcNow < entry.ExpiresAt)
            {
                branch = entry.Branch;
            }
            else
            {
                branch = await _branchRepository.GetByIdAsync(tenantId, id, cancellationToken);
                _branchByIdCache[id] = (tenantId, DateTimeOffset.UtcNow.AddSeconds(10), branch);
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        if (branch != null && _userContext.OrganizationId.HasValue && branch.OrganizationId != _userContext.OrganizationId)
            throw new UnauthorizedAccessException("Cannot access branches outside of active organization.");
        return branch;
    }

    public async Task<Branch> CreateBranchAsync(Guid organizationId, string name, string? location, CancellationToken cancellationToken = default)
    {
        if (_userContext.OrganizationId.HasValue && _userContext.OrganizationId != organizationId) 
            throw new UnauthorizedAccessException("Cannot access branches outside of active organization.");
        if (!_userContext.HasPermission("branch.manage")) throw new UnauthorizedAccessException("Requires branch.manage permission.");

        if (_quotaService != null)
        {
            await _quotaService.ValidateCanAddBranchAsync(organizationId, cancellationToken);
        }

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

        var created = await _branchRepository.AddAsync(branch, cancellationToken);
        _orgBranchesCache = null;
        _branchByIdCache.Remove(created.Id);
        return created;
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
        _orgBranchesCache = null;
        _branchByIdCache.Remove(branch.Id);
    }
}

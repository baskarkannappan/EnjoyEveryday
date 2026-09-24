using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class OrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITenantContext _tenantContext;

    public OrganizationService(IOrganizationRepository organizationRepository, ITenantContext tenantContext)
    {
        _organizationRepository = organizationRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Organization>> GetOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _organizationRepository.GetAllAsync(tenantId, cancellationToken);
    }

    public async Task<Organization?> GetOrganizationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _organizationRepository.GetByIdAsync(tenantId, id, cancellationToken);
    }

    public async Task<Organization> CreateOrganizationAsync(string name, string? location, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Location = location,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _organizationRepository.AddAsync(organization, cancellationToken);
    }

    public async Task UpdateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        if (organization.TenantId != tenantId)
            throw new UnauthorizedAccessException("Cross-tenant update attempted.");

        organization.UpdatedAt = DateTimeOffset.UtcNow;
        await _organizationRepository.UpdateAsync(organization, cancellationToken);
    }
}

using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Branch>> GetByOrganizationIdAsync(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<Branch> AddAsync(Branch branch, CancellationToken cancellationToken = default);
    Task UpdateAsync(Branch branch, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}

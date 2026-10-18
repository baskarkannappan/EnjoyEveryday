using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Domain.Repositories;

public class PlatformDbOrganizationRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int BranchCount { get; set; }
}

public interface IPlatformAdminRepository
{
    Task<List<PlatformDbOrganizationRecord>> GetOrganizationsOverviewAsync(CancellationToken ct = default);
    Task ProvisionOrganizationAsync(Guid id, string name, string location, string adminEmail, CancellationToken ct = default);
}

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
    public string? AdminEmail { get; set; }
}

public class PlatformDbBranchRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string EffectiveLocation { get; set; } = string.Empty;
    public int ClassroomsCount { get; set; }
    public int ChildrenCount { get; set; }
}

public class PlatformDbAdminUserRecord
{
    public Guid UserId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string RoleName { get; set; } = "Tenant Admin";
    public string AccountStatus { get; set; } = "Active";
}

public interface IPlatformAdminRepository
{
    Task<List<PlatformDbOrganizationRecord>> GetOrganizationsOverviewAsync(CancellationToken ct = default);
    Task ProvisionOrganizationAsync(Guid id, string name, string location, string adminEmail, int branchCount = 1, CancellationToken ct = default);
    Task UpdateOrganizationDetailsAsync(Guid id, string name, string? location, string adminEmail, int branchCount, CancellationToken ct = default);
    Task<List<PlatformDbBranchRecord>> GetAllBranchesAsync(CancellationToken ct = default);
    Task<PlatformDbBranchRecord> AddBranchAsync(Guid organizationId, string name, string? location, CancellationToken ct = default);
    Task<List<PlatformDbAdminUserRecord>> GetOrganizationAdminsAsync(CancellationToken ct = default);
}

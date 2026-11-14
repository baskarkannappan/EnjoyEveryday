using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public class PlatformDbOrganizationRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public string AccountStatus { get; set; } = "Active";
    public string? SuspensionReason { get; set; }
    public Guid? PlanId { get; set; }
    public string PlanCode { get; set; } = "PLAN-BLOOM";
    public string PlanName { get; set; } = "Bloom (Growth)";
    public int MaxBranches { get; set; } = 2;
    public int MaxClassrooms { get; set; } = 8;
    public int MaxChildren { get; set; } = 90;
    public bool IncludesAi { get; set; } = true;
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
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class PlatformOrganizationInvitationRecord
{
    public Guid OrganizationId { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
}

public class PlatformOrganizationInvitationValidationResult
{
    public bool Valid { get; set; }
    public Guid? InvitationId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? TenantId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public interface IPlatformAdminRepository
{
    Task<List<PlatformDbOrganizationRecord>> GetOrganizationsOverviewAsync(CancellationToken ct = default);
    Task ProvisionOrganizationAsync(Guid id, string name, string location, string adminEmail, int branchCount = 1, CancellationToken ct = default);
    Task UpdateOrganizationDetailsAsync(Guid id, string name, string? location, string adminEmail, int branchCount, CancellationToken ct = default);
    Task<List<PlatformDbBranchRecord>> GetAllBranchesAsync(CancellationToken ct = default);
    Task<PlatformDbBranchRecord> AddBranchAsync(Guid organizationId, string name, string? location, CancellationToken ct = default);
    Task<List<PlatformDbAdminUserRecord>> GetOrganizationAdminsAsync(CancellationToken ct = default);
    Task<int> GetPlatformAdminCountAsync(CancellationToken ct = default);
    Task CreateAdminUserAsync(string email, string firstName, string lastName, string password, string roleName, Guid? orgId, CancellationToken ct = default);
    Task UpdateAdminUserAsync(Guid userId, string email, string firstName, string lastName, string? password, string roleName, string accountStatus, Guid? orgId, CancellationToken ct = default);
    Task DeleteAdminUserAsync(Guid userId, CancellationToken ct = default);

    Task<PlatformOrganizationInvitationRecord?> CreateOrganizationInvitationAsync(Guid organizationId, string adminEmail, string token, int expiresDays = 7, CancellationToken ct = default);
    Task<List<PlatformOrganizationInvitationRecord>> GetLatestInvitationsAsync(CancellationToken ct = default);
    Task<PlatformOrganizationInvitationValidationResult?> ValidateInvitationAsync(string token, CancellationToken ct = default);
    Task<bool> AcceptInvitationAsync(string token, Guid userId, CancellationToken ct = default);
    Task UpdateOrganizationAccountStatusAsync(Guid organizationId, string status, string? reason, CancellationToken ct = default);
    Task UpdateOrganizationPlanQuotaAsync(Guid organizationId, Guid planId, string planCode, string planName, int maxBranches, int maxClassrooms, int maxChildren, bool includesAi, CancellationToken ct = default);
    Task<OrganizationQuotaUsage?> GetOrganizationQuotaUsageAsync(Guid organizationId, CancellationToken ct = default);
}

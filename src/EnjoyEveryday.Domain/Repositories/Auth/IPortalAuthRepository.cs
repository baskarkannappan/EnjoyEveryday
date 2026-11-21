namespace EnjoyEveryday.Domain.Repositories;

public sealed class PortalAuthSyncResult
{
    public string? OutStatus { get; set; }
    public Guid? OutUserId { get; set; }
    public Guid? OutTenantId { get; set; }
    public Guid? OutEffectiveTenantId { get; set; }
    public Guid? OutActiveOrgId { get; set; }
    public string? OutEmail { get; set; }
    public string? OutDisplayName { get; set; }
}

public sealed class PortalOrgValidationResult
{
    public bool IsValid { get; set; }
    public Guid? TenantId { get; set; }
}

public sealed class PortalAuthClaimRow
{
    public string? ClaimType { get; set; }
    public string? ClaimValue { get; set; }
}

public sealed class PortalInvitationValidationResult
{
    public bool Valid { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? TenantId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
}

public interface IPortalAuthRepository
{
    Task<PortalAuthSyncResult?> DevLoginAsync(string? email, CancellationToken ct = default);

    Task<PortalAuthSyncResult?> SyncExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string firstName,
        string lastName,
        string displayName,
        string portal,
        CancellationToken ct = default);

    Task<IReadOnlyList<PortalAuthClaimRow>> GetUserAuthClaimsAsync(
        Guid userId,
        Guid? activeOrgId,
        CancellationToken ct = default);

    Task<PortalOrgValidationResult?> ValidateAndSetOrganizationAsync(
        Guid userId,
        Guid? organizationId,
        CancellationToken ct = default);

    Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken ct = default);

    Task<PortalInvitationValidationResult?> ValidateOrganizationInvitationAsync(
        string token,
        CancellationToken ct = default);

    Task AcceptOrganizationInvitationAsync(
        string token,
        Guid userId,
        CancellationToken ct = default);

    Task<(Guid Id, string Name)?> GetActiveBranchInOrganizationAsync(
        Guid branchId,
        Guid organizationId,
        CancellationToken ct = default);

    Task SeedDevParentEnvironmentAsync(
        Guid userId,
        Guid tenantId,
        string email,
        Guid childId,
        string securityStamp,
        CancellationToken ct = default);

    Task<(Guid? UserId, Guid TenantId, bool IsActive)> SyncParentExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string firstName,
        string lastName,
        string displayName,
        Guid defaultTenantId,
        Guid defaultOrgId,
        CancellationToken ct = default);

    Task<bool> SyncParentRelationshipsAndCheckOnboardedAsync(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        Guid tenantId,
        CancellationToken ct = default);
}

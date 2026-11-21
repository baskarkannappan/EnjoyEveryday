namespace EnjoyEveryday.Domain.Repositories;

public sealed record OrganizationRolePermissionOverride(string RoleName, string FeatureKey, bool IsAllowed);

public interface IOrganizationPermissionRepository
{
    Task<IReadOnlyList<OrganizationRolePermissionOverride>> GetOverridesAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken ct = default);

    Task UpsertOverrideAsync(
        Guid tenantId,
        Guid organizationId,
        string roleName,
        string featureKey,
        bool isAllowed,
        CancellationToken ct = default);

    Task ResetOverridesAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken ct = default);
}

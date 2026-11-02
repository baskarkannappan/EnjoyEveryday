using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Shared.Authorization;

public sealed class FeatureDefinition
{
    public string Key { get; init; } = string.Empty;
    public string FeatureKey => Key;
    public string FeatureType => Routes.Any() ? "Page" : "Action";
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public IReadOnlyList<string> Routes { get; init; } = Array.Empty<string>();
    public HashSet<string> DefaultAllowedRoles { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface IOrganizationPermissionService
{
    IReadOnlyList<FeatureDefinition> GetAllFeatures();
    IReadOnlyList<string> GetConfigurableRoles();
    string GetRoleDisplayName(string roleName);
    Task<Dictionary<(string RoleName, string FeatureKey), bool>> GetPermissionsMatrixAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default);
    Task SavePermissionsMatrixAsync(Guid tenantId, Guid organizationId, Dictionary<(string RoleName, string FeatureKey), bool> matrix, CancellationToken ct = default);
    Task ResetPermissionsMatrixAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default);
    Task<bool> IsFeatureAllowedAsync(Guid tenantId, Guid organizationId, string roleName, string featureKey, CancellationToken ct = default);
    Task<bool?> CheckRouteCustomAllowedAsync(Guid tenantId, Guid organizationId, string roleName, string relativePath, CancellationToken ct = default);
}

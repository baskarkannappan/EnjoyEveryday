using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolePermissionsController : ControllerBase
{
    private readonly IOrganizationPermissionService _permService;
    private readonly ITenantContext _tenantContext;

    public RolePermissionsController(IOrganizationPermissionService permService, ITenantContext tenantContext)
    {
        _permService = permService;
        _tenantContext = tenantContext;
    }

    [HttpGet("organizations/{organizationId}")]
    public async Task<ActionResult<Dictionary<string, List<string>>>> GetPermissions(
        Guid organizationId,
        CancellationToken ct)
    {
        var rawMatrix = await _permService.GetPermissionsMatrixAsync(_tenantContext.TenantId, organizationId, ct);
        var result = new Dictionary<string, List<string>>();
        foreach (var kv in rawMatrix)
        {
            if (kv.Value)
            {
                if (!result.TryGetValue(kv.Key.RoleName, out var list))
                {
                    list = new List<string>();
                    result[kv.Key.RoleName] = list;
                }
                list.Add(kv.Key.FeatureKey);
            }
        }
        return Ok(result);
    }

    [HttpPost("organizations/{organizationId}")]
    public async Task<ActionResult> SavePermissions(
        Guid organizationId,
        [FromBody] Dictionary<string, List<string>> matrix,
        CancellationToken ct)
    {
        var allFeatures = _permService.GetAllFeatures();
        var dict = new Dictionary<(string RoleName, string FeatureKey), bool>();
        foreach (var (role, allowedKeys) in matrix)
        {
            var allowedSet = new HashSet<string>(allowedKeys, StringComparer.OrdinalIgnoreCase);
            foreach (var feat in allFeatures)
            {
                dict[(role, feat.Key)] = allowedSet.Contains(feat.Key);
            }
        }

        await _permService.SavePermissionsMatrixAsync(_tenantContext.TenantId, organizationId, dict, ct);
        return NoContent();
    }

    [HttpPost("organizations/{organizationId}/reset")]
    public async Task<ActionResult> ResetPermissions(
        Guid organizationId,
        CancellationToken ct)
    {
        await _permService.ResetPermissionsMatrixAsync(_tenantContext.TenantId, organizationId, ct);
        return NoContent();
    }

    [HttpGet("features")]
    public ActionResult<IReadOnlyList<FeatureDefinition>> GetAllFeatures()
    {
        var features = _permService.GetAllFeatures();
        return Ok(features);
    }
}

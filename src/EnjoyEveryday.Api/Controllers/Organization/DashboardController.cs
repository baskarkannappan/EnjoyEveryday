using EnjoyEveryday.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("organizations/{organizationId}/metrics")]
    public async Task<ActionResult<OrganizationMetrics>> GetOrganizationMetrics(Guid organizationId, CancellationToken cancellationToken)
    {
        return Ok(await _dashboardService.GetOrganizationMetricsAsync(organizationId, cancellationToken));
    }

    [HttpGet("organizations/{organizationId}/branches/metrics")]
    public async Task<ActionResult<IEnumerable<BranchMetrics>>> GetBranchMetrics(Guid organizationId, CancellationToken cancellationToken)
    {
        return Ok(await _dashboardService.GetBranchMetricsAsync(organizationId, cancellationToken));
    }
}

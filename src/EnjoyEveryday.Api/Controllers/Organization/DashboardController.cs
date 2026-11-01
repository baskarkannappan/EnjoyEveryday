using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;
    private readonly IDaycareOperationsBoardService _operationsBoardService;

    public DashboardController(DashboardService dashboardService, IDaycareOperationsBoardService operationsBoardService)
    {
        _dashboardService = dashboardService;
        _operationsBoardService = operationsBoardService;
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

    [HttpGet("/api/v1/dashboards/org-admin")]
    public async Task<ActionResult<OperationsBoardModel>> GetOrgAdminDashboard([FromQuery] Guid? branchId, [FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var filter = new OperationsBoardFilter
        {
            BranchId = branchId,
            Date = date ?? DateTime.Today,
            RoleOverride = EnjoyEveryday.Shared.Authorization.RoleTemplates.OrganizationAdmin
        };
        var board = await _operationsBoardService.GetOperationsBoardAsync(filter, cancellationToken);
        return Ok(board);
    }

    [HttpGet("/api/v1/dashboards/branch-admin")]
    public async Task<ActionResult<OperationsBoardModel>> GetBranchAdminDashboard([FromQuery] Guid? branchId, [FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var filter = new OperationsBoardFilter
        {
            BranchId = branchId,
            Date = date ?? DateTime.Today,
            RoleOverride = EnjoyEveryday.Shared.Authorization.RoleTemplates.BranchAdmin
        };
        var board = await _operationsBoardService.GetOperationsBoardAsync(filter, cancellationToken);
        return Ok(board);
    }
}

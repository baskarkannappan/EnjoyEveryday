using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IntelligenceController : ControllerBase
{
    private readonly IOrganizationIntelligenceService _intelService;
    private readonly IBranchBenchmarkingService _benchmarkingService;

    public IntelligenceController(
        IOrganizationIntelligenceService intelService,
        IBranchBenchmarkingService benchmarkingService)
    {
        _intelService = intelService;
        _benchmarkingService = benchmarkingService;
    }

    [HttpGet("report")]
    public async Task<ActionResult<OrganizationIntelligenceReport>> GetReport(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? classroomId,
        [FromQuery] string? period,
        [FromQuery] string? category,
        CancellationToken ct)
    {
        var filter = new OrganizationIntelligenceFilter
        {
            OrganizationId = organizationId,
            BranchId = branchId,
            ClassroomId = classroomId,
            ReportingPeriod = period ?? IntelligenceReportingPeriod.Last7Days,
            Category = category
        };

        var report = await _intelService.GetIntelligenceReportAsync(filter, ct);
        return Ok(report);
    }

    [HttpPost("generate-insights")]
    public async Task<ActionResult<OrganizationIntelligenceReport>> GenerateInsights(
        [FromBody] OrganizationIntelligenceFilter filter,
        CancellationToken ct)
    {
        var report = await _intelService.GenerateInsightsAsync(filter, ct);
        return Ok(report);
    }

    [HttpGet("benchmarking/{organizationId}")]
    public async Task<ActionResult<List<BranchHealthScorecard>>> GetBranchScorecards(
        Guid organizationId,
        CancellationToken ct)
    {
        var scorecards = await _benchmarkingService.GetBranchScorecardsAsync(organizationId, ct);
        return Ok(scorecards);
    }

    [HttpGet("goals")]
    public async Task<ActionResult<List<BranchOperationalGoal>>> GetGoals(CancellationToken ct)
    {
        var goals = await _benchmarkingService.GetOperationalGoalsAsync(ct: ct);
        return Ok(goals);
    }
}

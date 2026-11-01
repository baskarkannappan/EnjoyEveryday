using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StaffRosterController : ControllerBase
{
    private readonly IStaffRosterService _rosterService;

    public StaffRosterController(IStaffRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<StaffRosterOverview>> GetRosterOverview(
        [FromQuery] Guid branchId,
        [FromQuery] DateTime? date,
        CancellationToken ct)
    {
        var targetDate = date ?? DateTime.Today;
        var overview = await _rosterService.GetRosterOverviewAsync(branchId, targetDate, ct);
        return Ok(overview);
    }

    [HttpPost("leaves")]
    public async Task<ActionResult<Guid>> SubmitStaffLeave([FromBody] StaffLeave leave, CancellationToken ct)
    {
        var leaveId = await _rosterService.SubmitStaffLeaveAsync(leave, ct);
        return Ok(leaveId);
    }

    [HttpPost("substitute")]
    public async Task<ActionResult> AssignSubstituteCoverage([FromBody] AssignSubstituteRequest req, CancellationToken ct)
    {
        await _rosterService.AssignSubstituteCoverageAsync(req.LeaveId, req.SubstituteTeacherId, ct);
        return NoContent();
    }

    [HttpPost("dispatch-float")]
    public async Task<ActionResult> DispatchFloatTeacher([FromBody] DispatchFloatRequest req, CancellationToken ct)
    {
        await _rosterService.DispatchFloatTeacherAsync(req.TeacherId, req.ClassroomId, ct);
        return NoContent();
    }

    [HttpGet("coverage-needs")]
    public async Task<ActionResult<List<ActivityCoverageNeedRecord>>> GetCoverageNeeds(
        [FromQuery] Guid branchId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        var needs = await _rosterService.GetActivityCoverageNeedsAsync(branchId, from, to, ct);
        return Ok(needs);
    }

    [HttpGet("cover-candidates")]
    public async Task<ActionResult<List<CoverCandidateRecord>>> GetCoverCandidates(
        [FromQuery] Guid branchId,
        [FromQuery] Guid eventId,
        CancellationToken ct)
    {
        var candidates = await _rosterService.GetCoverCandidatesAsync(branchId, eventId, ct);
        return Ok(candidates);
    }

    [HttpPost("reassign-activity")]
    public async Task<ActionResult> ReassignActivity([FromBody] ReassignActivityRequest req, CancellationToken ct)
    {
        await _rosterService.ReassignActivityAsync(req.BranchId, req.EventId, req.NewTeacherUserId, ct);
        return NoContent();
    }
}

public record AssignSubstituteRequest(Guid LeaveId, Guid SubstituteTeacherId);
public record DispatchFloatRequest(Guid TeacherId, Guid ClassroomId);
public record ReassignActivityRequest(Guid BranchId, Guid EventId, Guid NewTeacherUserId);

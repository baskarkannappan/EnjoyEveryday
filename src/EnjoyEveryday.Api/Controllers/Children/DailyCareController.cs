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
public class DailyCareController : ControllerBase
{
    private readonly IChildDailyCareService _dailyCareService;

    public DailyCareController(IChildDailyCareService dailyCareService)
    {
        _dailyCareService = dailyCareService;
    }

    [HttpGet("classroom-feed")]
    public async Task<ActionResult<ClassroomDailyCareFeedDto>> GetClassroomFeed(
        [FromQuery] Guid branchId,
        [FromQuery] Guid? classroomId,
        [FromQuery] DateOnly? date,
        CancellationToken ct)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var feed = await _dailyCareService.GetClassroomDailyCareFeedAsync(branchId, classroomId, targetDate, ct);
        return Ok(feed);
    }

    [HttpGet("children/{childId}/package")]
    public async Task<ActionResult<ChildCarePackageDto>> GetChildCarePackage(
        Guid childId,
        [FromQuery] DateOnly? date,
        CancellationToken ct)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var package = await _dailyCareService.GetChildCarePackageAsync(childId, targetDate, ct);
        return Ok(package);
    }

    [HttpPost("events")]
    public async Task<ActionResult<Guid>> RecordCareEvent([FromBody] ChildCareEvent ev, CancellationToken ct)
    {
        var eventId = await _dailyCareService.RecordCareEventAsync(ev, ct);
        return Ok(eventId);
    }

    [HttpPost("events/batch")]
    public async Task<ActionResult<int>> RecordBatchCareEvents([FromBody] RecordBatchCareEventsRequest req, CancellationToken ct)
    {
        var count = await _dailyCareService.RecordBatchCareEventsAsync(
            req.ChildIds,
            req.EventType,
            req.DetailOption,
            req.Notes,
            req.ParentVisible,
            ct);
        return Ok(count);
    }

    [HttpPost("naps/batch-start")]
    public async Task<ActionResult<int>> StartBatchNap([FromBody] BatchNapRequest req, CancellationToken ct)
    {
        var count = await _dailyCareService.StartBatchNapAsync(req.ChildIds, req.ClassroomId, req.Notes, ct);
        return Ok(count);
    }

    [HttpPost("naps/batch-wake")]
    public async Task<ActionResult<int>> WakeBatchChildren([FromBody] BatchNapRequest req, CancellationToken ct)
    {
        var count = await _dailyCareService.WakeBatchChildrenAsync(req.ChildIds, req.Outcome, req.Notes, ct);
        return Ok(count);
    }

    [HttpPost("handovers")]
    public async Task<ActionResult<Guid>> SubmitHandover([FromBody] TeacherHandover handover, CancellationToken ct)
    {
        var handoverId = await _dailyCareService.SubmitHandoverAsync(handover, ct);
        return Ok(handoverId);
    }

    [HttpPost("handovers/{id}/acknowledge")]
    public async Task<ActionResult> AcknowledgeHandover(Guid id, [FromBody] AcknowledgeHandoverRequest req, CancellationToken ct)
    {
        await _dailyCareService.AcknowledgeHandoverAsync(id, req.Notes, ct);
        return NoContent();
    }

    [HttpPost("supplies/status")]
    public async Task<ActionResult<Guid>> UpdateSupplyStatus([FromBody] ChildCareSupply supply, CancellationToken ct)
    {
        var supplyId = await _dailyCareService.UpdateSupplyStatusAsync(supply, ct);
        return Ok(supplyId);
    }

    [HttpPost("care-plan")]
    public async Task<ActionResult<Guid>> SaveCarePlan([FromBody] ChildCarePlan plan, CancellationToken ct)
    {
        var planId = await _dailyCareService.SaveCarePlanAsync(plan, ct);
        return Ok(planId);
    }

    [HttpGet("children/{childId}/parent-report")]
    public async Task<ActionResult<ParentDailyCareReport>> GetParentReport(Guid childId, [FromQuery] DateOnly? date, CancellationToken ct)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var report = await _dailyCareService.GetParentDailyCareReportAsync(childId, targetDate, ct);
        return Ok(report);
    }
}

public class AcknowledgeHandoverRequest
{
    public string? Notes { get; set; }
}

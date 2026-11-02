using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Attendance;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers.Organization;

public class ExecuteEndOfDayRequest
{
    public Guid? BranchId { get; set; }
    public DateTime? Date { get; set; }
    public TimeSpan? CheckoutTime { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly AttendanceService _attendanceService;
    private readonly ISecurePickupService _pickupService;
    private readonly IEndOfDayService _eodService;
    private readonly ISystemLookupCatalog _lookupCatalog;
    private readonly IUserContext _userContext;

    public AttendanceController(
        AttendanceService attendanceService,
        ISecurePickupService pickupService,
        IEndOfDayService eodService,
        ISystemLookupCatalog lookupCatalog,
        IUserContext userContext)
    {
        _attendanceService = attendanceService;
        _pickupService = pickupService;
        _eodService = eodService;
        _lookupCatalog = lookupCatalog;
        _userContext = userContext;
    }

    [HttpGet("branch-roster")]
    public async Task<ActionResult<BranchAttendanceRosterDto>> GetBranchRoster([FromQuery] Guid branchId, [FromQuery] DateTime? date, CancellationToken ct)
    {
        var targetDate = date ?? DateTime.Today;
        var roster = await _attendanceService.GetBranchAttendanceRosterAsync(branchId, targetDate, ct);
        return Ok(roster);
    }

    [HttpPost("bulk-mark")]
    public async Task<ActionResult> BulkMarkAttendance([FromBody] BulkMarkAttendanceRequest request, CancellationToken ct)
    {
        await _attendanceService.BulkMarkAttendanceAsync(request, _userContext.UserId, ct);
        return Ok(new { success = true });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AttendanceSummary>> GetSummary([FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.Today;
        var summary = await _attendanceService.GetOrganizationSummaryAsync(targetDate);
        return Ok(summary);
    }

    [HttpGet("needs-attention")]
    public async Task<ActionResult<IEnumerable<AttendanceAttentionAlertDto>>> GetNeedsAttention([FromQuery] DateTime? date, CancellationToken ct)
    {
        var targetDate = date ?? DateTime.Today;
        var alerts = await _attendanceService.GetAttendanceNeedsAttentionAsync(targetDate, ct);
        return Ok(alerts);
    }

    [HttpGet("classroom-summaries")]
    public async Task<ActionResult<IEnumerable<ClassroomAttendanceSummaryDto>>> GetClassroomSummaries([FromQuery] DateTime? date, CancellationToken ct)
    {
        var targetDate = date ?? DateTime.Today;
        var summaries = await _attendanceService.GetClassroomAttendanceSummariesAsync(targetDate, null, null, ct);
        return Ok(summaries);
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<IReadOnlyList<MasterLookupItem>>> GetAttendanceStatuses(CancellationToken ct)
    {
        var lookups = await _lookupCatalog.GetLookupsByCategoryAsync("AttendanceStatus", ct);
        return Ok(lookups);
    }

    [HttpGet("pickup-profile/{childId:guid}")]
    public async Task<ActionResult<ChildPickupSecurityProfile>> GetPickupProfile(Guid childId, [FromQuery] DateTime? date, CancellationToken ct)
    {
        var profile = await _pickupService.GetChildPickupSecurityProfileAsync(childId, date, ct);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [HttpPost("departure")]
    public async Task<ActionResult<RecordChildDepartureResponse>> RecordDeparture([FromBody] RecordChildDepartureRequest request, CancellationToken ct)
    {
        var result = await _pickupService.VerifyAndRecordChildDepartureAsync(request, ct);
        if (!result.Success)
        {
            return BadRequest(new { success = false, error = result.ErrorMessage });
        }
        return Ok(result);
    }

    [HttpGet("eod/pending-summary")]
    public async Task<ActionResult<EndOfDayPendingSummary>> GetEndOfDayPending([FromQuery] Guid? branchId, [FromQuery] DateTime? date, CancellationToken ct)
    {
        var summary = await _eodService.GetPendingSummaryAsync(branchId, date, ct);
        return Ok(summary);
    }

    [HttpPost("eod/execute")]
    public async Task<ActionResult<EndOfDayProcessReport>> ExecuteEndOfDay([FromBody] ExecuteEndOfDayRequest request, CancellationToken ct)
    {
        var report = await _eodService.ExecuteEndOfDayProcessAsync(request.BranchId, request.Date, request.CheckoutTime, ct);
        return Ok(report);
    }

    [HttpPost("child")]
    public async Task<ActionResult> MarkChildAttendance([FromBody] ChildAttendance attendance)
    {
        if (attendance.RecordedByUserId == Guid.Empty)
        {
            attendance.RecordedByUserId = _userContext.UserId;
        }
        await _attendanceService.MarkChildAttendanceAsync(attendance);
        return Ok(attendance);
    }

    [HttpPost("teacher")]
    public async Task<ActionResult> MarkTeacherAttendance([FromBody] TeacherAttendance attendance)
    {
        if (attendance.RecordedByUserId == Guid.Empty)
        {
            attendance.RecordedByUserId = _userContext.UserId;
        }
        await _attendanceService.MarkTeacherAttendanceAsync(attendance);
        return Ok(attendance);
    }
}

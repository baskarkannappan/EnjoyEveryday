using System;
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

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly AttendanceService _attendanceService;
    private readonly IUserContext _userContext;

    public AttendanceController(AttendanceService attendanceService, IUserContext userContext)
    {
        _attendanceService = attendanceService;
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlannerController : ControllerBase
{
    private readonly PlannerService _plannerService;

    public PlannerController(PlannerService plannerService)
    {
        _plannerService = plannerService;
    }

    [HttpGet("classroom/{classroomId}")]
    public async Task<ActionResult<IEnumerable<ExperienceSchedule>>> GetMonthlySchedule(Guid classroomId, [FromQuery] int year, [FromQuery] int month)
    {
        var schedules = await _plannerService.GetMonthlyScheduleAsync(classroomId, year, month);
        return Ok(schedules);
    }

    public class ScheduleRequest
    {
        public Guid ExperienceId { get; set; }
        public Guid ClassroomId { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public string TimeOfDay { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<ActionResult<ExperienceSchedule>> ScheduleExperience([FromBody] ScheduleRequest request)
    {
        var schedule = await _plannerService.ScheduleExperienceAsync(request.ExperienceId, request.ClassroomId, request.ScheduledDate, request.TimeOfDay);
        return Ok(schedule);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSchedule(Guid id)
    {
        await _plannerService.DeleteScheduleAsync(id);
        return NoContent();
    }
}

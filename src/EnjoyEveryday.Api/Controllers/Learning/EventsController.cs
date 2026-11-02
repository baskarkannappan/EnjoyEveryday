using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly EventService _eventService;

    public EventsController(EventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {
        var events = await _eventService.GetEventsAsync();
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Event>> GetEventById(Guid id)
    {
        var evt = await _eventService.GetEventByIdAsync(id);
        if (evt == null)
        {
            return NotFound();
        }
        return Ok(evt);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateEvent([FromBody] Event evt)
    {
        var id = await _eventService.CreateEventAsync(evt);
        return CreatedAtAction(nameof(GetEventById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateEvent(Guid id, [FromBody] Event evt)
    {
        if (id != evt.Id)
        {
            return BadRequest("ID mismatch.");
        }
        await _eventService.UpdateEventAsync(evt);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteEvent(Guid id)
    {
        await _eventService.DeleteEventAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/classrooms")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetEventClassrooms(Guid id)
    {
        var classrooms = await _eventService.GetEventClassroomsAsync(id);
        return Ok(classrooms);
    }

    [HttpPost("{id:guid}/classrooms")]
    public async Task<ActionResult> AddClassroomToEvent(Guid id, [FromBody] AddClassroomEventRequest req)
    {
        await _eventService.AddClassroomToEventAsync(id, req.ClassroomId);
        return NoContent();
    }

    [HttpDelete("{id:guid}/classrooms/{classroomId:guid}")]
    public async Task<ActionResult> RemoveClassroomFromEvent(Guid id, Guid classroomId)
    {
        await _eventService.RemoveClassroomFromEventAsync(id, classroomId);
        return NoContent();
    }
}

public record AddClassroomEventRequest(Guid ClassroomId);

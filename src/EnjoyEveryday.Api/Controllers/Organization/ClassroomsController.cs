using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/branches/{branchId}/[controller]")]
public class ClassroomsController : ControllerBase
{
    private readonly ClassroomService _classroomService;

    public ClassroomsController(ClassroomService classroomService)
    {
        _classroomService = classroomService;
    }

    [HttpGet]
    public async Task<IActionResult> GetClassrooms(Guid branchId, CancellationToken cancellationToken)
    {
        var classrooms = await _classroomService.GetClassroomsAsync(branchId, cancellationToken);
        return Ok(classrooms);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClassroom(Guid branchId, Guid id, CancellationToken cancellationToken)
    {
        var classroom = await _classroomService.GetClassroomByIdAsync(id, cancellationToken);
        if (classroom == null || classroom.BranchId != branchId) return NotFound();
        return Ok(classroom);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClassroom(Guid branchId, [FromBody] CreateClassroomRequest request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomService.CreateClassroomAsync(branchId, request.Name, request.AgeGroup, request.Capacity, request.Environment, cancellationToken);
        return CreatedAtAction(nameof(GetClassroom), new { branchId, id = classroom.Id }, classroom);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClassroom(Guid branchId, Guid id, [FromBody] UpdateClassroomRequest request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomService.GetClassroomByIdAsync(id, cancellationToken);
        if (classroom == null || classroom.BranchId != branchId) return NotFound();

        classroom.Name = request.Name;
        classroom.AgeGroup = request.AgeGroup;
        classroom.Capacity = request.Capacity;
        classroom.Environment = request.Environment;
        classroom.IsActive = request.IsActive;

        await _classroomService.UpdateClassroomAsync(classroom, cancellationToken);
        return NoContent();
    }
}

public record CreateClassroomRequest(string Name, string? AgeGroup, int? Capacity, string Environment);
public record UpdateClassroomRequest(string Name, string? AgeGroup, int? Capacity, string Environment, bool IsActive);

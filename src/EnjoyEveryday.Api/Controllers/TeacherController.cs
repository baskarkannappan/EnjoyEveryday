using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TeacherController : ControllerBase
{
    private readonly ITeacherService _teacherService;

    public TeacherController(ITeacherService teacherService)
    {
        _teacherService = teacherService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid tenantId, CancellationToken cancellationToken)
    {
        var teachers = await _teacherService.GetAllTeachersAsync(tenantId, cancellationToken);
        return Ok(teachers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var teacher = await _teacherService.GetTeacherByIdAsync(tenantId, id, cancellationToken);
        if (teacher == null) return NotFound();
        return Ok(teacher);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] Guid tenantId, [FromBody] Teacher teacher, CancellationToken cancellationToken)
    {
        teacher.TenantId = tenantId;
        var created = await _teacherService.CreateTeacherAsync(teacher, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { tenantId, id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromQuery] Guid tenantId, Guid id, [FromBody] Teacher teacher, CancellationToken cancellationToken)
    {
        if (id != teacher.Id) return BadRequest("ID mismatch");
        teacher.TenantId = tenantId;
        await _teacherService.UpdateTeacherAsync(teacher, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromQuery] Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        await _teacherService.DeleteTeacherAsync(tenantId, id, cancellationToken);
        return NoContent();
    }
}

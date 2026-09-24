using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChildrenController : ControllerBase
{
    private readonly ChildService _childService;

    public ChildrenController(ChildService childService)
    {
        _childService = childService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Child>>> GetChildren(CancellationToken cancellationToken)
    {
        var children = await _childService.GetChildrenAsync(cancellationToken);
        return Ok(children);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Child>> GetChild(Guid id, CancellationToken cancellationToken)
    {
        var child = await _childService.GetChildByIdAsync(id, cancellationToken);
        if (child == null) return NotFound();
        return Ok(child);
    }

    [HttpPost]
    public async Task<ActionResult<Child>> CreateChild([FromBody] CreateChildRequest request, CancellationToken cancellationToken)
    {
        var child = await _childService.CreateChildAsync(request.FirstName, request.LastName, request.DateOfBirth, request.ClassroomId, cancellationToken);
        return CreatedAtAction(nameof(GetChild), new { id = child.Id }, child);
    }
}

public record CreateChildRequest(string FirstName, string LastName, DateTime? DateOfBirth, Guid? ClassroomId);

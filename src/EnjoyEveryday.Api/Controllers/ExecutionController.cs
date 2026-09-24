using EnjoyEveryday.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/execution")]
[Authorize]
public class ExecutionController : ControllerBase
{
    private readonly ExecutionService _executionService;

    public ExecutionController(ExecutionService executionService)
    {
        _executionService = executionService;
    }

    [HttpPost("{scheduleId}/start")]
    public async Task<IActionResult> StartExperience(Guid scheduleId, CancellationToken cancellationToken)
    {
        await _executionService.StartExperienceAsync(scheduleId, cancellationToken);
        return Ok();
    }

    [HttpPost("{scheduleId}/complete")]
    public async Task<IActionResult> CompleteExperience(Guid scheduleId, [FromBody] CompleteExperienceRequest request, CancellationToken cancellationToken)
    {
        await _executionService.CompleteExperienceAsync(scheduleId, request.TeacherId, request.Rating, request.Notes, cancellationToken);
        return Ok();
    }
}

public class CompleteExperienceRequest
{
    public Guid TeacherId { get; set; }
    public string Rating { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExperiencesController : ControllerBase
{
    private readonly ExperienceService _experienceService;

    public ExperiencesController(ExperienceService experienceService)
    {
        _experienceService = experienceService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Experience>>> GetExperiences(CancellationToken cancellationToken)
    {
        var experiences = await _experienceService.GetExperiencesAsync(cancellationToken);
        return Ok(experiences);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Experience>> GetExperience(Guid id, CancellationToken cancellationToken)
    {
        var experience = await _experienceService.GetExperienceByIdAsync(id, cancellationToken);
        if (experience == null) return NotFound();
        return Ok(experience);
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<IEnumerable<ExperienceVersion>>> GetExperienceHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await _experienceService.GetExperienceHistoryAsync(id, cancellationToken);
        return Ok(history);
    }

    [HttpPost]
    public async Task<ActionResult<Experience>> CreateExperience([FromBody] CreateExperienceRequest request, CancellationToken cancellationToken)
    {
        // Extract userId from User claims or use dummy for now if auth is incomplete.
        // Assuming a claim "sub" or ClaimTypes.NameIdentifier
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString();
        var userId = Guid.Parse(userIdString);

        var experience = await _experienceService.CreateExperienceAsync(request.Title, request.Description, request.DnaPayload, userId, cancellationToken);
        return CreatedAtAction(nameof(GetExperience), new { id = experience.Id }, experience);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateExperience(Guid id, [FromBody] UpdateExperienceRequest request, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString();
        var userId = Guid.Parse(userIdString);

        try
        {
            await _experienceService.UpdateExperienceAsync(id, request.Title, request.Description, request.DnaPayload, request.Status, request.ChangeReason, userId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}

public record CreateExperienceRequest(string Title, string Description, string DnaPayload);
public record UpdateExperienceRequest(string Title, string Description, string DnaPayload, string Status, string ChangeReason);

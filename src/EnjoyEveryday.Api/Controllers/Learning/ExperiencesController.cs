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

    [HttpGet("catalog")]
    [HttpGet("/api/v1/experiences/catalog")]
    public async Task<ActionResult<IEnumerable<ExperienceCatalogItemDto>>> GetCatalog(CancellationToken cancellationToken)
    {
        var catalog = await _experienceService.GetCatalogAsync(cancellationToken: cancellationToken);
        return Ok(catalog);
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

        var experience = await _experienceService.CreateExperienceAsync(request.Title, request.Description, request.DnaPayload, userId, null, cancellationToken);
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

    [HttpPost("{id}/publish")]
    [HttpPost("/api/v1/experiences/{id}/publish")]
    public async Task<ActionResult<PublishExperienceResult>> PublishExperience(Guid id, [FromBody] PublishExperienceRequest request, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString();
        var userId = Guid.TryParse(userIdString, out var parsedId) ? parsedId : Guid.Empty;

        try
        {
            var result = await _experienceService.PublishExperienceAsync(id, request.Title, request.Description, request.DnaPayload, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public record CreateExperienceRequest(string Title, string Description, string DnaPayload);
public record UpdateExperienceRequest(string Title, string Description, string DnaPayload, string Status, string ChangeReason);
public record PublishExperienceRequest(string Title, string Description, string DnaPayload);


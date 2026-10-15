using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/journey")]
[Authorize]
public class JourneyController : ControllerBase
{
    private readonly JourneyService _journeyService;

    public JourneyController(JourneyService journeyService)
    {
        _journeyService = journeyService;
    }

    [HttpGet("children/{childId}/stories")]
    public async Task<ActionResult<IEnumerable<ChildStory>>> GetStories(Guid childId, [FromQuery] bool includeDrafts = true, CancellationToken cancellationToken = default)
    {
        if (includeDrafts)
            return Ok(await _journeyService.GetChildStoriesAsync(childId, cancellationToken));
        else
            return Ok(await _journeyService.GetPublishedChildStoriesAsync(childId, cancellationToken));
    }

    [HttpGet("children/{childId}/interests")]
    public async Task<ActionResult<IEnumerable<ChildInterest>>> GetInterests(Guid childId, CancellationToken cancellationToken = default)
    {
        return Ok(await _journeyService.GetChildInterestsAsync(childId, cancellationToken));
    }

    [HttpPost("children/{childId}/stories")]
    public async Task<ActionResult<ChildStory>> CreateStory(Guid childId, [FromBody] CreateStoryRequest request, CancellationToken cancellationToken = default)
    {
        var story = await _journeyService.CreateStoryAsync(childId, request.TeacherId, request.ExperienceScheduleId, request.Content, cancellationToken);
        return Ok(story);
    }

    [HttpPost("children/{childId}/interests")]
    public async Task<ActionResult<ChildInterest>> AddInterest(Guid childId, [FromBody] AddInterestRequest request, CancellationToken cancellationToken = default)
    {
        var interest = await _journeyService.AddInterestAsync(childId, request.Name, cancellationToken);
        return Ok(interest);
    }

    [HttpPost("stories/{storyId}/approve")]
    public async Task<IActionResult> ApproveStory(Guid storyId, CancellationToken cancellationToken = default)
    {
        await _journeyService.ApproveStoryAsync(storyId, cancellationToken);
        return Ok();
    }
}

public class CreateStoryRequest
{
    public Guid TeacherId { get; set; }
    public Guid? ExperienceScheduleId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class AddInterestRequest
{
    public string Name { get; set; } = string.Empty;
}

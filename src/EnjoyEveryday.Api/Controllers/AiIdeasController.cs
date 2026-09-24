using System.Collections.Generic;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiIdeasController : ControllerBase
{
    private readonly IAiIdeaService _aiIdeaService;

    public AiIdeasController(IAiIdeaService aiIdeaService)
    {
        _aiIdeaService = aiIdeaService;
    }

    public class IdeaRequest
    {
        public string Idea { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<ActionResult<IEnumerable<AiSuggestion>>> GenerateIdeas([FromBody] IdeaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Idea))
            return BadRequest("Idea cannot be empty.");

        var suggestions = await _aiIdeaService.GenerateIdeasAsync(request.Idea);
        return Ok(suggestions);
    }
}

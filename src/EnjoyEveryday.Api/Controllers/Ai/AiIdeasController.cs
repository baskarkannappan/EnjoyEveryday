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
        public string? AgeGroup { get; set; }
        public string? Environment { get; set; }
        public string? FocusArea { get; set; }
        public List<string> AvailableMaterials { get; set; } = new();
    }

    [HttpPost]
    public async Task<ActionResult<IEnumerable<ExperienceFullData>>> GenerateIdeas([FromBody] IdeaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Idea))
            return BadRequest("Idea cannot be empty.");

        var options = new IdeaGenerationOptions
        {
            AgeGroup = request.AgeGroup,
            Environment = request.Environment,
            FocusArea = request.FocusArea,
            AvailableMaterials = request.AvailableMaterials
        };

        var result = await _aiIdeaService.GenerateIdeasWithDiagnosticsAsync(request.Idea, options);
        return Ok(result.Suggestions);
    }
}

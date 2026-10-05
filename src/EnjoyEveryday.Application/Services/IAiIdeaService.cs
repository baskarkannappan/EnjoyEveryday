using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public class AiSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ExperienceImprovementResult
{
    public string Action { get; set; } = string.Empty;
    public string TargetSection { get; set; } = string.Empty;
    public string CurrentContent { get; set; } = string.Empty;
    public string SuggestedContent { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool RequiresApproval { get; set; }
}

public interface IAiIdeaService
{
    Task<IEnumerable<AiSuggestion>> GenerateIdeasAsync(string ideaInput);
    Task<ExperienceImprovementResult?> ImproveExperienceAsync(object experienceDna, string requestContext);
}

using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public class AiSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public interface IAiIdeaService
{
    Task<IEnumerable<AiSuggestion>> GenerateIdeasAsync(string ideaInput);
}

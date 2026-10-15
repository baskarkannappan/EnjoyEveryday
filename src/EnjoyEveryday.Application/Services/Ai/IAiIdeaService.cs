using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;



public class ExperienceFullData
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
    public string Together { get; set; } = string.Empty;
    public string Create { get; set; } = string.Empty;
    public string ChildChoice { get; set; } = string.Empty;
    public string MagicMoment { get; set; } = string.Empty;
    public string Accomplishment { get; set; } = string.Empty;
    public string Reflection { get; set; } = string.Empty;
    public string TeacherGuidance { get; set; } = string.Empty;
    public List<string> Materials { get; set; } = new();
    public EnjoyEveryday.Domain.Entities.ExecutionGuide ExecutionGuide { get; set; } = new();
}

public class ExperienceImprovementResult
{
    public string Reason { get; set; } = string.Empty;
    public ExperienceFullData UpdatedData { get; set; } = new();
}

public interface IAiIdeaService
{
    Task<IEnumerable<ExperienceFullData>> GenerateIdeasAsync(string ideaInput);
    Task<ExperienceImprovementResult?> ImproveExperienceAsync(object experienceDna, string requestContext);
    Task<IEnumerable<string>> SuggestMaterialsAsync(object experienceDna);
}

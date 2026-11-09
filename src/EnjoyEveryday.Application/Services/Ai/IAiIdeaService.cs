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
    public int AgeMin { get; set; } = 3;
    public int AgeMax { get; set; } = 5;
    public int GroupSizeMin { get; set; } = 2;
    public int GroupSizeMax { get; set; } = 6;
    public string Environment { get; set; } = "Indoor";
    public List<string> DevelopmentalGoals { get; set; } = new();
    public List<string> Materials { get; set; } = new();
    public EnjoyEveryday.Domain.Entities.ExecutionGuide ExecutionGuide { get; set; } = new();

    public void ApplyTo(EnjoyEveryday.Domain.Entities.Experience experience, EnjoyEveryday.Domain.Entities.ExperienceDna dna)
    {
        if (!string.IsNullOrWhiteSpace(Title)) experience.Title = Title;
        if (!string.IsNullOrWhiteSpace(Description)) experience.Description = Description;
        if (!string.IsNullOrWhiteSpace(Challenge)) dna.Challenge = Challenge;
        if (!string.IsNullOrWhiteSpace(Together)) dna.Together = Together;
        if (!string.IsNullOrWhiteSpace(Create)) dna.Create = Create;
        if (!string.IsNullOrWhiteSpace(ChildChoice)) dna.ChildChoice = ChildChoice;
        if (!string.IsNullOrWhiteSpace(MagicMoment)) dna.MagicMoment = MagicMoment;
        if (!string.IsNullOrWhiteSpace(Accomplishment)) dna.Accomplishment = Accomplishment;
        if (!string.IsNullOrWhiteSpace(Reflection)) dna.Reflection = Reflection;
        if (!string.IsNullOrWhiteSpace(TeacherGuidance)) dna.TeacherGuidance = TeacherGuidance;
        if (AgeMin > 0) dna.AgeMin = AgeMin;
        if (AgeMax > 0) dna.AgeMax = AgeMax;
        if (GroupSizeMin > 0) dna.GroupSizeMin = GroupSizeMin;
        if (GroupSizeMax > 0) dna.GroupSizeMax = GroupSizeMax;
        if (!string.IsNullOrWhiteSpace(Environment)) dna.Environment = Environment;
        if (DevelopmentalGoals != null && DevelopmentalGoals.Count > 0) dna.DevelopmentalGoals = new List<string>(DevelopmentalGoals);
        if (Materials != null && Materials.Count > 0) dna.Materials = new List<string>(Materials);
        if (ExecutionGuide != null && ExecutionGuide.Phases.Count > 0) dna.ExecutionGuide = ExecutionGuide;
    }
}

public class ExperienceImprovementResult
{
    public string Reason { get; set; } = string.Empty;
    public ExperienceFullData UpdatedData { get; set; } = new();
    public AiIdeaDiagnostics Diagnostics { get; set; } = new();
}

public class AiExecutionMeta
{
    public string Provider { get; set; } = "GoogleGemini";
    public string Model { get; set; } = "gemini-3.1-flash-lite";
    public bool IsFallback { get; set; }
    public int ExecutionTimeMs { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AiIdeaDiagnostics
{
    public List<string> Logs { get; set; } = new();
    public string StatusBadge { get; set; } = "Ready";
    public string StatusType { get; set; } = "success"; // "connecting", "success", "fallback", "error"
    public AiExecutionMeta? Meta { get; set; }
}

public class AiIdeaGenerationResult
{
    public List<ExperienceFullData> Suggestions { get; set; } = new();
    public AiIdeaDiagnostics Diagnostics { get; set; } = new();
}

public interface IAiIdeaService
{
    Task<IEnumerable<ExperienceFullData>> GenerateIdeasAsync(string ideaInput);
    Task<AiIdeaGenerationResult> GenerateIdeasWithDiagnosticsAsync(string ideaInput, Action<string>? onProgress = null);
    Task<ExperienceImprovementResult?> ImproveExperienceAsync(object experienceDna, string requestContext, Action<string>? onProgress = null);
    Task<IEnumerable<string>> SuggestMaterialsAsync(object experienceDna);
}

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
        if (DevelopmentalGoals != null && DevelopmentalGoals.Count > 0) dna.DevelopmentalGoals = DevelopmentalGoals;
        if (Materials != null) dna.Materials = Materials;
        if (ExecutionGuide != null) dna.ExecutionGuide = ExecutionGuide;
    }
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

using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public class ExperienceDna
{
    public int AgeMin { get; set; } = 3;
    public int AgeMax { get; set; } = 5;
    public int GroupSizeMin { get; set; } = 4;
    public int GroupSizeMax { get; set; } = 10;
    public string Environment { get; set; } = "Indoor / Outdoor";
    
    public string Challenge { get; set; } = string.Empty;
    public string Together { get; set; } = string.Empty;
    public string Create { get; set; } = string.Empty;
    public string ChildChoice { get; set; } = string.Empty;
    public string MagicMoment { get; set; } = string.Empty;
    public string Accomplishment { get; set; } = string.Empty;
    public string Reflection { get; set; } = string.Empty;
    public string TeacherGuidance { get; set; } = string.Empty;

    public List<string> Materials { get; set; } = new();
    public List<string> DevelopmentalGoals { get; set; } = new();

    public ExecutionGuide ExecutionGuide { get; set; } = new();
}

public class ExecutionGuide
{
    public List<string> Preparation { get; set; } = new();
    public List<string> EnvironmentSetup { get; set; } = new();
    public List<string> MaterialSetup { get; set; } = new();
    public int SuggestedDurationMinutes { get; set; } = 25;
    public string TeacherIntroduction { get; set; } = string.Empty;
    public List<ExecutionPhase> Phases { get; set; } = new();
    public string Watch { get; set; } = string.Empty;
    public string Ask { get; set; } = string.Empty;
    public string Try { get; set; } = string.Empty;
    public List<string> Safety { get; set; } = new();
    public List<string> Adaptations { get; set; } = new();
    public List<string> Extensions { get; set; } = new();
    public string Closing { get; set; } = string.Empty;
    public string LittleMomentOpportunity { get; set; } = string.Empty;
}

public class ExecutionPhase
{
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string ChildrenDo { get; set; } = string.Empty;
    public string TeacherDoes { get; set; } = string.Empty;
}

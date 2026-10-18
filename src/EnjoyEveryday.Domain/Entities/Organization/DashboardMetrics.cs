namespace EnjoyEveryday.Domain.Entities;

public class OrganizationMetrics
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public int TotalBranches { get; set; }
    public int TotalClassrooms { get; set; }
    public int ActiveBranchCount { get; set; }
    public int ChildrenExperiencingToday { get; set; }
    public int ExperiencesThisWeek { get; set; }
    public int ExperiencesThisWeekCompleted { get; set; }
    public int ExperiencesThisWeekUpcoming { get; set; }
    public int TeacherCreatedExperiences { get; set; }
    public int TeacherCreatedExperiencesThisMonth { get; set; }
    public int TotalChildren { get; set; }
    public int TotalTeachers { get; set; }
}

public class BranchMetrics
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Classrooms { get; set; }
    public int Children { get; set; }
    public int Experiences { get; set; }
    public int ExperienceRhythm { get; set; }
    public int Teachers { get; set; }
    public string AttentionStatus { get; set; } = string.Empty;
    public string RecentInsight { get; set; } = string.Empty;
}

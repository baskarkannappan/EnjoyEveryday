namespace EnjoyEveryday.UI.Shared.Components.ClassroomWizard;

using System.ComponentModel.DataAnnotations;

public class ClassroomWizardModel
{
    // Step 1: Basic Information
    public Guid? BranchId { get; set; }
    [Required]
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public string? Description { get; set; }
    [Required]
    public string ClassroomType { get; set; } = "Preschool";
    [Required]
    public string AgeGroup { get; set; } = "3-4 years";
    public int? MinAgeMonths { get; set; }
    public int? MaxAgeMonths { get; set; }

    // Step 2: Capacity
    public int? Capacity { get; set; }

    // Step 3: Environment
    public string Environment { get; set; } = "Indoor";
    public string? PhotoUrl { get; set; }

    // Step 4: Teachers
    public string? LeadTeacher { get; set; }
    public string? AssistantTeachers { get; set; }

    // Step 5: Schedule
    public TimeOnly? OpeningTime { get; set; } = new TimeOnly(8, 0);
    public TimeOnly? ClosingTime { get; set; } = new TimeOnly(17, 0);

    // Step 6: Daily Routine
    public TimeOnly? ArrivalTime { get; set; } = new TimeOnly(8, 30);
    public TimeOnly? MealTime { get; set; } = new TimeOnly(11, 30);
    public TimeOnly? NapTime { get; set; } = new TimeOnly(12, 30);
    public TimeOnly? DepartureTime { get; set; } = new TimeOnly(16, 30);

    // Step 7: Safety
    public string? EmergencyExit { get; set; }
    public string? FirstAidLocation { get; set; }
    public string? AssemblyPoint { get; set; }

    // Step 8: Resources
    public string? Equipment { get; set; }
    public string? LearningResources { get; set; }
}

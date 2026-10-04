namespace EnjoyEveryday.Domain.Entities;

public class ChildJourneyEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
    public Guid? EnrollmentId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid TeacherId { get; set; }
    
    // Mapping ActivityId to ExperienceId and ExperienceScheduleId based on the architecture
    public Guid? ExperienceId { get; set; }
    public Guid? ExperienceScheduleId { get; set; }
    public Guid? ExperienceSessionId { get; set; }
    public Guid? ExperienceParticipationId { get; set; }
    public Guid? LittleMomentId { get; set; }
    public Guid? MilestoneId { get; set; }
    
    public string JourneyType { get; set; } = string.Empty; // Observation, LittleMoment, ActivityObservation, Discovery, Milestone
    public string? Title { get; set; }
    public string Observation { get; set; } = string.Empty;
    public string? ChildVoice { get; set; }
    public string? TeacherReflection { get; set; }
    
    public DateTimeOffset ObservationDate { get; set; } = DateTimeOffset.UtcNow;
    public string? ParticipationType { get; set; }
    
    public bool IsMilestone { get; set; }
    public bool IsParentVisible { get; set; }
    public string? ParentVisibilityStatus { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
    
    public int Version { get; set; } = 1;
}

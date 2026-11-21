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
    public Guid? EventId { get; set; }
    
    /// <summary>
    /// Type of journey entry (e.g. Observation, LittleMoment, ActivityObservation, Discovery, Milestone, Attendance).
    /// See <see cref="Constants.JourneyTypes"/> for standard constants.
    /// </summary>
    public string JourneyType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Observation { get; set; } = string.Empty;
    public string? ChildVoice { get; set; }
    public string? TeacherReflection { get; set; }
    
    public DateTimeOffset ObservationDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Participation mode. See <see cref="Constants.ParticipationTypes"/> for standard constants.
    /// </summary>
    public string? ParticipationType { get; set; }
    
    public bool IsMilestone { get; set; }
    public bool IsParentVisible { get; set; }

    /// <summary>
    /// Parent visibility status (Draft, PendingReview, Approved).
    /// See <see cref="Constants.ParentVisibilityStatuses"/> for standard constants.
    /// </summary>
    public string? ParentVisibilityStatus { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
    
    public int Version { get; set; } = 1;
    
    public List<string> Photos { get; set; } = new();
}

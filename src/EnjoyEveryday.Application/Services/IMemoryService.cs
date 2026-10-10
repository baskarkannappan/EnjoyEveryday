namespace EnjoyEveryday.Application.Services;

public class MemoryEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset EventDate { get; set; }
    public string EventType { get; set; } = string.Empty; // "Observation", "Feedback", "Session"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public Guid SourceId { get; set; }
    public string SourceType { get; set; } = string.Empty; // "child_stories", "experience_feedbacks", "experience_sessions"

    public Guid? ChildId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? TeacherId { get; set; }
    public Guid? ExperienceId { get; set; }
}

public class MemoryQueryParameters
{
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int Limit { get; set; } = 50;
    public int Offset { get; set; } = 0;
}

public interface IMemoryService
{
    Task<IEnumerable<MemoryEvent>> GetChildMemoriesAsync(Guid childId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<IEnumerable<MemoryEvent>> GetClassroomMemoriesAsync(Guid classroomId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<IEnumerable<MemoryEvent>> GetTeacherMemoriesAsync(Guid teacherId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<IEnumerable<MemoryEvent>> GetExperienceMemoriesAsync(Guid experienceId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default);
}

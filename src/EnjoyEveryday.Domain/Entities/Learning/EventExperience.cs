using System;

namespace EnjoyEveryday.Domain.Entities;

public class EventExperience
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid ExperienceId { get; set; }
    public Guid? ExperienceScheduleId { get; set; }
    public Guid? ExperienceSessionId { get; set; }
}

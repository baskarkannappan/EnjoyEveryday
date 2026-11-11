using System;

namespace EnjoyEveryday.Domain.Entities;

public class ExperienceCatalogItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DnaPayload { get; set; } = "{}";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string ExperienceType { get; set; } = "Experience";
    public bool IsAiCreated { get; set; }
    public bool IsPlatform { get; set; }
    public Guid? PlatformExperienceId { get; set; }
    public string AgeGroup { get; set; } = "3-4";
    public string Duration { get; set; } = "15 min";
    public string Environment { get; set; } = "Indoor";
}

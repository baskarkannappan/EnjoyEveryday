using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public class ActivityRecommendationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid? ChildId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset RequestTimestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Scope { get; set; } = "Classroom"; // "Classroom" or "Child"
    public string? ContextPreferences { get; set; } // JSON
    public int EvidenceCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<ActivityRecommendationItem> Items { get; set; } = new();
}

public class ActivityRecommendationItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ExperienceId { get; set; }
    public string ExperienceTitle { get; set; } = string.Empty;
    public string? SuggestedFormat { get; set; } // "Individual", "Small Group", "Whole Class"
    public int? EstimatedDurationMinutes { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public string? SupportingEvidence { get; set; } // JSON array of evidence citations
    public string? Materials { get; set; } // JSON array of materials
    public int RankOrder { get; set; } = 1;
    public bool IsEvidenceLimited { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ActivityRecommendationDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid RecommendationItemId { get; set; }
    public Guid TenantId { get; set; }
    public Guid DecidedByUserId { get; set; }
    public string Decision { get; set; } = "Accepted"; // "Accepted", "Edited", "Rejected"
    public string? RejectionReason { get; set; }
    public Guid? ResultingScheduleId { get; set; }
    public DateTimeOffset DecidedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class RecommendationPreferencesDto
{
    public int? PreferredDurationMinutes { get; set; }
    public string? Environment { get; set; }
    public string? Format { get; set; }
    public string? Category { get; set; }
    public string? AdditionalNotes { get; set; }
}

public class SupportingEvidenceRef
{
    public Guid SourceId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public class ChildJourneyInsightRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid ChildId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string PeriodType { get; set; } = "Last30Days"; // Last7Days, Last30Days, CurrentMonth, Custom
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public int EvidenceCount { get; set; }
    public string? OverallSummary { get; set; }
    public string? EvidenceGaps { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<ChildJourneyInsightItem> Items { get; set; } = new();
}

public class ChildJourneyInsightItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid TenantId { get; set; }
    public string Category { get; set; } = "JourneySummary"; // JourneySummary, RecordedInterests, ParticipationPatterns, LearningOpportunities, IdeasToConsider
    public string Classification { get; set; } = "RecordedFact"; // RecordedFact, ObservedPattern, EducationalSuggestion
    public string Statement { get; set; } = string.Empty;
    public string EvidenceStrength { get; set; } = "Supported"; // Strong, Supported, Emerging, Limited
    public string? SupportingEvidence { get; set; } // JSON array of citations
    public Guid? SuggestedExperienceId { get; set; }
    public string? SuggestedExperienceTitle { get; set; }
    public int RankOrder { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ChildJourneyEvidenceCitation
{
    public Guid SourceId { get; set; }
    public string SourceType { get; set; } = string.Empty; // child_stories, child_journey_entries, experience_participations
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? EventDate { get; set; }
}

public class GenerateJourneyInsightInput
{
    public Guid ChildId { get; set; }
    public string PeriodType { get; set; } = "Last30Days"; // Last7Days, Last30Days, CurrentMonth, Custom
    public DateOnly? CustomStartDate { get; set; }
    public DateOnly? CustomEndDate { get; set; }
}

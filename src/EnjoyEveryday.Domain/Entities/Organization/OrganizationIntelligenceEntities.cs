using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public static class OrganizationIntelligenceCategory
{
    public const string OperationalOverview = "OperationalOverview";
    public const string CurriculumVariety = "CurriculumVariety";
    public const string PlanningGaps = "PlanningGaps";
    public const string DataQuality = "DataQuality";
    public const string Engagement = "Engagement";
}

public static class EvidenceStrengthLevel
{
    public const string Strong = "Strong";
    public const string Limited = "Limited";
    public const string Insufficient = "Insufficient";
}

public static class IntelligenceReportingPeriod
{
    public const string Last7Days = "Last7Days";
    public const string Last30Days = "Last30Days";
    public const string ThisMonth = "ThisMonth";
}

public class OrganizationIntelligenceFilter
{
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public string ReportingPeriod { get; set; } = IntelligenceReportingPeriod.Last7Days;
    public string? Category { get; set; }
}

public class OperationalOverviewMetrics
{
    public int ExpectedAttendanceCount { get; set; }
    public int ActualPresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public double AttendanceRatePercentage { get; set; }

    public int TotalClassrooms { get; set; }
    public int PlannedClassroomsCount { get; set; }
    public double PlanningCoveragePercentage { get; set; }

    public int PlannedExperiencesCount { get; set; }
    public int CompletedExperiencesCount { get; set; }
    public int UpcomingExperiencesCount { get; set; }
    public double CompletionRatePercentage => PlannedExperiencesCount > 0 ? Math.Round(CompletedExperiencesCount * 100.0 / PlannedExperiencesCount, 1) : 0.0;

    public int LoggedMomentsCount { get; set; }
    public int LoggedCareEventsCount { get; set; }
    public double DataCompletenessScorePercentage { get; set; }
}

public class CurriculumVarietyMetrics
{
    public int TotalExperiencesConducted { get; set; }
    public int UniqueExperienceTitlesCount { get; set; }
    public double VarietyDiversityPercentage { get; set; }
    public Dictionary<string, int> DomainDistribution { get; set; } = new();
    public List<string> MostFrequentExperiences { get; set; } = new();
}

public class ClassroomGapItem
{
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int UpcomingPlansCount { get; set; }
    public string Status { get; set; } = "Needs Attention";
}

public class PlanningGapsMetrics
{
    public List<ClassroomGapItem> ClassroomsWithFewOrNoPlans { get; set; } = new();
    public int PotentialSchedulingConflictsCount { get; set; }
    public int OverdueUncompletedPlansCount { get; set; }
}

public class DataQualityMetrics
{
    public int MissingDepartureTimesCount { get; set; }
    public int UnmarkedAttendanceDaysCount { get; set; }
    public int DraftsAwaitingReviewCount { get; set; }
    public int TotalActionableItems => MissingDepartureTimesCount + UnmarkedAttendanceDaysCount + DraftsAwaitingReviewCount;
}

public class OrganizationIntelligenceInsight
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }

    public string ScopeType { get; set; } = "Organization";
    public string ReportingPeriod { get; set; } = IntelligenceReportingPeriod.Last7Days;
    public string Category { get; set; } = OrganizationIntelligenceCategory.OperationalOverview;

    public string Title { get; set; } = string.Empty;
    public string Observation { get; set; } = string.Empty;
    public string SupportingMetricsJson { get; set; } = "{}";
    public string? PossibleExplanation { get; set; }
    public string? SuggestedNextStep { get; set; }
    public string EvidenceStrength { get; set; } = EvidenceStrengthLevel.Strong;
    public string? DataLimitations { get; set; }

    public string SourceType { get; set; } = "DeterministicRule";
    public bool IsAiGenerated { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class OrganizationIntelligenceReport
{
    public OrganizationIntelligenceFilter Filter { get; set; } = new();
    public string OrganizationName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string? ClassroomName { get; set; }
    public string ReportingPeriodFormatted { get; set; } = string.Empty;

    public OperationalOverviewMetrics OperationalOverview { get; set; } = new();
    public CurriculumVarietyMetrics CurriculumVariety { get; set; } = new();
    public PlanningGapsMetrics PlanningGaps { get; set; } = new();
    public DataQualityMetrics DataQuality { get; set; } = new();

    public List<OrganizationIntelligenceInsight> Insights { get; set; } = new();
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}


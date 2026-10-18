using System;

namespace EnjoyEveryday.Domain.Entities;

public class BranchHealthScorecard
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int TotalClassrooms { get; set; }
    public int TotalCapacity { get; set; }
    public int EnrolledChildren { get; set; }
    public double OccupancyPercentage => TotalCapacity > 0 ? Math.Round((double)EnrolledChildren / TotalCapacity * 100, 1) : 0;
    public double AttendanceRatePercentage { get; set; }
    public double RatioCompliancePercentage { get; set; }
    public double PlanningCoveragePercentage { get; set; }
    public double TuitionCollectionRatePercentage { get; set; }
    
    // Composite weighted score (Occupancy 25%, Attendance 20%, Ratio 25%, Planning 15%, Tuition 15%)
    public double CompositeHealthScore => Math.Round(
        (OccupancyPercentage * 0.25) +
        (AttendanceRatePercentage * 0.20) +
        (RatioCompliancePercentage * 0.25) +
        (PlanningCoveragePercentage * 0.15) +
        (TuitionCollectionRatePercentage * 0.15), 1);

    public string HealthRating => CompositeHealthScore switch
    {
        >= 88 => "Exceptional",
        >= 75 => "Healthy",
        >= 60 => "Moderate",
        _ => "Needs Attention"
    };

    public string RatingCssClass => HealthRating switch
    {
        "Exceptional" => "exceptional",
        "Healthy" => "healthy",
        "Moderate" => "moderate",
        _ => "attention"
    };
}

public class BranchOperationalGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Enrollment"; // Enrollment, StaffingRatios, Attendance, Financial, QualityCurriculum
    public string? TargetMetric { get; set; }
    public string? CurrentValue { get; set; }
    public string? TargetValue { get; set; }
    public string Status { get; set; } = "InProgress"; // InProgress, Completed, AtRisk
    public DateTime? TargetDate { get; set; }
    public string? OwnerName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

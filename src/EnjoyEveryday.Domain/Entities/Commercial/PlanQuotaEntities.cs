using System;

namespace EnjoyEveryday.Domain.Entities;

public class OrganizationQuotaUsage
{
    public Guid OrganizationId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public int MaxBranches { get; set; }
    public int CurrentBranches { get; set; }
    public int MaxClassrooms { get; set; }
    public int CurrentClassrooms { get; set; }
    public int MaxChildren { get; set; }
    public int CurrentChildren { get; set; }
    public bool IncludesAi { get; set; }

    public bool CanAddBranch => CurrentBranches < MaxBranches;
    public bool CanAddClassroom => CurrentClassrooms < MaxClassrooms;
    public bool CanAddChild => CurrentChildren < MaxChildren;
}

public class PlanQuotaExceededException : InvalidOperationException
{
    public string ResourceType { get; }
    public int CurrentCount { get; }
    public int MaxAllowed { get; }
    public string PlanName { get; }

    public PlanQuotaExceededException(string resourceType, int currentCount, int maxAllowed, string planName)
        : base($"You are exceeding your quota limit. Your current plan '{planName}' allows a maximum of {maxAllowed} {resourceType.ToLowerInvariant()} (currently using {currentCount}/{maxAllowed}). Please upgrade your subscription plan to add more.")
    {
        ResourceType = resourceType;
        CurrentCount = currentCount;
        MaxAllowed = maxAllowed;
        PlanName = planName;
    }
}


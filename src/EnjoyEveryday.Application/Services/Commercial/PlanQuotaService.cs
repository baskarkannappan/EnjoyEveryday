using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public interface IPlanQuotaService
{
    Task<OrganizationQuotaUsage> GetQuotaUsageAsync(Guid organizationId, CancellationToken ct = default);
    Task ValidateCanAddBranchAsync(Guid organizationId, CancellationToken ct = default);
    Task ValidateCanAddClassroomAsync(Guid organizationId, CancellationToken ct = default);
    Task ValidateCanAddChildAsync(Guid organizationId, CancellationToken ct = default);
}

public class PlanQuotaService : IPlanQuotaService
{
    private readonly IPlatformAdminRepository _repository;
    private readonly ILogger<PlanQuotaService> _logger;

    public PlanQuotaService(IPlatformAdminRepository repository, ILogger<PlanQuotaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<OrganizationQuotaUsage> GetQuotaUsageAsync(Guid organizationId, CancellationToken ct = default)
    {
        var usage = await _repository.GetOrganizationQuotaUsageAsync(organizationId, ct);
        if (usage != null) return usage;

        // Fallback default
        return new OrganizationQuotaUsage
        {
            OrganizationId = organizationId,
            PlanId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            PlanCode = "PLAN-BLOOM",
            PlanName = "Bloom (Growth)",
            MaxBranches = 2,
            CurrentBranches = 1,
            MaxClassrooms = 8,
            CurrentClassrooms = 1,
            MaxChildren = 90,
            CurrentChildren = 1,
            IncludesAi = true
        };
    }

    public async Task ValidateCanAddBranchAsync(Guid organizationId, CancellationToken ct = default)
    {
        var usage = await GetQuotaUsageAsync(organizationId, ct);
        if (!usage.CanAddBranch)
        {
            _logger.LogWarning("Branch quota exceeded for organization {OrgId}: {Current}/{Max} on plan '{Plan}'",
                organizationId, usage.CurrentBranches, usage.MaxBranches, usage.PlanName);
            throw new PlanQuotaExceededException("Branches", usage.CurrentBranches, usage.MaxBranches, usage.PlanName);
        }
    }

    public async Task ValidateCanAddClassroomAsync(Guid organizationId, CancellationToken ct = default)
    {
        var usage = await GetQuotaUsageAsync(organizationId, ct);
        if (!usage.CanAddClassroom)
        {
            _logger.LogWarning("Classroom quota exceeded for organization {OrgId}: {Current}/{Max} on plan '{Plan}'",
                organizationId, usage.CurrentClassrooms, usage.MaxClassrooms, usage.PlanName);
            throw new PlanQuotaExceededException("Classrooms", usage.CurrentClassrooms, usage.MaxClassrooms, usage.PlanName);
        }
    }

    public async Task ValidateCanAddChildAsync(Guid organizationId, CancellationToken ct = default)
    {
        var usage = await GetQuotaUsageAsync(organizationId, ct);
        if (!usage.CanAddChild)
        {
            _logger.LogWarning("Child enrollment quota exceeded for organization {OrgId}: {Current}/{Max} on plan '{Plan}'",
                organizationId, usage.CurrentChildren, usage.MaxChildren, usage.PlanName);
            throw new PlanQuotaExceededException("Children", usage.CurrentChildren, usage.MaxChildren, usage.PlanName);
        }
    }
}


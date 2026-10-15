using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IOrganizationIntelligenceRepository
{
    Task<OperationalOverviewMetrics> GetOperationalOverviewAsync(
        Guid tenantId, 
        OrganizationIntelligenceFilter filter, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken cancellationToken = default);

    Task<CurriculumVarietyMetrics> GetCurriculumVarietyAsync(
        Guid tenantId, 
        OrganizationIntelligenceFilter filter, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken cancellationToken = default);

    Task<PlanningGapsMetrics> GetPlanningGapsAsync(
        Guid tenantId, 
        OrganizationIntelligenceFilter filter, 
        DateOnly today, 
        CancellationToken cancellationToken = default);

    Task<DataQualityMetrics> GetDataQualityAsync(
        Guid tenantId, 
        OrganizationIntelligenceFilter filter, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken cancellationToken = default);

    Task<OrganizationIntelligenceInsight> SaveInsightAsync(
        OrganizationIntelligenceInsight insight, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<OrganizationIntelligenceInsight>> GetRecentInsightsAsync(
        Guid tenantId, 
        Guid organizationId, 
        Guid? branchId, 
        Guid? classroomId, 
        string reportingPeriod, 
        CancellationToken cancellationToken = default);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SafetyTrustController : ControllerBase
{
    private readonly IConsentService _consentService;
    private readonly ChildService _childService;
    private readonly BranchService _branchService;
    private readonly OrganizationService _orgService;
    private readonly ISystemLookupCatalog _lookupCatalog;

    public SafetyTrustController(
        IConsentService consentService,
        ChildService childService,
        BranchService branchService,
        OrganizationService orgService,
        ISystemLookupCatalog lookupCatalog)
    {
        _consentService = consentService;
        _childService = childService;
        _branchService = branchService;
        _orgService = orgService;
        _lookupCatalog = lookupCatalog;
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<SafetyTrustMetricsDto>> GetMetrics(CancellationToken ct)
    {
        var children = (await _childService.GetChildrenAsync(ct)).ToList();
        var docs = (await _consentService.GetDocumentsAsync(ct: ct)).ToList();

        var signedChildIds = docs.Where(d => d.Status == ConsentWorkflowStatus.Signed)
                                 .Select(d => d.ChildId)
                                 .Distinct()
                                 .ToHashSet();

        var mediaDocs = docs.Where(d => (d.DocumentTitle != null && d.DocumentTitle.Contains("Media", StringComparison.OrdinalIgnoreCase))
                                     || (d.DocumentTitle != null && d.DocumentTitle.Contains("Photo", StringComparison.OrdinalIgnoreCase))).ToList();
        var signedMediaChildIds = mediaDocs.Where(d => d.Status == ConsentWorkflowStatus.Signed)
                                           .Select(d => d.ChildId)
                                           .Distinct()
                                           .ToHashSet();

        int totalChildren = children.Count;
        int consented = totalChildren > 0 ? signedChildIds.Count : 0;
        int mediaConsented = totalChildren > 0 ? signedMediaChildIds.Count : 0;
        int consentPercent = totalChildren > 0 ? (int)Math.Round((double)consented / totalChildren * 100) : 100;

        return Ok(new SafetyTrustMetricsDto
        {
            TotalChildrenCount = totalChildren,
            ConsentedChildrenCount = consented,
            ConsentPercentage = consentPercent,
            MediaConsentCount = mediaConsented,
            MediaReviewRequiredCount = Math.Max(0, totalChildren - mediaConsented),
            OpenIncidentsCount = 0,
            AiSafetyCheckPercentage = 100
        });
    }

    [HttpGet("consent-documents")]
    public async Task<ActionResult<IEnumerable<ChildConsentDocument>>> GetConsentDocuments(CancellationToken ct)
    {
        var docs = await _consentService.GetDocumentsAsync(ct: ct);
        return Ok(docs);
    }
}

public class SafetyTrustMetricsDto
{
    public int TotalChildrenCount { get; set; }
    public int ConsentedChildrenCount { get; set; }
    public int ConsentPercentage { get; set; }
    public int MediaConsentCount { get; set; }
    public int MediaReviewRequiredCount { get; set; }
    public int OpenIncidentsCount { get; set; }
    public int AiSafetyCheckPercentage { get; set; }
}

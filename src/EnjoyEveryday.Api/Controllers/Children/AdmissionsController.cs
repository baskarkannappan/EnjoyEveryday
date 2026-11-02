using System;
using System.Collections.Generic;
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
public class AdmissionsController : ControllerBase
{
    private readonly IEnrollmentPipelineService _pipelineService;

    public AdmissionsController(IEnrollmentPipelineService pipelineService)
    {
        _pipelineService = pipelineService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<PipelineOverviewDto>> GetOverview(
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var overview = await _pipelineService.GetPipelineOverviewAsync(branchId, ct);
        return Ok(overview);
    }

    [HttpGet("applications")]
    public async Task<ActionResult<List<EnrollmentPipelineApplicationDto>>> GetApplications(
        [FromQuery] Guid? branchId,
        [FromQuery] string? stage,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var apps = await _pipelineService.GetApplicationsAsync(branchId, stage, search, ct);
        return Ok(apps);
    }

    [HttpGet("applications/{applicationId}")]
    public async Task<ActionResult<EnrollmentPipelineApplicationDto>> GetApplicationById(
        Guid applicationId,
        CancellationToken ct)
    {
        var app = await _pipelineService.GetApplicationByIdAsync(applicationId, ct);
        if (app == null) return NotFound();
        return Ok(app);
    }

    [HttpPost("applications")]
    public async Task<ActionResult<Guid>> CreateApplication(
        [FromBody] CreateApplicationRequest req,
        CancellationToken ct)
    {
        var id = await _pipelineService.CreateApplicationAsync(req, ct);
        return Ok(id);
    }

    [HttpPost("applications/{applicationId}/stage")]
    public async Task<ActionResult> UpdateStage(
        Guid applicationId,
        [FromBody] UpdateStageRequest req,
        CancellationToken ct)
    {
        await _pipelineService.UpdateStageAsync(applicationId, req.NewStage, req.Notes, req.TourDate, ct);
        return NoContent();
    }

    [HttpPost("finalize-contract")]
    public async Task<ActionResult<Guid>> FinalizeContract(
        [FromBody] FinalizeEnrollmentContractRequest req,
        CancellationToken ct)
    {
        var contractId = await _pipelineService.FinalizeEnrollmentAndBillingAsync(req, ct);
        return Ok(contractId);
    }

    [HttpGet("vacancies")]
    public async Task<ActionResult<List<ClassroomVacancyStats>>> GetVacancies(
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var stats = await _pipelineService.GetClassroomVacanciesAsync(branchId, ct);
        return Ok(stats);
    }

    [HttpGet("family-contracts")]
    public async Task<ActionResult<List<FamilyBillingContract>>> GetFamilyContracts(
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var contracts = await _pipelineService.GetActiveFamilyContractsAsync(branchId, ct);
        return Ok(contracts);
    }
}

public class UpdateStageRequest
{
    public string NewStage { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? TourDate { get; set; }
}

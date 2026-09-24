using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId}/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly BranchService _branchService;

    public BranchesController(BranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBranches(Guid organizationId, CancellationToken cancellationToken)
    {
        var branches = await _branchService.GetBranchesAsync(organizationId, cancellationToken);
        return Ok(branches);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBranch(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var branch = await _branchService.GetBranchByIdAsync(id, cancellationToken);
        if (branch == null || branch.OrganizationId != organizationId) return NotFound();
        return Ok(branch);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBranch(Guid organizationId, [FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var branch = await _branchService.CreateBranchAsync(organizationId, request.Name, request.Location, cancellationToken);
        return CreatedAtAction(nameof(GetBranch), new { organizationId, id = branch.Id }, branch);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBranch(Guid organizationId, Guid id, [FromBody] UpdateBranchRequest request, CancellationToken cancellationToken)
    {
        var branch = await _branchService.GetBranchByIdAsync(id, cancellationToken);
        if (branch == null || branch.OrganizationId != organizationId) return NotFound();

        branch.Name = request.Name;
        branch.Location = request.Location;
        branch.IsActive = request.IsActive;

        await _branchService.UpdateBranchAsync(branch, cancellationToken);
        return NoContent();
    }
}

public record CreateBranchRequest(string Name, string? Location);
public record UpdateBranchRequest(string Name, string? Location, bool IsActive);

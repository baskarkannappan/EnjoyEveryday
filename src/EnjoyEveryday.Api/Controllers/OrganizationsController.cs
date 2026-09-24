using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly OrganizationService _organizationService;

    public OrganizationsController(OrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganizations(CancellationToken cancellationToken)
    {
        var orgs = await _organizationService.GetOrganizationsAsync(cancellationToken);
        return Ok(orgs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrganization(Guid id, CancellationToken cancellationToken)
    {
        var org = await _organizationService.GetOrganizationByIdAsync(id, cancellationToken);
        if (org == null) return NotFound();
        return Ok(org);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var org = await _organizationService.CreateOrganizationAsync(request.Name, request.Location, cancellationToken);
        return CreatedAtAction(nameof(GetOrganization), new { id = org.Id }, org);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrganization(Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var org = await _organizationService.GetOrganizationByIdAsync(id, cancellationToken);
        if (org == null) return NotFound();

        org.Name = request.Name;
        org.Location = request.Location;
        org.IsActive = request.IsActive;

        await _organizationService.UpdateOrganizationAsync(org, cancellationToken);
        return NoContent();
    }
}

public record CreateOrganizationRequest(string Name, string? Location);
public record UpdateOrganizationRequest(string Name, string? Location, bool IsActive);

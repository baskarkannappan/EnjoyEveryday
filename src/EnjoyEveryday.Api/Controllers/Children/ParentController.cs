using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/v1/parent")]
[Authorize]
public class ParentController : ControllerBase
{
    private readonly IParentService _parentService;

    public ParentController(IParentService parentService)
    {
        _parentService = parentService;
    }

    [HttpGet("children/{childId:guid}/today")]
    [HttpGet("/api/children/{childId:guid}/today")]
    public async Task<ActionResult<ChildDailySummary>> GetChildTodaySummary(Guid childId, [FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateTime.Today;
        try
        {
            var summary = await _parentService.GetChildDailySummaryAsync(childId, targetDate, cancellationToken);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}

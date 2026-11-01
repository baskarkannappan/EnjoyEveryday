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
public class CommunicationController : ControllerBase
{
    private readonly IClassroomCommunicationService _communicationService;

    public CommunicationController(IClassroomCommunicationService communicationService)
    {
        _communicationService = communicationService;
    }

    [HttpGet("conversations-feed")]
    public async Task<ActionResult<ClassroomConversationsFeedDto>> GetConversationsFeed(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? classroomId,
        CancellationToken ct)
    {
        var feed = await _communicationService.GetConversationsFeedAsync(organizationId, branchId, classroomId, ct);
        return Ok(feed);
    }

    [HttpGet("children/{childId}/messages")]
    public async Task<ActionResult<IEnumerable<ParentChatMessage>>> GetChildMessages(
        Guid childId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var messages = await _communicationService.GetChildMessagesForEducatorAsync(childId, take, ct);
        return Ok(messages);
    }

    [HttpPost("messages")]
    public async Task<ActionResult<ParentChatMessage>> SendMessage(
        [FromBody] SendMessageRequest req,
        CancellationToken ct)
    {
        var sent = await _communicationService.SendEducatorMessageAsync(req.ChildId, req.Content, req.ClassroomId, ct);
        return Ok(sent);
    }

    [HttpPost("children/{childId}/read")]
    public async Task<ActionResult> MarkAsRead(Guid childId, CancellationToken ct)
    {
        await _communicationService.MarkConversationAsReadByEducatorAsync(childId, ct);
        return NoContent();
    }
}

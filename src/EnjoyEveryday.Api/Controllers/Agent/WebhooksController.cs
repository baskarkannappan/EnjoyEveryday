using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities.Security;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers.Agent;

public record CreateWebhookSubscriptionRequest(
    string Name,
    string WebhookUrl,
    List<string>? SubscribedEvents = null);

public record WebhookSubscriptionCreatedResponse(
    Guid Id,
    string Name,
    string WebhookUrl,
    string SecretKey,
    List<string> SubscribedEvents,
    bool IsActive,
    DateTime CreatedAt);

public record WebhookSubscriptionDto(
    Guid Id,
    string Name,
    string WebhookUrl,
    List<string> SubscribedEvents,
    bool IsActive,
    DateTime? LastTriggeredAt,
    int FailureCount,
    DateTime CreatedAt);

[ApiController]
[Route("api/v1/agent/webhooks")]
[Authorize]
public class WebhooksController : ControllerBase
{
    private readonly IAgentWebhookRepository _webhookRepo;

    public WebhooksController(IAgentWebhookRepository webhookRepo)
    {
        _webhookRepo = webhookRepo ?? throw new ArgumentNullException(nameof(webhookRepo));
    }

    private (Guid TenantId, Guid UserId) GetCallerIdentity()
    {
        var tenantStr = User.FindFirstValue("tenant_id") ?? "10000000-0000-0000-0000-000000000001";
        var userStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "50000000-0000-0000-0000-000000000002";

        return (Guid.Parse(tenantStr), Guid.Parse(userStr));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WebhookSubscriptionDto>>> GetSubscriptions(CancellationToken cancellationToken)
    {
        var (tenantId, _) = GetCallerIdentity();
        var subscriptions = await _webhookRepo.GetTenantSubscriptionsAsync(tenantId, cancellationToken);

        var result = new List<WebhookSubscriptionDto>();
        foreach (var s in subscriptions)
        {
            result.Add(new WebhookSubscriptionDto(
                s.Id,
                s.Name,
                s.WebhookUrl,
                s.SubscribedEvents,
                s.IsActive,
                s.LastTriggeredAt,
                s.FailureCount,
                s.CreatedAt));
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<WebhookSubscriptionCreatedResponse>> RegisterSubscription(
        [FromBody] CreateWebhookSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.WebhookUrl))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "Name and WebhookUrl are required."
            });
        }

        if (!Uri.TryCreate(request.WebhookUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "WebhookUrl must be a valid HTTP or HTTPS absolute URL."
            });
        }

        var (tenantId, userId) = GetCallerIdentity();

        // Generate 32 bytes cryptographically secure hex secret key
        var secretBytes = new byte[32];
        RandomNumberGenerator.Fill(secretBytes);
        var secretKey = Convert.ToHexString(secretBytes).ToLowerInvariant();

        var events = request.SubscribedEvents != null && request.SubscribedEvents.Count > 0
            ? request.SubscribedEvents
            : new List<string> { "classroom.message.sent", "attendance.recorded" };

        var subscription = new AgentWebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Name = request.Name.Trim(),
            WebhookUrl = request.WebhookUrl.Trim(),
            SecretKey = secretKey,
            SubscribedEvents = events,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var id = await _webhookRepo.CreateSubscriptionAsync(subscription, cancellationToken);

        return Ok(new WebhookSubscriptionCreatedResponse(
            id,
            subscription.Name,
            subscription.WebhookUrl,
            subscription.SecretKey,
            subscription.SubscribedEvents,
            subscription.IsActive,
            subscription.CreatedAt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken cancellationToken)
    {
        var (tenantId, _) = GetCallerIdentity();
        var deleted = await _webhookRepo.DeleteSubscriptionAsync(tenantId, id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}

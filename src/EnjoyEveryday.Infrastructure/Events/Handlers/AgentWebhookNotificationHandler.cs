using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities.Security;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Events;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Events.Handlers;

/// <summary>
/// Background event handler that dispatches domain events to registered AI Agent webhook endpoints
/// with HMAC-SHA256 signatures for authenticity verification.
/// </summary>
public class AgentWebhookNotificationHandler :
    IDomainEventHandler<ClassroomMessageSentEvent>,
    IDomainEventHandler<ChildAttendanceRecordedEvent>
{
    private readonly IAgentWebhookRepository _webhookRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AgentWebhookNotificationHandler> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AgentWebhookNotificationHandler(
        IAgentWebhookRepository webhookRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<AgentWebhookNotificationHandler> logger)
    {
        _webhookRepository = webhookRepository ?? throw new ArgumentNullException(nameof(webhookRepository));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ClassroomMessageSentEvent domainEvent, CancellationToken cancellationToken = default)
    {
        const string eventType = "classroom.message.sent";
        var payload = new
        {
            eventId = domainEvent.EventId,
            eventType,
            timestamp = domainEvent.OccurredOn,
            tenantId = domainEvent.TenantId,
            data = new
            {
                messageId = domainEvent.MessageId,
                childId = domainEvent.ChildId,
                classroomId = domainEvent.ClassroomId,
                senderRole = domainEvent.SenderRole,
                senderName = domainEvent.SenderName,
                contentPreview = domainEvent.ContentPreview
            }
        };

        await DispatchToSubscribersAsync(domainEvent.TenantId, eventType, payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ChildAttendanceRecordedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        const string eventType = "attendance.recorded";
        var payload = new
        {
            eventId = domainEvent.EventId,
            eventType,
            timestamp = domainEvent.OccurredOn,
            tenantId = domainEvent.TenantId,
            data = new
            {
                attendanceId = domainEvent.AttendanceId,
                childId = domainEvent.ChildId,
                classroomId = domainEvent.ClassroomId,
                status = domainEvent.Status,
                recordedAt = domainEvent.RecordedAt
            }
        };

        await DispatchToSubscribersAsync(domainEvent.TenantId, eventType, payload, cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchToSubscribersAsync(
        Guid tenantId,
        string eventType,
        object payload,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await _webhookRepository.GetActiveSubscriptionsForEventAsync(tenantId, eventType, cancellationToken).ConfigureAwait(false);
            var jsonPayload = JsonSerializer.Serialize(payload, JsonOpts);
            var payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

            var httpClient = _httpClientFactory.CreateClient("AgentWebhookClient");
            httpClient.Timeout = TimeSpan.FromSeconds(5); // Bound request timeout

            foreach (var sub in subscriptions)
            {
                await DeliverToEndpointAsync(httpClient, sub, eventType, jsonPayload, payloadBytes, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying or dispatching webhooks for event {EventType} in tenant {TenantId}", eventType, tenantId);
        }
    }

    private async Task DeliverToEndpointAsync(
        HttpClient httpClient,
        AgentWebhookSubscription sub,
        string eventType,
        string jsonPayload,
        byte[] payloadBytes,
        CancellationToken cancellationToken)
    {
        bool success = false;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, sub.WebhookUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            // Compute HMAC-SHA256 signature
            var secretBytes = Encoding.UTF8.GetBytes(sub.SecretKey);
            using var hmac = new HMACSHA256(secretBytes);
            var hash = hmac.ComputeHash(payloadBytes);
            var signature = Convert.ToHexString(hash).ToLowerInvariant();

            request.Headers.Add("X-EnjoyEveryday-Event", eventType);
            request.Headers.Add("X-EnjoyEveryday-Signature", $"sha256={signature}");
            request.Headers.Add("User-Agent", "EnjoyEveryday-AgentWebhook/1.0");

            var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            success = response.IsSuccessStatusCode;

            if (!success)
            {
                _logger.LogWarning("Webhook delivery failed for {SubscriptionName} ({Url}): HTTP {StatusCode}",
                    sub.Name, sub.WebhookUrl, (int)response.StatusCode);
            }
            else
            {
                _logger.LogInformation("Webhook delivered successfully to {SubscriptionName} for event {EventType}",
                    sub.Name, eventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception delivering webhook to {SubscriptionName} ({Url})", sub.Name, sub.WebhookUrl);
            success = false;
        }

        try
        {
            await _webhookRepository.RecordDeliveryResultAsync(sub.Id, success, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record delivery result for subscription {SubscriptionId}", sub.Id);
        }
    }
}

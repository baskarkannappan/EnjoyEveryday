using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class PlatformBusinessEventService : IPlatformBusinessEventService
{
    private readonly IPlatformBusinessEventRepository _eventRepository;
    private readonly IEmailNotificationService? _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PlatformBusinessEventService> _logger;

    public PlatformBusinessEventService(
        IPlatformBusinessEventRepository eventRepository,
        IConfiguration configuration,
        ILogger<PlatformBusinessEventService> logger,
        IEmailNotificationService? emailService = null)
    {
        _eventRepository = eventRepository;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<PlatformBusinessEvent> PublishEventAsync(PublishBusinessEventRequest request, CancellationToken ct = default)
    {
        var eventId = Guid.NewGuid();
        var jsonPayload = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : "{}";

        var businessEvent = new PlatformBusinessEvent
        {
            Id = eventId,
            EventType = request.EventType,
            TenantId = request.TenantId,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            InitiatedByUserId = request.InitiatedByUserId,
            TransactionReference = request.TransactionReference,
            TransactionCategory = request.TransactionCategory,
            Title = request.Title,
            Description = request.Description,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = request.Status,
            EventPayload = jsonPayload,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // 1. Persist Business Event
        await _eventRepository.RecordBusinessEventAsync(businessEvent, ct);

        // 2. Persist In-App Platform Admin Notification
        var notification = new PlatformAdminNotification
        {
            Id = Guid.NewGuid(),
            BusinessEventId = eventId,
            UserId = null, // Global platform notification for all administrators
            Title = request.Title,
            Message = request.Description ?? $"{request.EventType}: {request.TransactionReference}",
            ActionUrl = request.ActionUrl ?? "/marketplace-operations",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _eventRepository.CreateNotificationAsync(notification, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist in-app notification for business event {EventId}", eventId);
        }

        // 3. Dispatch Email Notification to Platform Admins Asynchronously
        if (request.SendPlatformAdminEmail && _emailService != null)
        {
            var adminEmailsConfig = _configuration["PlatformAdmin:NotificationEmails"]
                                 ?? _configuration["AzureCommunicationServices:AdminCcAddress"]
                                 ?? "admin@littlestars.com";

            var recipients = adminEmailsConfig.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var subject = request.EmailSubject ?? $"[EnjoyEveryday Platform] {request.Title}";
            var body = request.EmailHtmlBody ?? $@"
                <p>A new platform business activity has occurred:</p>
                <ul>
                    <li><strong>Activity:</strong> {request.Title}</li>
                    <li><strong>Category:</strong> {request.TransactionCategory}</li>
                    <li><strong>Reference:</strong> {request.TransactionReference ?? "N/A"}</li>
                    <li><strong>Amount:</strong> {(request.Amount.HasValue ? $"{request.Currency} {request.Amount.Value:N2}" : "N/A")}</li>
                    <li><strong>Status:</strong> {request.Status}</li>
                    <li><strong>Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                </ul>
                <p>{request.Description}</p>
                <p><a href='https://localhost:7001{request.ActionUrl ?? "/marketplace-operations"}'>Open Platform Admin Portal &rarr;</a></p>";

            foreach (var recipient in recipients)
            {
                var recipientEmail = recipient;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            toAddress: recipientEmail,
                            subject: subject,
                            htmlBody: body,
                            notificationType: $"Platform_{request.EventType}",
                            tenantId: request.TenantId,
                            metadata: new { BusinessEventId = eventId, request.EventType, request.TransactionReference },
                            ccAddress: null,
                            ct: CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send platform admin email notification to {Email} for event {EventId}", recipientEmail, eventId);
                    }
                });
            }
        }

        return businessEvent;
    }

    public async Task<List<PlatformBusinessEvent>> GetRecentEventsAsync(
        int limit = 50,
        int offset = 0,
        string? category = null,
        string? eventType = null,
        Guid? organizationId = null,
        CancellationToken ct = default)
    {
        return await _eventRepository.GetBusinessEventsAsync(limit, offset, category, eventType, organizationId, ct);
    }

    public async Task<List<PlatformAdminNotification>> GetAdminNotificationsAsync(
        Guid userId,
        bool unreadOnly = false,
        int limit = 30,
        CancellationToken ct = default)
    {
        return await _eventRepository.GetNotificationsAsync(userId, unreadOnly, limit, ct);
    }

    public async Task<int> GetUnreadNotificationCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _eventRepository.GetUnreadNotificationCountAsync(userId, ct);
    }

    public async Task MarkNotificationReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        await _eventRepository.MarkNotificationReadAsync(notificationId, userId, ct);
    }

    public async Task MarkAllNotificationsReadAsync(Guid userId, CancellationToken ct = default)
    {
        await _eventRepository.MarkAllNotificationsReadAsync(userId, ct);
    }

    public async Task<string?> ResolveOrganizationAdminEmailAsync(Guid organizationId, CancellationToken ct = default)
    {
        var result = await _eventRepository.GetOrgAdminEmailForOrgAsync(organizationId, ct);
        return result?.Email;
    }
}


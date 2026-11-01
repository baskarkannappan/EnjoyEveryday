using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Shared.Events;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Events.Handlers;

/// <summary>
/// Background event handler that receives ClassroomMessageSentEvent and dispatches real-time notifications
/// without blocking or delaying the user's primary send-message HTTP request.
/// </summary>
public class ClassroomMessageBackgroundNotificationHandler : IDomainEventHandler<ClassroomMessageSentEvent>
{
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly ILogger<ClassroomMessageBackgroundNotificationHandler> _logger;

    public ClassroomMessageBackgroundNotificationHandler(
        IRealtimeNotificationService realtimeNotificationService,
        ILogger<ClassroomMessageBackgroundNotificationHandler> logger)
    {
        _realtimeNotificationService = realtimeNotificationService ?? throw new ArgumentNullException(nameof(realtimeNotificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ClassroomMessageSentEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing background notification for message {MessageId} from {SenderRole} ({SenderName})",
            domainEvent.MessageId, domainEvent.SenderRole, domainEvent.SenderName);

        var isFromParent = domainEvent.SenderRole.Equals("Parent", StringComparison.OrdinalIgnoreCase);

        var notification = new ParentMessageNotification
        {
            MessageId = domainEvent.MessageId,
            TenantId = domainEvent.TenantId,
            ChildId = domainEvent.ChildId,
            ClassroomId = domainEvent.ClassroomId,
            SenderName = domainEvent.SenderName,
            Content = domainEvent.ContentPreview,
            Timestamp = domainEvent.OccurredOn,
            IsFromParent = isFromParent
        };

        if (domainEvent.SenderRole.Equals("Educator", StringComparison.OrdinalIgnoreCase))
        {
            await _realtimeNotificationService.NotifyEducatorMessageSentAsync(notification, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _realtimeNotificationService.NotifyParentMessageSentAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}

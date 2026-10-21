using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public interface IRealtimeNotificationService
{
    Task NotifyParentMessageSentAsync(ParentMessageNotification notification, CancellationToken cancellationToken = default);
    Task NotifyEducatorMessageSentAsync(ParentMessageNotification notification, CancellationToken cancellationToken = default);
}

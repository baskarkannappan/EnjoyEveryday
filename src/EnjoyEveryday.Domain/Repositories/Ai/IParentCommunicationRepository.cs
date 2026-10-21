using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IParentCommunicationRepository
{
    Task<ParentCommunication> CreateAsync(ParentCommunication communication, CancellationToken cancellationToken = default);
    Task UpdateAsync(ParentCommunication communication, CancellationToken cancellationToken = default);
    Task<ParentCommunication?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ParentCommunication>> GetByChildIdAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ParentCommunicationRecipientInfo>> GetRecipientsForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task UpdateDeliveryStatusAsync(Guid tenantId, Guid id, string status, string? error, DateTimeOffset? sentAt, CancellationToken cancellationToken = default);
    Task<IEnumerable<ParentCommunication>> GetRecentCommunicationsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, Guid? classroomId = null, int limit = 50, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountForEducatorAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, Guid? classroomId = null, CancellationToken cancellationToken = default);
    Task MarkAsReadByEducatorAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
}

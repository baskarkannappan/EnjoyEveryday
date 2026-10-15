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
}

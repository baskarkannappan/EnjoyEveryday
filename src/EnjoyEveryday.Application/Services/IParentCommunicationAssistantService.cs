using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public interface IParentCommunicationAssistantService
{
    Task<IEnumerable<ParentCommunicationRecipientInfo>> GetRecipientsForChildAsync(
        Guid childId, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ParentCommunication>> GetChildCommunicationHistoryAsync(
        Guid childId, 
        CancellationToken cancellationToken = default);

    Task<ParentCommunication> GenerateDraftAsync(
        GenerateParentCommunicationInput input, 
        CancellationToken cancellationToken = default);

    Task<ParentCommunication> UpdateDraftAsync(
        UpdateParentCommunicationDraftInput input, 
        CancellationToken cancellationToken = default);

    Task<ParentCommunication> ApproveAndSendAsync(
        SendParentCommunicationInput input, 
        CancellationToken cancellationToken = default);
}

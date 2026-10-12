using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IConsentRepository
{
    Task<IEnumerable<ConsentTemplate>> GetTemplatesAsync(Guid tenantId, Guid? organizationId = null, CancellationToken ct = default);
    Task<ConsentTemplate?> GetTemplateByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);

    Task<ChildConsentDocument> CreateDocumentAsync(ChildConsentDocument document, List<ConsentSigner> signers, List<ConsentChoice> choices, CancellationToken ct = default);
    Task UpdateDocumentAsync(ChildConsentDocument document, CancellationToken ct = default);
    Task<ChildConsentDocument?> GetDocumentByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<IEnumerable<ChildConsentDocument>> GetDocumentsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, Guid? childId = null, string? status = null, CancellationToken ct = default);

    Task<ConsentSigner?> GetSignerByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<ConsentSigner?> GetSignerByIdAsync(Guid tenantId, Guid signerId, CancellationToken ct = default);
    Task UpdateSignerAsync(ConsentSigner signer, CancellationToken ct = default);

    Task RecordChoicesAsync(Guid tenantId, Guid documentId, List<ConsentChoice> choices, CancellationToken ct = default);
    Task<IEnumerable<ConsentChoice>> GetChoicesByDocumentIdAsync(Guid tenantId, Guid documentId, CancellationToken ct = default);

    Task AddAuditEventAsync(ConsentAuditEvent auditEvent, CancellationToken ct = default);
    Task<IEnumerable<ConsentAuditEvent>> GetAuditEventsAsync(Guid tenantId, Guid documentId, CancellationToken ct = default);
}

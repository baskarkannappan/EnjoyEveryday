using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ConsentRepository : IConsentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ConsentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] CreateDocumentParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "ClassroomId", "ChildId",
        "ConsentTemplateId", "TemplateVersion", "DocumentVersion", "DocumentTitle",
        "DocumentSnapshot", "Status", "SigningDeadline", "CreatedBy", "CreatedAt",
        "SupersedesDocumentId", "RowVersion"
    ];

    private static readonly string[] UpdateDocumentParams =
    [
        "Id", "TenantId", "Status", "AdminReviewedBy", "AdminReviewedAt",
        "AdminReviewNotes", "ApprovedAt", "SentAt", "SignedAt", "DeclinedAt",
        "WithdrawnAt", "SignedDocumentStorageKey", "SignedDocumentChecksum"
    ];

    private static readonly string[] InsertSignerParams =
    [
        "Id", "TenantId", "ConsentDocumentId", "FamilyRelationshipId", "PersonId",
        "IntendedSignerUserId", "SignerNameSnapshot", "SignerEmailSnapshot",
        "SignerRelationship", "IsRequired", "SigningStatus", "TokenHash",
        "TokenExpiresAt", "InvitationSentCount", "LastInvitationSentAt"
    ];

    private static readonly string[] UpdateSignerParams =
    [
        "Id", "TenantId", "SigningStatus", "TokenHash", "TokenExpiresAt",
        "InvitationSentCount", "LastInvitationSentAt", "LastOpenedAt",
        "SignedAt", "SignatureLegalName", "SignatureIntentText",
        "SignatureIpAddress", "SignatureUserAgent"
    ];

    private static readonly string[] ChoiceParams =
    [
        "Id", "TenantId", "ConsentDocumentId", "ConsentSignerId",
        "SectionKey", "SectionTitle", "IsMandatory", "Choice", "RecordedAt"
    ];

    private static readonly string[] AuditEventParams =
    [
        "Id", "TenantId", "OrganizationId", "ConsentDocumentId",
        "ActorType", "ActorUserId", "ActorName", "EventType",
        "TimestampUtc", "CorrelationId", "SafeMetadata"
    ];

    public async Task<IEnumerable<ConsentTemplate>> GetTemplatesAsync(Guid tenantId, Guid? organizationId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFunctionAsync<ConsentTemplate>(
            "fn_get_consent_templates",
            new { TenantId = tenantId, OrganizationId = organizationId },
            cancellationToken: ct);
    }

    public async Task<ConsentTemplate?> GetTemplateByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<ConsentTemplate>(
            "fn_get_consent_template_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: ct);
    }

    public async Task<ChildConsentDocument> CreateDocumentAsync(
        ChildConsentDocument document,
        List<ConsentSigner> signers,
        List<ConsentChoice> choices,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        if (document.Id == Guid.Empty) document.Id = Guid.NewGuid();

        await conn.ExecuteProcedureAsync(
            "sp_create_child_consent_document",
            document,
            parameterNames: CreateDocumentParams,
            transaction: tx,
            cancellationToken: ct);

        if (signers.Any())
        {
            foreach (var s in signers)
            {
                if (s.Id == Guid.Empty) s.Id = Guid.NewGuid();
                s.ConsentDocumentId = document.Id;
                await conn.ExecuteProcedureAsync(
                    "sp_insert_consent_signer",
                    s,
                    parameterNames: InsertSignerParams,
                    transaction: tx,
                    cancellationToken: ct);
            }
        }

        if (choices.Any())
        {
            foreach (var c in choices)
            {
                if (c.Id == Guid.Empty) c.Id = Guid.NewGuid();
                c.ConsentDocumentId = document.Id;
                await conn.ExecuteProcedureAsync(
                    "sp_insert_consent_choice",
                    c,
                    parameterNames: ChoiceParams,
                    transaction: tx,
                    cancellationToken: ct);
            }
        }

        tx.Commit();
        return document;
    }

    public async Task UpdateDocumentAsync(ChildConsentDocument document, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_update_child_consent_document",
            document,
            parameterNames: UpdateDocumentParams,
            cancellationToken: ct);
    }

    public async Task<ChildConsentDocument?> GetDocumentByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var doc = await conn.QuerySingleFunctionAsync<ChildConsentDocument>(
            "fn_get_child_consent_document_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: ct);

        if (doc == null) return null;

        var signers = await conn.QueryFunctionAsync<ConsentSigner>(
            "fn_get_consent_signers_by_doc_id",
            new { DocId = id },
            cancellationToken: ct);

        var choices = await conn.QueryFunctionAsync<ConsentChoice>(
            "fn_get_consent_choices_by_doc_id",
            new { DocId = id },
            cancellationToken: ct);

        var audit = await conn.QueryFunctionAsync<ConsentAuditEvent>(
            "fn_get_consent_audit_events_by_doc_id",
            new { DocId = id },
            cancellationToken: ct);

        doc.Signers = signers.ToList();
        doc.Choices = choices.ToList();
        doc.AuditEvents = audit.ToList();
        return doc;
    }

    public async Task<IEnumerable<ChildConsentDocument>> GetDocumentsAsync(
        Guid tenantId,
        Guid? organizationId = null,
        Guid? branchId = null,
        Guid? childId = null,
        string? status = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var docs = (await conn.QueryFunctionAsync<ChildConsentDocument>(
            "fn_get_child_consent_documents",
            new {
                TenantId = tenantId,
                OrgId = organizationId,
                BranchId = branchId,
                ChildId = childId,
                Status = status
            },
            cancellationToken: ct)).ToList();

        if (docs.Any())
        {
            var docIds = docs.Select(x => x.Id).ToArray();
            var signers = (await conn.QueryFunctionAsync<ConsentSigner>(
                "fn_get_consent_signers_by_doc_ids",
                new { DocIds = docIds },
                cancellationToken: ct)).ToList();

            var signersByDoc = signers.GroupBy(s => s.ConsentDocumentId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var doc in docs)
            {
                if (signersByDoc.TryGetValue(doc.Id, out var docSigners))
                {
                    doc.Signers = docSigners;
                }
            }
        }

        return docs;
    }

    public async Task<ConsentSigner?> GetSignerByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<ConsentSigner>(
            "fn_get_consent_signer_by_token_hash",
            new { TokenHash = tokenHash },
            cancellationToken: ct);
    }

    public async Task<ConsentSigner?> GetSignerByIdAsync(Guid tenantId, Guid signerId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<ConsentSigner>(
            "fn_get_consent_signer_by_id",
            new { TenantId = tenantId, SignerId = signerId },
            cancellationToken: ct);
    }

    public async Task UpdateSignerAsync(ConsentSigner signer, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_update_consent_signer",
            signer,
            parameterNames: UpdateSignerParams,
            cancellationToken: ct);
    }

    public async Task RecordChoicesAsync(Guid tenantId, Guid documentId, List<ConsentChoice> choices, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        await conn.ExecuteProcedureAsync(
            "sp_delete_consent_choices",
            new { TenantId = tenantId, DocId = documentId },
            transaction: tx,
            cancellationToken: ct);

        if (choices.Any())
        {
            foreach (var c in choices)
            {
                if (c.Id == Guid.Empty) c.Id = Guid.NewGuid();
                c.TenantId = tenantId;
                c.ConsentDocumentId = documentId;
                await conn.ExecuteProcedureAsync(
                    "sp_insert_consent_choice",
                    c,
                    parameterNames: ChoiceParams,
                    transaction: tx,
                    cancellationToken: ct);
            }
        }

        tx.Commit();
    }

    public async Task<IEnumerable<ConsentChoice>> GetChoicesByDocumentIdAsync(Guid tenantId, Guid documentId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFunctionAsync<ConsentChoice>(
            "fn_get_consent_choices_by_doc_id",
            new { DocId = documentId },
            cancellationToken: ct);
    }

    public async Task AddAuditEventAsync(ConsentAuditEvent auditEvent, CancellationToken ct = default)
    {
        if (auditEvent.Id == Guid.Empty) auditEvent.Id = Guid.NewGuid();

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_add_consent_audit_event",
            auditEvent,
            parameterNames: AuditEventParams,
            cancellationToken: ct);
    }

    public async Task<IEnumerable<ConsentAuditEvent>> GetAuditEventsAsync(Guid tenantId, Guid documentId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFunctionAsync<ConsentAuditEvent>(
            "fn_get_consent_audit_events_by_doc_id",
            new { DocId = documentId },
            cancellationToken: ct);
    }
}

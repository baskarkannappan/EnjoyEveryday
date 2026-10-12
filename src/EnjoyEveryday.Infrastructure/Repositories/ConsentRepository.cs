using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ConsentRepository : IConsentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ConsentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ConsentTemplate>> GetTemplatesAsync(Guid tenantId, Guid? organizationId = null, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM consent_templates
            WHERE tenant_id = @TenantId
              AND is_active = true
              AND (@OrganizationId IS NULL OR organization_id IS NULL OR organization_id = @OrganizationId)
            ORDER BY required_for_enrollment DESC, name ASC";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryAsync<ConsentTemplate>(sql, new { TenantId = tenantId, OrganizationId = organizationId });
    }

    public async Task<ConsentTemplate?> GetTemplateByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        const string sql = @"SELECT * FROM consent_templates WHERE tenant_id = @TenantId AND id = @Id";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ConsentTemplate>(sql, new { TenantId = tenantId, Id = id });
    }

    public async Task<ChildConsentDocument> CreateDocumentAsync(
        ChildConsentDocument document,
        List<ConsentSigner> signers,
        List<ConsentChoice> choices,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        const string docSql = @"
            INSERT INTO child_consent_documents (
                id, tenant_id, organization_id, branch_id, classroom_id, child_id,
                consent_template_id, template_version, document_version, document_title,
                document_snapshot, status, signing_deadline, created_by, created_at,
                supersedes_document_id, row_version
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @ClassroomId, @ChildId,
                @ConsentTemplateId, @TemplateVersion, @DocumentVersion, @DocumentTitle,
                @DocumentSnapshot, @Status, @SigningDeadline, @CreatedBy, @CreatedAt,
                @SupersedesDocumentId, @RowVersion
            )";

        await conn.ExecuteAsync(docSql, document, tx);

        if (signers.Any())
        {
            const string signerSql = @"
                INSERT INTO consent_signers (
                    id, tenant_id, consent_document_id, family_relationship_id, person_id,
                    intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
                    signer_relationship, is_required, signing_status, token_hash,
                    token_expires_at, invitation_sent_count, last_invitation_sent_at
                ) VALUES (
                    @Id, @TenantId, @ConsentDocumentId, @FamilyRelationshipId, @PersonId,
                    @IntendedSignerUserId, @SignerNameSnapshot, @SignerEmailSnapshot,
                    @SignerRelationship, @IsRequired, @SigningStatus, @TokenHash,
                    @TokenExpiresAt, @InvitationSentCount, @LastInvitationSentAt
                )";
            await conn.ExecuteAsync(signerSql, signers, tx);
        }

        if (choices.Any())
        {
            const string choiceSql = @"
                INSERT INTO consent_choices (
                    id, tenant_id, consent_document_id, consent_signer_id,
                    section_key, section_title, is_mandatory, choice, recorded_at
                ) VALUES (
                    @Id, @TenantId, @ConsentDocumentId, @ConsentSignerId,
                    @SectionKey, @SectionTitle, @IsMandatory, @Choice, @RecordedAt
                )";
            await conn.ExecuteAsync(choiceSql, choices, tx);
        }

        tx.Commit();
        return document;
    }

    public async Task UpdateDocumentAsync(ChildConsentDocument document, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE child_consent_documents
            SET status = @Status,
                admin_reviewed_by = @AdminReviewedBy,
                admin_reviewed_at = @AdminReviewedAt,
                admin_review_notes = @AdminReviewNotes,
                approved_at = @ApprovedAt,
                sent_at = @SentAt,
                signed_at = @SignedAt,
                declined_at = @DeclinedAt,
                withdrawn_at = @WithdrawnAt,
                signed_document_storage_key = @SignedDocumentStorageKey,
                signed_document_checksum = @SignedDocumentChecksum,
                row_version = row_version + 1
            WHERE id = @Id AND tenant_id = @TenantId";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, document);
    }

    public async Task<ChildConsentDocument?> GetDocumentByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        const string docSql = @"
            SELECT 
                d.*,
                TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
                b.name AS branch_name,
                cl.name AS classroom_name
            FROM child_consent_documents d
            JOIN children c ON d.child_id = c.id
            LEFT JOIN branches b ON d.branch_id = b.id
            LEFT JOIN classrooms cl ON d.classroom_id = cl.id
            WHERE d.tenant_id = @TenantId AND d.id = @Id";

        const string signersSql = @"SELECT * FROM consent_signers WHERE consent_document_id = @DocId";
        const string choicesSql = @"SELECT * FROM consent_choices WHERE consent_document_id = @DocId";
        const string auditSql = @"SELECT * FROM consent_audit_events WHERE consent_document_id = @DocId ORDER BY timestamp_utc DESC";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var doc = await conn.QuerySingleOrDefaultAsync<ChildConsentDocument>(docSql, new { TenantId = tenantId, Id = id });
        if (doc == null) return null;

        var signers = await conn.QueryAsync<ConsentSigner>(signersSql, new { DocId = id });
        var choices = await conn.QueryAsync<ConsentChoice>(choicesSql, new { DocId = id });
        var audit = await conn.QueryAsync<ConsentAuditEvent>(auditSql, new { DocId = id });

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
        var sql = @"
            SELECT 
                d.*,
                TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
                b.name AS branch_name,
                cl.name AS classroom_name
            FROM child_consent_documents d
            JOIN children c ON d.child_id = c.id
            LEFT JOIN branches b ON d.branch_id = b.id
            LEFT JOIN classrooms cl ON d.classroom_id = cl.id
            WHERE d.tenant_id = @TenantId
              AND (@OrgId IS NULL OR d.organization_id = @OrgId)
              AND (@BranchId IS NULL OR d.branch_id = @BranchId)
              AND (@ChildId IS NULL OR d.child_id = @ChildId)
              AND (@Status IS NULL OR d.status = @Status)
            ORDER BY d.created_at DESC";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var docs = (await conn.QueryAsync<ChildConsentDocument>(sql, new {
            TenantId = tenantId,
            OrgId = organizationId,
            BranchId = branchId,
            ChildId = childId,
            Status = status
        })).ToList();

        if (docs.Any())
        {
            var docIds = docs.Select(x => x.Id).ToList();
            const string signersSql = @"SELECT * FROM consent_signers WHERE consent_document_id = ANY(@DocIds)";
            var signers = (await conn.QueryAsync<ConsentSigner>(signersSql, new { DocIds = docIds })).ToList();
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
        const string sql = @"SELECT * FROM consent_signers WHERE token_hash = @TokenHash";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ConsentSigner>(sql, new { TokenHash = tokenHash });
    }

    public async Task<ConsentSigner?> GetSignerByIdAsync(Guid tenantId, Guid signerId, CancellationToken ct = default)
    {
        const string sql = @"SELECT * FROM consent_signers WHERE tenant_id = @TenantId AND id = @SignerId";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ConsentSigner>(sql, new { TenantId = tenantId, SignerId = signerId });
    }

    public async Task UpdateSignerAsync(ConsentSigner signer, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE consent_signers
            SET signing_status = @SigningStatus,
                token_hash = @TokenHash,
                token_expires_at = @TokenExpiresAt,
                invitation_sent_count = @InvitationSentCount,
                last_invitation_sent_at = @LastInvitationSentAt,
                last_opened_at = @LastOpenedAt,
                signed_at = @SignedAt,
                signature_legal_name = @SignatureLegalName,
                signature_intent_text = @SignatureIntentText,
                signature_ip_address = @SignatureIpAddress,
                signature_user_agent = @SignatureUserAgent
            WHERE id = @Id AND tenant_id = @TenantId";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, signer);
    }

    public async Task RecordChoicesAsync(Guid tenantId, Guid documentId, List<ConsentChoice> choices, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        const string deleteSql = @"DELETE FROM consent_choices WHERE tenant_id = @TenantId AND consent_document_id = @DocId";
        await conn.ExecuteAsync(deleteSql, new { TenantId = tenantId, DocId = documentId }, tx);

        if (choices.Any())
        {
            const string insertSql = @"
                INSERT INTO consent_choices (
                    id, tenant_id, consent_document_id, consent_signer_id,
                    section_key, section_title, is_mandatory, choice, recorded_at
                ) VALUES (
                    @Id, @TenantId, @ConsentDocumentId, @ConsentSignerId,
                    @SectionKey, @SectionTitle, @IsMandatory, @Choice, @RecordedAt
                )";
            await conn.ExecuteAsync(insertSql, choices, tx);
        }

        tx.Commit();
    }

    public async Task<IEnumerable<ConsentChoice>> GetChoicesByDocumentIdAsync(Guid tenantId, Guid documentId, CancellationToken ct = default)
    {
        const string sql = @"SELECT * FROM consent_choices WHERE tenant_id = @TenantId AND consent_document_id = @DocId";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryAsync<ConsentChoice>(sql, new { TenantId = tenantId, DocId = documentId });
    }

    public async Task AddAuditEventAsync(ConsentAuditEvent auditEvent, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO consent_audit_events (
                id, tenant_id, organization_id, consent_document_id,
                actor_type, actor_user_id, actor_name, event_type,
                timestamp_utc, correlation_id, safe_metadata
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @ConsentDocumentId,
                @ActorType, @ActorUserId, @ActorName, @EventType,
                @TimestampUtc, @CorrelationId, @SafeMetadata
            )";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, auditEvent);
    }

    public async Task<IEnumerable<ConsentAuditEvent>> GetAuditEventsAsync(Guid tenantId, Guid documentId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM consent_audit_events
            WHERE tenant_id = @TenantId AND consent_document_id = @DocId
            ORDER BY timestamp_utc DESC";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryAsync<ConsentAuditEvent>(sql, new { TenantId = tenantId, DocId = documentId });
    }
}

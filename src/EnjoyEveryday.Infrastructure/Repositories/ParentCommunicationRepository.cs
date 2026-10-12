using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ParentCommunicationRepository : IParentCommunicationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ParentCommunicationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ParentCommunication> CreateAsync(ParentCommunication comm, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO parent_communications (
                id, tenant_id, organization_id, classroom_id, child_id,
                recipient_person_id, recipient_user_id, recipient_email, recipient_name, recipient_relationship,
                communication_type, reporting_date_start, reporting_date_end,
                title, content_draft, content_final, family_conversation_starter,
                evidence_summary_json, consent_verified, consent_notes,
                status, teacher_id, teacher_name, approved_by_user_id, approved_at, sent_at,
                delivery_channel, delivery_status, delivery_error, idempotency_key,
                created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @ClassroomId, @ChildId,
                @RecipientPersonId, @RecipientUserId, @RecipientEmail, @RecipientName, @RecipientRelationship,
                @CommunicationType, @ReportingDateStart, @ReportingDateEnd,
                @Title, @ContentDraft, @ContentFinal, @FamilyConversationStarter,
                @EvidenceSummaryJson::jsonb, @ConsentVerified, @ConsentNotes,
                @Status, @TeacherId, @TeacherName, @ApprovedByUserId, @ApprovedAt, @SentAt,
                @DeliveryChannel, @DeliveryStatus, @DeliveryError, @IdempotencyKey,
                @CreatedAt, @UpdatedAt
            )
            RETURNING *;";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<ParentCommunication>(sql, new
        {
            comm.Id,
            comm.TenantId,
            comm.OrganizationId,
            comm.ClassroomId,
            comm.ChildId,
            comm.RecipientPersonId,
            comm.RecipientUserId,
            comm.RecipientEmail,
            comm.RecipientName,
            comm.RecipientRelationship,
            comm.CommunicationType,
            ReportingDateStart = comm.ReportingDateStart.ToDateTime(TimeOnly.MinValue),
            ReportingDateEnd = comm.ReportingDateEnd.ToDateTime(TimeOnly.MinValue),
            comm.Title,
            comm.ContentDraft,
            comm.ContentFinal,
            comm.FamilyConversationStarter,
            EvidenceSummaryJson = comm.EvidenceSummaryJson ?? "{}",
            comm.ConsentVerified,
            comm.ConsentNotes,
            comm.Status,
            comm.TeacherId,
            comm.TeacherName,
            comm.ApprovedByUserId,
            comm.ApprovedAt,
            comm.SentAt,
            comm.DeliveryChannel,
            comm.DeliveryStatus,
            comm.DeliveryError,
            comm.IdempotencyKey,
            comm.CreatedAt,
            comm.UpdatedAt
        });
    }

    public async Task UpdateAsync(ParentCommunication comm, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE parent_communications
            SET title = @Title,
                content_final = @ContentFinal,
                family_conversation_starter = @FamilyConversationStarter,
                evidence_summary_json = @EvidenceSummaryJson::jsonb,
                consent_verified = @ConsentVerified,
                consent_notes = @ConsentNotes,
                status = @Status,
                teacher_name = @TeacherName,
                approved_by_user_id = @ApprovedByUserId,
                approved_at = @ApprovedAt,
                sent_at = @SentAt,
                delivery_channel = @DeliveryChannel,
                delivery_status = @DeliveryStatus,
                delivery_error = @DeliveryError,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId;";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new
        {
            comm.Id,
            comm.TenantId,
            comm.Title,
            comm.ContentFinal,
            comm.FamilyConversationStarter,
            EvidenceSummaryJson = comm.EvidenceSummaryJson ?? "{}",
            comm.ConsentVerified,
            comm.ConsentNotes,
            comm.Status,
            comm.TeacherName,
            comm.ApprovedByUserId,
            comm.ApprovedAt,
            comm.SentAt,
            comm.DeliveryChannel,
            comm.DeliveryStatus,
            comm.DeliveryError,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }

    public async Task<ParentCommunication?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                pc.*,
                TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS ChildName,
                cl.name AS ClassroomName
            FROM parent_communications pc
            JOIN children c ON pc.child_id = c.id
            LEFT JOIN classrooms cl ON pc.classroom_id = cl.id
            WHERE pc.id = @Id AND pc.tenant_id = @TenantId
            LIMIT 1;";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<ParentCommunication>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<ParentCommunication>> GetByChildIdAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                pc.*,
                TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS ChildName,
                cl.name AS ClassroomName
            FROM parent_communications pc
            JOIN children c ON pc.child_id = c.id
            LEFT JOIN classrooms cl ON pc.classroom_id = cl.id
            WHERE pc.child_id = @ChildId AND pc.tenant_id = @TenantId
            ORDER BY pc.created_at DESC;";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<ParentCommunication>(sql, new { ChildId = childId, TenantId = tenantId });
    }

    public async Task<IEnumerable<ParentCommunicationRecipientInfo>> GetRecipientsForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT DISTINCT
                p.id AS PersonId,
                p.user_id AS UserId,
                TRIM(CONCAT(p.first_name, ' ', p.last_name)) AS FullName,
                COALESCE(cg.relationship, fr.relationship, p.relationship, 'Guardian') AS Relationship,
                COALESCE(p.email, u.email) AS Email,
                p.mobile_phone AS MobilePhone,
                COALESCE(cg.can_receive_information, true) AS CanReceiveInformation,
                COALESCE(p.receives_daily_updates, true) AS ReceivesDailyUpdates,
                COALESCE(p.has_portal_access, false) AS HasPortalAccess,
                true AS PhotoConsentGranted
            FROM persons p
            LEFT JOIN child_guardians cg ON cg.person_id = p.id AND cg.child_id = @ChildId
            LEFT JOIN family_relationships fr ON fr.user_id = p.user_id AND fr.child_id = @ChildId
            LEFT JOIN users u ON p.user_id = u.id
            WHERE p.tenant_id = @TenantId
              AND (cg.child_id = @ChildId OR fr.child_id = @ChildId);";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<ParentCommunicationRecipientInfo>(sql, new { TenantId = tenantId, ChildId = childId });
    }

    public async Task UpdateDeliveryStatusAsync(Guid tenantId, Guid id, string status, string? error, DateTimeOffset? sentAt, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE parent_communications
            SET delivery_status = @DeliveryStatus,
                delivery_error = @DeliveryError,
                sent_at = COALESCE(@SentAt, sent_at),
                updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId;";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            TenantId = tenantId,
            DeliveryStatus = status,
            DeliveryError = error,
            SentAt = sentAt
        });
    }
}

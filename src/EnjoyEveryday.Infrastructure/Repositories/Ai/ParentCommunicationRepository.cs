using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
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
        if (comm.Id == Guid.Empty) comm.Id = Guid.NewGuid();

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_create_parent_communication",
            new
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
            },
            parameterNames: new[]
            {
                "Id::uuid", "TenantId::uuid", "OrganizationId::uuid", "ClassroomId::uuid", "ChildId::uuid",
                "RecipientPersonId::uuid", "RecipientUserId::uuid", "RecipientEmail::varchar", "RecipientName::varchar", "RecipientRelationship::varchar",
                "CommunicationType::varchar", "ReportingDateStart::date", "ReportingDateEnd::date",
                "Title::varchar", "ContentDraft::text", "ContentFinal::text", "FamilyConversationStarter::text",
                "EvidenceSummaryJson::jsonb", "ConsentVerified::boolean", "ConsentNotes::varchar",
                "Status::varchar", "TeacherId::uuid", "TeacherName::varchar", "ApprovedByUserId::uuid", "ApprovedAt::timestamptz", "SentAt::timestamptz",
                "DeliveryChannel::varchar", "DeliveryStatus::varchar", "DeliveryError::text", "IdempotencyKey::varchar",
                "CreatedAt::timestamptz", "UpdatedAt::timestamptz"
            },
            cancellationToken: cancellationToken);

        return (await GetByIdAsync(comm.TenantId, comm.Id, cancellationToken))!;
    }

    public async Task UpdateAsync(ParentCommunication comm, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_update_parent_communication",
            new
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
            },
            parameterNames: new[]
            {
                "Id::uuid", "TenantId::uuid", "Title::varchar", "ContentFinal::text", "FamilyConversationStarter::text",
                "EvidenceSummaryJson::jsonb", "ConsentVerified::boolean", "ConsentNotes::varchar",
                "Status::varchar", "TeacherName::varchar", "ApprovedByUserId::uuid", "ApprovedAt::timestamptz", "SentAt::timestamptz",
                "DeliveryChannel::varchar", "DeliveryStatus::varchar", "DeliveryError::text", "UpdatedAt::timestamptz"
            },
            cancellationToken: cancellationToken);
    }

    public async Task<ParentCommunication?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<ParentCommunication>(
            "fn_get_parent_communication_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ParentCommunication>> GetByChildIdAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ParentCommunication>(
            "fn_get_parent_communications_by_child_id",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ParentCommunicationRecipientInfo>> GetRecipientsForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ParentCommunicationRecipientInfo>(
            "fn_get_recipients_for_child",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task UpdateDeliveryStatusAsync(Guid tenantId, Guid id, string status, string? error, DateTimeOffset? sentAt, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_update_parent_communication_delivery_status",
            new
            {
                TenantId = tenantId,
                Id = id,
                DeliveryStatus = status,
                DeliveryError = error,
                SentAt = sentAt
            },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ParentCommunication>> GetRecentCommunicationsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, Guid? classroomId = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ParentCommunication>(
            "fn_get_recent_parent_communications",
            new
            {
                TenantId = tenantId,
                OrganizationId = organizationId,
                BranchId = branchId,
                ClassroomId = classroomId,
                Limit = limit
            },
            cancellationToken: cancellationToken);
    }

    public async Task<int> GetUnreadCountForEducatorAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, Guid? classroomId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarFunctionAsync<int>(
            "fn_get_unread_parent_communications_count",
            new
            {
                TenantId = tenantId,
                OrganizationId = organizationId,
                BranchId = branchId,
                ClassroomId = classroomId
            },
            cancellationToken: cancellationToken);
    }

    public async Task MarkAsReadByEducatorAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_mark_parent_communications_as_read",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }
}

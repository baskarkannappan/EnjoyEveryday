using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ParentRepository : IParentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ParentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ParentAuthorizedChild>> GetAuthorizedChildrenAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ParentAuthorizedChild>(
            "fn_get_parent_authorized_children",
            new { TenantId = tenantId, UserId = userId },
            cancellationToken: cancellationToken);
    }

    public async Task<bool> HasChildAccessAsync(Guid tenantId, Guid userId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarFunctionAsync<bool>(
            "fn_has_parent_child_access",
            new { TenantId = tenantId, UserId = userId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<ChildAttendance?> GetChildAttendanceForDateAsync(Guid tenantId, Guid childId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<ChildAttendance>(
            "fn_get_child_attendance_for_date",
            new { ChildId = childId, Date = DateOnly.FromDateTime(date) },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildJourneyEntry>> GetApprovedJourneyEntriesAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var entries = (await connection.QueryFunctionAsync<ChildJourneyEntry>(
            "fn_get_parent_approved_journey_entries",
            new { ChildId = childId },
            cancellationToken: cancellationToken)).ToList();

        if (entries.Any())
        {
            var consentStatus = await connection.ExecuteScalarFunctionAsync<string?>(
                "fn_get_parent_child_photo_consent",
                new { ChildId = childId },
                cancellationToken: cancellationToken);
            // Allow photos when signed OR when no Photo/Media consent document has been created yet;
            // only withhold if a Photo/Media consent document exists and is not Signed (e.g. Pending/Declined/Revoked).
            bool hasPhotoConsent = string.IsNullOrWhiteSpace(consentStatus)
                                || string.Equals(consentStatus, "Signed", StringComparison.OrdinalIgnoreCase);

            if (hasPhotoConsent)
            {
                var entryIds = entries.Select(e => e.Id).ToArray();
                var evidenceRecords = await connection.QueryFunctionAsync<dynamic>(
                    "fn_get_parent_journey_evidence",
                    new { EntryIds = entryIds },
                    cancellationToken: cancellationToken);

                var photoLookup = evidenceRecords.GroupBy(x => (Guid)x.journey_entry_id)
                    .ToDictionary(g => g.Key, g => g.Select(x => (string)x.text_content).ToList());

                foreach (var entry in entries)
                {
                    if (photoLookup.TryGetValue(entry.Id, out var photos))
                    {
                        entry.Photos = photos;
                    }
                }
            }
        }

        return entries;
    }

    public async Task<IEnumerable<ChildMilestone>> GetApprovedMilestonesAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ChildMilestone>(
            "fn_get_parent_approved_milestones",
            new { ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Event>> GetApprovedEventsForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<Event>(
            "fn_get_parent_approved_events_for_child",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<Person?> GetPersonByUserIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<Person>(
            "fn_get_person_by_user_id",
            new { TenantId = tenantId, UserId = userId },
            cancellationToken: cancellationToken);
    }

    public async Task UpdatePersonContactAsync(Guid tenantId, Guid personId, string? mobilePhone, string? preferredContactMethod, string? preferredLanguage, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_update_person_contact",
            new
            {
                TenantId = tenantId,
                PersonId = personId,
                MobilePhone = mobilePhone,
                PreferredContactMethod = preferredContactMethod,
                PreferredLanguage = preferredLanguage
            },
            cancellationToken: cancellationToken);
    }

    public async Task<bool> IsPrivacyAndTrustApprovedAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarFunctionAsync<bool>(
            "fn_is_privacy_and_trust_approved",
            new { p_tenant_id = tenantId, p_child_id = childId },
            cancellationToken: cancellationToken);
    }

    public async Task ApprovePrivacyAndTrustAsync(Guid tenantId, Guid childId, string approvedByName, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_approve_privacy_and_trust",
            new { p_tenant_id = tenantId, p_child_id = childId, p_approved_by_name = approvedByName },
            cancellationToken: cancellationToken);
    }

    public async Task<DateTimeOffset?> GetPrivacyAndTrustApprovalDateAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var dt = await connection.ExecuteScalarFunctionAsync<DateTime?>(
            "fn_get_privacy_and_trust_approval_date",
            new { p_tenant_id = tenantId, p_child_id = childId },
            cancellationToken: cancellationToken);

        if (dt.HasValue)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc));
        }
        return null;
    }
}

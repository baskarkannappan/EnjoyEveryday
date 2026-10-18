using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ChildDevelopmentPortfolioRepository : IChildDevelopmentPortfolioRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChildDevelopmentPortfolioRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<PortfolioConsentDocumentRecord?> GetLatestMediaConsentDocumentAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT d.id AS Id, d.status AS Status, d.document_title AS DocumentTitle,
                   d.signed_at AS SignedAt, d.withdrawn_at AS WithdrawnAt, d.declined_at AS DeclinedAt,
                   COALESCE(t.document_type, '') AS DocumentType
            FROM child_consent_documents d
            LEFT JOIN consent_templates t ON d.consent_template_id = t.id
            WHERE d.tenant_id = @TenantId AND d.child_id = @ChildId
              AND (t.document_type = 'PhotoVideoConsent' OR d.document_title ILIKE '%Photo%' OR d.document_title ILIKE '%Media%')
            ORDER BY d.created_at DESC
            LIMIT 1";

        return await conn.QueryFirstOrDefaultAsync<PortfolioConsentDocumentRecord>(
            new CommandDefinition(sql, new { TenantId = tenantId, ChildId = childId }, cancellationToken: ct));
    }

    public async Task<List<string>> GetConsentChoicesAsync(Guid tenantId, Guid consentDocumentId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT choice FROM consent_choices 
            WHERE tenant_id = @TenantId AND consent_document_id = @DocId
              AND (section_key ILIKE '%photo%' OR section_key ILIKE '%media%')";

        var choices = await conn.QueryAsync<string>(
            new CommandDefinition(sql, new { TenantId = tenantId, DocId = consentDocumentId }, cancellationToken: ct));

        return choices.ToList();
    }

    public async Task<PortfolioChildProfileRecord?> GetChildProfileAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT c.id AS Id, c.first_name AS FirstName, c.last_name AS LastName,
                   c.photo_url AS PhotoUrl, c.date_of_birth AS DateOfBirth,
                   cr.name AS ClassroomName, b.name AS BranchName
            FROM children c
            LEFT JOIN classrooms cr ON c.classroom_id = cr.id
            LEFT JOIN branches b ON cr.branch_id = b.id
            WHERE c.tenant_id = @TenantId AND c.id = @ChildId";

        return await conn.QueryFirstOrDefaultAsync<PortfolioChildProfileRecord>(
            new CommandDefinition(sql, new { TenantId = tenantId, ChildId = childId }, cancellationToken: ct));
    }

    public async Task<List<DevelopmentAreaRecord>> GetDevelopmentAreasAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT id AS Id, name AS Name, icon AS Icon, display_order AS DisplayOrder
            FROM development_areas
            WHERE (tenant_id IS NULL OR tenant_id = @TenantId) AND is_active = TRUE
            ORDER BY display_order";

        var results = await conn.QueryAsync<DevelopmentAreaRecord>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));

        return results.ToList();
    }

    public async Task<List<PortfolioJourneyEntryRecord>> GetJourneyEntriesAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT je.id AS Id, je.observation_date AS ObservationDate, je.journey_type AS JourneyType,
                   je.title AS Title, je.observation AS Observation, je.child_voice AS ChildVoice,
                   je.teacher_reflection AS TeacherReflection, je.is_milestone AS IsMilestone,
                   je.is_parent_visible AS IsParentVisible, je.parent_visibility_status AS ParentVisibilityStatus,
                   COALESCE(t.first_name || ' ' || t.last_name, 'Teacher') AS TeacherName
            FROM child_journey_entries je
            LEFT JOIN teachers t ON je.teacher_id = t.id
            WHERE je.tenant_id = @TenantId AND je.child_id = @ChildId
            ORDER BY je.observation_date DESC";

        var results = await conn.QueryAsync<PortfolioJourneyEntryRecord>(
            new CommandDefinition(sql, new { TenantId = tenantId, ChildId = childId }, cancellationToken: ct));

        return results.ToList();
    }

    public async Task<List<EntryDevelopmentAreaRecord>> GetEntryDevelopmentAreasAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT d.journey_entry_id AS JourneyEntryId, d.development_area_id AS DevelopmentAreaId,
                   a.name AS Name, a.icon AS Icon
            FROM child_journey_developments d
            JOIN development_areas a ON d.development_area_id = a.id
            WHERE d.journey_entry_id IN (
                SELECT id FROM child_journey_entries WHERE tenant_id = @TenantId AND child_id = @ChildId
            )";

        var results = await conn.QueryAsync<EntryDevelopmentAreaRecord>(
            new CommandDefinition(sql, new { TenantId = tenantId, ChildId = childId }, cancellationToken: ct));

        return results.ToList();
    }

    public async Task<List<EntryEvidenceRecord>> GetEntryEvidenceAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT journey_entry_id AS JourneyEntryId, text_content AS TextContent
            FROM child_journey_evidence
            WHERE journey_entry_id IN (
                SELECT id FROM child_journey_entries WHERE tenant_id = @TenantId AND child_id = @ChildId
            )
            ORDER BY created_at DESC";

        var results = await conn.QueryAsync<EntryEvidenceRecord>(
            new CommandDefinition(sql, new { TenantId = tenantId, ChildId = childId }, cancellationToken: ct));

        return results.ToList();
    }

    public async Task<List<PortfolioMilestoneRecord>> GetMilestonesAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT m.id AS Id, m.milestone_date AS MilestoneDate, m.title AS Title,
                   m.description AS Description, m.evidence_summary AS EvidenceSummary,
                   m.parent_visible AS ParentVisible, m.status AS Status,
                   m.development_area_id AS DevelopmentAreaId,
                   COALESCE(t.first_name || ' ' || t.last_name, 'Teacher') AS TeacherName
            FROM child_milestones m
            LEFT JOIN teachers t ON m.teacher_id = t.id
            WHERE m.tenant_id = @TenantId AND m.child_id = @ChildId
            ORDER BY m.milestone_date DESC";

        var results = await conn.QueryAsync<PortfolioMilestoneRecord>(
            new CommandDefinition(sql, new { TenantId = tenantId, ChildId = childId }, cancellationToken: ct));

        return results.ToList();
    }

    public async Task<List<PortfolioExperienceParticipationRecord>> GetExperienceParticipationsAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT p.id AS Id,
                   s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::time) AS SessionDate,
                   e.title AS ExperienceTitle,
                   COALESCE(p.participation_notes, s.actual_notes) AS Notes,
                   COALESCE(t.first_name || ' ' || t.last_name, 'Teacher') AS TeacherName
            FROM experience_participations p
            JOIN experience_sessions s ON p.experience_session_id = s.id
            JOIN experiences e ON s.experience_id = e.id
            LEFT JOIN teachers t ON s.primary_teacher_id = t.id
            WHERE s.tenant_id = @TenantId AND p.child_id = @ChildId AND p.participation_status = 'Participated'
            ORDER BY s.session_date DESC";

        var results = await conn.QueryAsync<PortfolioExperienceParticipationRecord>(
            new CommandDefinition(sql, new { TenantId = tenantId, ChildId = childId }, cancellationToken: ct));

        return results.ToList();
    }
}

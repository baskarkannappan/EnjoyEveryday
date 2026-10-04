using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ChildJourneyRepository : IChildJourneyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChildJourneyRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ChildJourneyEntry?> GetJourneyEntryByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = @"
            SELECT * FROM child_journey_entries 
            WHERE id = @Id AND tenant_id = @TenantId";
            
        return await connection.QuerySingleOrDefaultAsync<ChildJourneyEntry>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<ChildJourneyEntry>> GetJourneyEntriesForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = @"
            SELECT * FROM child_journey_entries 
            WHERE tenant_id = @TenantId AND child_id = @ChildId
            ORDER BY observation_date DESC";
            
        return await connection.QueryAsync<ChildJourneyEntry>(sql, new { TenantId = tenantId, ChildId = childId });
    }

    public async Task CreateJourneyEntryAsync(ChildJourneyEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = @"
            INSERT INTO child_journey_entries (
                id, tenant_id, child_id, enrollment_id, classroom_id, teacher_id, 
                experience_id, experience_schedule_id, experience_session_id, experience_participation_id, 
                little_moment_id, milestone_id, 
                journey_type, title, observation, child_voice, teacher_reflection, 
                observation_date, participation_type, is_milestone, is_parent_visible, 
                parent_visibility_status, created_at, created_by, updated_at, updated_by, version
            ) VALUES (
                @Id, @TenantId, @ChildId, @EnrollmentId, @ClassroomId, @TeacherId, 
                @ExperienceId, @ExperienceScheduleId, @ExperienceSessionId, @ExperienceParticipationId, 
                @LittleMomentId, @MilestoneId, 
                @JourneyType, @Title, @Observation, @ChildVoice, @TeacherReflection, 
                @ObservationDate, @ParticipationType, @IsMilestone, @IsParentVisible, 
                @ParentVisibilityStatus, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy, @Version
            )";
            
        await connection.ExecuteAsync(sql, entry);
    }

    public async Task UpdateJourneyEntryAsync(ChildJourneyEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = @"
            UPDATE child_journey_entries SET
                journey_type = @JourneyType,
                title = @Title,
                observation = @Observation,
                child_voice = @ChildVoice,
                teacher_reflection = @TeacherReflection,
                observation_date = @ObservationDate,
                participation_type = @ParticipationType,
                is_milestone = @IsMilestone,
                is_parent_visible = @IsParentVisible,
                parent_visibility_status = @ParentVisibilityStatus,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy,
                version = version + 1
            WHERE id = @Id AND tenant_id = @TenantId";
            
        await connection.ExecuteAsync(sql, entry);
    }

    public async Task DeleteJourneyEntryAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = "DELETE FROM child_journey_entries WHERE id = @Id AND tenant_id = @TenantId";
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<DevelopmentArea>> GetAllDevelopmentAreasAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = @"
            SELECT * FROM development_areas 
            WHERE (tenant_id IS NULL OR tenant_id = @TenantId) AND is_active = TRUE
            ORDER BY display_order";
            
        return await connection.QueryAsync<DevelopmentArea>(sql, new { TenantId = tenantId });
    }

    public async Task AddJourneyDevelopmentsAsync(Guid journeyEntryId, IEnumerable<Guid> developmentAreaIds, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        // First delete existing mappings
        var deleteSql = "DELETE FROM child_journey_developments WHERE journey_entry_id = @JourneyEntryId";
        await connection.ExecuteAsync(deleteSql, new { JourneyEntryId = journeyEntryId });
        
        // Then insert new ones
        var insertSql = @"
            INSERT INTO child_journey_developments (journey_entry_id, development_area_id) 
            VALUES (@JourneyEntryId, @DevelopmentAreaId)";
            
        var parameters = developmentAreaIds.Select(id => new { JourneyEntryId = journeyEntryId, DevelopmentAreaId = id });
        await connection.ExecuteAsync(insertSql, parameters);
    }

    public async Task<IEnumerable<Guid>> GetDevelopmentAreasForJourneyEntryAsync(Guid journeyEntryId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = "SELECT development_area_id FROM child_journey_developments WHERE journey_entry_id = @JourneyEntryId";
        return await connection.QueryAsync<Guid>(sql, new { JourneyEntryId = journeyEntryId });
    }

    public async Task CreateJourneyEvidenceAsync(ChildJourneyEvidence evidence, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = @"
            INSERT INTO child_journey_evidence (
                id, journey_entry_id, evidence_type, media_id, text_content, 
                captured_at, captured_by, visibility, created_at
            ) VALUES (
                @Id, @JourneyEntryId, @EvidenceType, @MediaId, @TextContent, 
                @CapturedAt, @CapturedBy, @Visibility, @CreatedAt
            )";
            
        await connection.ExecuteAsync(sql, evidence);
    }

    public async Task<IEnumerable<ChildJourneyEvidence>> GetEvidenceForJourneyEntryAsync(Guid journeyEntryId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = "SELECT * FROM child_journey_evidence WHERE journey_entry_id = @JourneyEntryId ORDER BY created_at";
        return await connection.QueryAsync<ChildJourneyEvidence>(sql, new { JourneyEntryId = journeyEntryId });
    }

    public async Task CreateMilestoneAsync(ChildMilestone milestone, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = @"
            INSERT INTO child_milestones (
                id, tenant_id, child_id, classroom_id, teacher_id, title, description, 
                milestone_date, development_area_id, evidence_summary, journey_entry_id, 
                parent_visible, status, created_at, created_by, updated_at, updated_by
            ) VALUES (
                @Id, @TenantId, @ChildId, @ClassroomId, @TeacherId, @Title, @Description, 
                @MilestoneDate, @DevelopmentAreaId, @EvidenceSummary, @JourneyEntryId, 
                @ParentVisible, @Status, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy
            )";
            
        await connection.ExecuteAsync(sql, milestone);
    }

    public async Task<IEnumerable<ChildMilestone>> GetMilestonesForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var sql = @"
            SELECT * FROM child_milestones 
            WHERE tenant_id = @TenantId AND child_id = @ChildId
            ORDER BY milestone_date DESC";
            
        return await connection.QueryAsync<ChildMilestone>(sql, new { TenantId = tenantId, ChildId = childId });
    }
}

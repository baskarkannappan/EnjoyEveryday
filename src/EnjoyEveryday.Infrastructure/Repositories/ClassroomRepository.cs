using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ClassroomRepository : IClassroomRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ClassroomRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectFields = @"
        id, tenant_id as TenantId, branch_id as BranchId, name, display_name as DisplayName, short_name as ShortName, 
        code, description, classroom_type as ClassroomType, status, age_group as AgeGroup, 
        min_age_months as MinAgeMonths, max_age_months as MaxAgeMonths, capacity, 
        current_enrollment as CurrentEnrollment, environment, photo_url as PhotoUrl, 
        draft_data as DraftData, profile_completion_percentage as ProfileCompletionPercentage, 
        is_active as IsActive, created_at as CreatedAt, created_by as CreatedBy, 
        updated_at as UpdatedAt, updated_by as UpdatedBy";

    public async Task<Classroom?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        string sql = $@"
            SELECT {SelectFields}
            FROM classrooms
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Classroom>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<Classroom>> GetByBranchIdAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        string sql = $@"
            SELECT {SelectFields}
            FROM classrooms
            WHERE branch_id = @BranchId AND tenant_id = @TenantId
            ORDER BY name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Classroom>(sql, new { BranchId = branchId, TenantId = tenantId });
    }

    public async Task<IEnumerable<Classroom>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        string sql = $@"
            SELECT {SelectFields}
            FROM classrooms
            WHERE tenant_id = @TenantId
            ORDER BY name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Classroom>(sql, new { TenantId = tenantId });
    }

    public async Task<Classroom> AddAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO classrooms (
                id, tenant_id, branch_id, name, display_name, short_name, code, description, 
                classroom_type, status, age_group, min_age_months, max_age_months, capacity, 
                current_enrollment, environment, photo_url, draft_data, profile_completion_percentage, 
                is_active, created_at, created_by, updated_at, updated_by
            )
            VALUES (
                @Id, @TenantId, @BranchId, @Name, @DisplayName, @ShortName, @Code, @Description, 
                @ClassroomType, @Status, @AgeGroup, @MinAgeMonths, @MaxAgeMonths, @Capacity, 
                @CurrentEnrollment, @Environment, @PhotoUrl, @DraftData::jsonb, @ProfileCompletionPercentage, 
                @IsActive, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy
            )
            RETURNING id;";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, classroom);
        return classroom;
    }

    public async Task UpdateAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE classrooms
            SET name = @Name,
                display_name = @DisplayName,
                short_name = @ShortName,
                code = @Code,
                description = @Description,
                classroom_type = @ClassroomType,
                status = @Status,
                age_group = @AgeGroup,
                min_age_months = @MinAgeMonths,
                max_age_months = @MaxAgeMonths,
                capacity = @Capacity,
                current_enrollment = @CurrentEnrollment,
                environment = @Environment,
                photo_url = @PhotoUrl,
                draft_data = @DraftData::jsonb,
                profile_completion_percentage = @ProfileCompletionPercentage,
                is_active = @IsActive,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, classroom);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM classrooms
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }
}

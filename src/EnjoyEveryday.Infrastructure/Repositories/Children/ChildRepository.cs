using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ChildRepository : IChildRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChildRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Child?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, first_name, last_name, date_of_birth, classroom_id, photo_url, profile_completion_percentage, enrollment_status, draft_data, is_active, created_at, updated_at
            FROM children
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Child>(sql, new { Id = id, TenantId = tenantId });
    }

    public Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => GetAllAsync(tenantId, null, cancellationToken);

    public async Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT c.id, c.tenant_id, c.first_name, c.last_name, c.date_of_birth, c.classroom_id, 
                   c.photo_url, c.profile_completion_percentage, c.enrollment_status, c.draft_data, 
                   c.is_active, c.created_at, c.updated_at
            FROM children c
            LEFT JOIN classrooms cl ON c.classroom_id = cl.id AND c.tenant_id = cl.tenant_id
            LEFT JOIN branches b ON cl.branch_id = b.id AND cl.tenant_id = b.tenant_id
            WHERE c.tenant_id = @TenantId AND c.is_active = true
              AND (@OrganizationId IS NULL 
                   OR b.organization_id = @OrganizationId 
                   OR EXISTS (
                       SELECT 1 FROM enrollments e 
                       JOIN branches eb ON e.branch_id = eb.id 
                       WHERE e.child_id = c.id AND eb.organization_id = @OrganizationId
                   ))
            ORDER BY c.first_name, c.last_name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Child>(sql, new { TenantId = tenantId, OrganizationId = organizationId });
    }

    public async Task<IEnumerable<Child>> GetByClassroomIdAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, first_name, last_name, date_of_birth, classroom_id, photo_url, profile_completion_percentage, enrollment_status, draft_data, is_active, created_at, updated_at
            FROM children
            WHERE classroom_id = @ClassroomId AND tenant_id = @TenantId AND is_active = true
            ORDER BY first_name, last_name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Child>(sql, new { ClassroomId = classroomId, TenantId = tenantId });
    }

    public async Task<Child> AddAsync(Child child, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO children (id, tenant_id, first_name, last_name, date_of_birth, classroom_id, photo_url, profile_completion_percentage, enrollment_status, draft_data, is_active, created_at, updated_at)
            VALUES (@Id, @TenantId, @FirstName, @LastName, @DateOfBirth, @ClassroomId, @PhotoUrl, @ProfileCompletionPercentage, @EnrollmentStatus, @DraftData, @IsActive, @CreatedAt, @UpdatedAt)
            RETURNING id, tenant_id, first_name, last_name, date_of_birth, classroom_id, photo_url, profile_completion_percentage, enrollment_status, draft_data, is_active, created_at, updated_at";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Child>(sql, child);
    }

    public async Task UpdateAsync(Child child, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE children
            SET first_name = @FirstName,
                last_name = @LastName,
                date_of_birth = @DateOfBirth,
                classroom_id = @ClassroomId,
                photo_url = @PhotoUrl,
                profile_completion_percentage = @ProfileCompletionPercentage,
                enrollment_status = @EnrollmentStatus,
                draft_data = @DraftData,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, child);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE children SET is_active = false
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }
}

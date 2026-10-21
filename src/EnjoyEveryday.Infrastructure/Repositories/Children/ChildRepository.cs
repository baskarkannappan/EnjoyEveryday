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

    public async Task SyncParentGuardianAsync(Guid tenantId, Guid childId, string parentEmail, string parentName, string relationship, string phone, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(parentEmail)) return;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        string trimmedEmail = parentEmail.Trim();
        string normEmail = trimmedEmail.ToUpperInvariant();

        // 1. Find or create User
        var existingUserId = await connection.ExecuteScalarAsync<Guid?>(
            "SELECT id FROM users WHERE normalized_email = @NormEmail LIMIT 1",
            new { NormEmail = normEmail });

        Guid userId;
        if (existingUserId.HasValue && existingUserId.Value != Guid.Empty)
        {
            userId = existingUserId.Value;
        }
        else
        {
            userId = Guid.NewGuid();
            var names = (parentName ?? "Parent Guardian").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string first = names.Length > 0 ? names[0] : "Parent";
            string last = names.Length > 1 ? names[1] : "Guardian";

            await connection.ExecuteAsync(@"
                INSERT INTO users (
                    id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp, is_active, created_at, updated_at
                ) VALUES (
                    @UserId, @TenantId, @Email, @NormEmail, TRUE, @FirstName, @LastName, @DisplayName, '', @SecurityStamp, TRUE, NOW(), NOW()
                ) ON CONFLICT (normalized_email) DO NOTHING;",
                new {
                    UserId = userId,
                    TenantId = tenantId,
                    Email = trimmedEmail,
                    NormEmail = normEmail,
                    FirstName = first,
                    LastName = last,
                    DisplayName = string.IsNullOrWhiteSpace(parentName) ? $"{first} {last}" : parentName.Trim(),
                    SecurityStamp = Guid.NewGuid().ToString("N")
                });
                
            // Re-fetch in case of conflict race
            var confirmedId = await connection.ExecuteScalarAsync<Guid?>(
                "SELECT id FROM users WHERE normalized_email = @NormEmail LIMIT 1",
                new { NormEmail = normEmail });
            if (confirmedId.HasValue) userId = confirmedId.Value;

            // Assign Parent role
            var roleId = await connection.ExecuteScalarAsync<Guid?>(
                "SELECT id FROM roles WHERE name = 'Parent' AND (tenant_id = @TenantId OR tenant_id IS NULL) LIMIT 1",
                new { TenantId = tenantId });
            if (roleId.HasValue)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO user_roles (user_id, role_id)
                    VALUES (@UserId, @RoleId) ON CONFLICT DO NOTHING;",
                    new { UserId = userId, RoleId = roleId.Value });
            }
        }

        // 2. Find or create Person
        var personId = await connection.ExecuteScalarAsync<Guid?>(
            "SELECT id FROM persons WHERE tenant_id = @TenantId AND user_id = @UserId LIMIT 1",
            new { TenantId = tenantId, UserId = userId });

        if (!personId.HasValue)
        {
            personId = Guid.NewGuid();
            var names = (parentName ?? "Parent Guardian").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string first = names.Length > 0 ? names[0] : "Parent";
            string last = names.Length > 1 ? names[1] : "Guardian";

            await connection.ExecuteAsync(@"
                INSERT INTO persons (
                    id, tenant_id, user_id, first_name, last_name, relationship, email, mobile_phone, has_portal_access, receives_daily_updates, receives_photos, created_at, updated_at
                ) VALUES (
                    @PersonId, @TenantId, @UserId, @FirstName, @LastName, @Relationship, @Email, @Phone, TRUE, TRUE, TRUE, NOW(), NOW()
                );",
                new {
                    PersonId = personId.Value,
                    TenantId = tenantId,
                    UserId = userId,
                    FirstName = first,
                    LastName = last,
                    Relationship = string.IsNullOrWhiteSpace(relationship) ? "Parent" : relationship.Trim(),
                    Email = trimmedEmail,
                    Phone = phone?.Trim()
                });
        }
        else if (!string.IsNullOrWhiteSpace(phone))
        {
            await connection.ExecuteAsync(@"
                UPDATE persons SET mobile_phone = @Phone, updated_at = NOW() WHERE id = @PersonId;",
                new { PersonId = personId.Value, Phone = phone.Trim() });
        }

        // 3. Link child_guardians
        await connection.ExecuteAsync(@"
            INSERT INTO child_guardians (
                id, tenant_id, child_id, person_id, relationship, is_primary, has_legal_custody, can_pickup, can_receive_information, can_authorize_medical_care
            ) VALUES (
                gen_random_uuid(), @TenantId, @ChildId, @PersonId, @Relationship, TRUE, TRUE, TRUE, TRUE, TRUE
            ) ON CONFLICT (child_id, person_id) DO UPDATE SET 
                relationship = EXCLUDED.relationship, 
                can_pickup = TRUE;",
            new {
                TenantId = tenantId,
                ChildId = childId,
                PersonId = personId.Value,
                Relationship = string.IsNullOrWhiteSpace(relationship) ? "Parent" : relationship.Trim()
            });

        // 4. Link family_relationships
        await connection.ExecuteAsync(@"
            INSERT INTO family_relationships (
                id, tenant_id, child_id, user_id, relationship, is_primary, created_at, status, verified_at
            ) VALUES (
                gen_random_uuid(), @TenantId, @ChildId, @UserId, @Relationship, TRUE, NOW(), 'Verified', NOW()
            ) ON CONFLICT (child_id, user_id) DO UPDATE SET 
                relationship = EXCLUDED.relationship,
                status = 'Verified',
                revoked_at = NULL,
                verified_at = NOW();",
            new {
                TenantId = tenantId,
                ChildId = childId,
                UserId = userId,
                Relationship = string.IsNullOrWhiteSpace(relationship) ? "Parent" : relationship.Trim()
            });

        // 5. Link authorized_pickups
        await connection.ExecuteAsync(@"
            INSERT INTO authorized_pickups (
                id, tenant_id, child_id, person_id, relationship, is_active, notes
            ) VALUES (
                gen_random_uuid(), @TenantId, @ChildId, @PersonId, @Relationship, TRUE, 'Primary guardian on file.'
            ) ON CONFLICT DO NOTHING;",
            new {
                TenantId = tenantId,
                ChildId = childId,
                PersonId = personId.Value,
                Relationship = string.IsNullOrWhiteSpace(relationship) ? "Parent" : relationship.Trim()
            });
    }
}

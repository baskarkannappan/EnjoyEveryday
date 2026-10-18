using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TeacherRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Teacher?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id as TenantId, person_id as PersonId, user_id as UserId, employee_number as EmployeeNumber, 
                   first_name as FirstName, middle_name as MiddleName, last_name as LastName, preferred_name as PreferredName, 
                   display_name as DisplayName, profile_photo_url as ProfilePhotoUrl, date_of_birth as DateOfBirth, gender as Gender, 
                   preferred_language as PreferredLanguage, other_languages as OtherLanguages, pronouns as Pronouns, bio as Bio, 
                   status as Status, draft_data as DraftData, profile_completion_percentage as ProfileCompletionPercentage, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM teachers
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Teacher>(sql, new { Id = id, TenantId = tenantId });
    }

    public Task<IEnumerable<Teacher>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => GetAllAsync(tenantId, null, cancellationToken);

    public async Task<IEnumerable<Teacher>> GetAllAsync(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT t.id, t.tenant_id as TenantId, t.person_id as PersonId, t.user_id as UserId, t.employee_number as EmployeeNumber, 
                   t.first_name as FirstName, t.middle_name as MiddleName, t.last_name as LastName, t.preferred_name as PreferredName, 
                   t.display_name as DisplayName, t.profile_photo_url as ProfilePhotoUrl, t.date_of_birth as DateOfBirth, t.gender as Gender, 
                   t.preferred_language as PreferredLanguage, t.other_languages as OtherLanguages, t.pronouns as Pronouns, t.bio as Bio, 
                   t.status as Status, t.draft_data as DraftData, t.profile_completion_percentage as ProfileCompletionPercentage, 
                   t.created_at as CreatedAt, t.updated_at as UpdatedAt
            FROM teachers t
            WHERE t.tenant_id = @TenantId AND t.status != 'Inactive'
              AND (@OrganizationId IS NULL OR (
                  (t.user_id IS NOT NULL AND EXISTS (
                      SELECT 1 FROM organization_memberships om 
                      WHERE om.user_id = t.user_id AND om.organization_id = @OrganizationId AND om.status = 'Active'
                  ))
                  OR EXISTS (
                      SELECT 1 FROM teacher_employments te
                      JOIN branches tb ON te.branch_id = tb.id
                      WHERE te.teacher_id = t.id AND tb.organization_id = @OrganizationId
                  )
              ))
            ORDER BY t.first_name, t.last_name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Teacher>(sql, new { TenantId = tenantId, OrganizationId = organizationId });
    }

    public async Task<Teacher> AddAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO teachers (
                id, tenant_id, person_id, user_id, employee_number, first_name, middle_name, last_name, 
                preferred_name, display_name, profile_photo_url, date_of_birth, gender, preferred_language, 
                other_languages, pronouns, bio, status, draft_data, profile_completion_percentage, 
                created_at, updated_at)
            VALUES (
                @Id, @TenantId, @PersonId, @UserId, @EmployeeNumber, @FirstName, @MiddleName, @LastName, 
                @PreferredName, @DisplayName, @ProfilePhotoUrl, @DateOfBirth, @Gender, @PreferredLanguage, 
                @OtherLanguages, @Pronouns, @Bio, @Status, @DraftData::jsonb, @ProfileCompletionPercentage, 
                @CreatedAt, @UpdatedAt)
            RETURNING id, tenant_id as TenantId, person_id as PersonId, user_id as UserId, employee_number as EmployeeNumber, 
                   first_name as FirstName, middle_name as MiddleName, last_name as LastName, preferred_name as PreferredName, 
                   display_name as DisplayName, profile_photo_url as ProfilePhotoUrl, date_of_birth as DateOfBirth, gender as Gender, 
                   preferred_language as PreferredLanguage, other_languages as OtherLanguages, pronouns as Pronouns, bio as Bio, 
                   status as Status, draft_data as DraftData, profile_completion_percentage as ProfileCompletionPercentage, 
                   created_at as CreatedAt, updated_at as UpdatedAt";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Teacher>(sql, new {
            Id = teacher.Id,
            TenantId = teacher.TenantId,
            PersonId = teacher.PersonId,
            UserId = teacher.UserId,
            EmployeeNumber = teacher.EmployeeNumber,
            FirstName = teacher.FirstName,
            MiddleName = teacher.MiddleName,
            LastName = teacher.LastName,
            PreferredName = teacher.PreferredName,
            DisplayName = teacher.DisplayName,
            ProfilePhotoUrl = teacher.ProfilePhotoUrl,
            DateOfBirth = teacher.DateOfBirth,
            Gender = teacher.Gender,
            PreferredLanguage = teacher.PreferredLanguage,
            OtherLanguages = teacher.OtherLanguages,
            Pronouns = teacher.Pronouns,
            Bio = teacher.Bio,
            Status = teacher.Status,
            DraftData = teacher.DraftData,
            ProfileCompletionPercentage = teacher.ProfileCompletionPercentage,
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt
        });
    }

    public async Task UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE teachers
            SET person_id = @PersonId,
                user_id = @UserId,
                employee_number = @EmployeeNumber,
                first_name = @FirstName,
                middle_name = @MiddleName,
                last_name = @LastName,
                preferred_name = @PreferredName,
                display_name = @DisplayName,
                profile_photo_url = @ProfilePhotoUrl,
                date_of_birth = @DateOfBirth,
                gender = @Gender,
                preferred_language = @PreferredLanguage,
                other_languages = @OtherLanguages,
                pronouns = @Pronouns,
                bio = @Bio,
                status = @Status,
                draft_data = @DraftData::jsonb,
                profile_completion_percentage = @ProfileCompletionPercentage,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new {
            Id = teacher.Id,
            TenantId = teacher.TenantId,
            PersonId = teacher.PersonId,
            UserId = teacher.UserId,
            EmployeeNumber = teacher.EmployeeNumber,
            FirstName = teacher.FirstName,
            MiddleName = teacher.MiddleName,
            LastName = teacher.LastName,
            PreferredName = teacher.PreferredName,
            DisplayName = teacher.DisplayName,
            ProfilePhotoUrl = teacher.ProfilePhotoUrl,
            DateOfBirth = teacher.DateOfBirth,
            Gender = teacher.Gender,
            PreferredLanguage = teacher.PreferredLanguage,
            OtherLanguages = teacher.OtherLanguages,
            Pronouns = teacher.Pronouns,
            Bio = teacher.Bio,
            Status = teacher.Status,
            DraftData = teacher.DraftData,
            ProfileCompletionPercentage = teacher.ProfileCompletionPercentage,
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt
        });
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE teachers SET status = 'Inactive'
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<bool> IsTeacherAssignedToClassroomAsync(
        Guid tenantId,
        Guid classroomId,
        Guid teacherId,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM classroom_teachers 
            WHERE tenant_id = @TenantId 
              AND classroom_id = @ClassroomId 
              AND (user_id = @TeacherId OR (@UserId IS NOT NULL AND user_id = @UserId))";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, ClassroomId = classroomId, TeacherId = teacherId, UserId = userId });
        return count > 0;
    }
}

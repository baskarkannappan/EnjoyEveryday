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

    public async Task<IEnumerable<Teacher>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id as TenantId, person_id as PersonId, user_id as UserId, employee_number as EmployeeNumber, 
                   first_name as FirstName, middle_name as MiddleName, last_name as LastName, preferred_name as PreferredName, 
                   display_name as DisplayName, profile_photo_url as ProfilePhotoUrl, date_of_birth as DateOfBirth, gender as Gender, 
                   preferred_language as PreferredLanguage, other_languages as OtherLanguages, pronouns as Pronouns, bio as Bio, 
                   status as Status, draft_data as DraftData, profile_completion_percentage as ProfileCompletionPercentage, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM teachers
            WHERE tenant_id = @TenantId AND status != 'Inactive'
            ORDER BY first_name, last_name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Teacher>(sql, new { TenantId = tenantId });
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
}

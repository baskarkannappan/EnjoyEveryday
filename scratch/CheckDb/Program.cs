using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

public class Program 
{
    public static async Task Main() 
    {
        var connectionString = "Host=localhost;Port=5433;Database=enjoyeveryday;Username=postgres;Password=postgres;Include Error Detail=true";
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var teacher = new {
            Id = Guid.NewGuid(),
            TenantId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            PersonId = (Guid?)null,
            UserId = Guid.Parse("50000000-0000-0000-0000-000000000001"),
            EmployeeNumber = "EMP-001",
            FirstName = "Test",
            MiddleName = "",
            LastName = "Teacher",
            PreferredName = "Test",
            DisplayName = "Test Teacher",
            ProfilePhotoUrl = (string)null,
            DateOfBirth = (DateTime?)null,
            Gender = "",
            PreferredLanguage = "English",
            OtherLanguages = "",
            Pronouns = "",
            Bio = (string)null,
            Status = "Draft",
            DraftData = "{}",
            ProfileCompletionPercentage = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

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
            RETURNING id";

        try 
        {
            var id = await connection.QuerySingleAsync<Guid>(sql, teacher);
            Console.WriteLine($"Successfully inserted teacher with ID {id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting teacher:");
            Console.WriteLine(ex.ToString());
        }
    }
}

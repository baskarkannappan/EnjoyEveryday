using System;
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

        var sql = @"
            UPDATE experience_feedback f
            SET experience_id = s.experience_id
            FROM experience_schedules s
            WHERE f.experience_schedule_id = s.id AND f.experience_id IS NULL;
        ";
        
        var rows = await connection.ExecuteAsync(sql);
        Console.WriteLine($"Updated {rows} feedback rows.");
    }
}

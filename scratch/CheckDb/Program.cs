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

        try 
        {
            await connection.ExecuteAsync("ALTER TABLE experience_feedback ADD COLUMN IF NOT EXISTS stars INT NOT NULL DEFAULT 5;");
            Console.WriteLine("Successfully added stars column.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

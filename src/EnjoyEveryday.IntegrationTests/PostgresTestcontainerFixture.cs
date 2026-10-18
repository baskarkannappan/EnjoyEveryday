using System;
using System.IO;
using System.Threading.Tasks;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace EnjoyEveryday.IntegrationTests;

/// <summary>
/// Reusable test fixture for integration testing against real PostgreSQL instances via Testcontainers.
/// </summary>
public abstract class PostgresTestcontainerFixture
{
    private PostgreSqlContainer? _postgresContainer;
    protected IDbConnectionFactory ConnectionFactory { get; private set; } = null!;
    protected string ConnectionString { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public virtual async Task GlobalSetUpAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("enjoyeveryday_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        try
        {
            await _postgresContainer.StartAsync();
            ConnectionString = _postgresContainer.GetConnectionString();
            ConnectionFactory = new NpgsqlConnectionFactory(ConnectionString);
        }
        catch (Exception ex)
        {
            // If Docker engine is unavailable in the environment, mark test ignored rather than failing compilation
            Assert.Ignore($"Docker or Testcontainers engine unavailable: {ex.Message}");
        }
    }

    [OneTimeTearDown]
    public virtual async Task GlobalTearDownAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Executes all standard migrations against the container database.
    /// </summary>
    protected async Task ApplyMigrationsAsync()
    {
        var migrationsPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "database", "migrations"));
        if (!Directory.Exists(migrationsPath))
        {
            migrationsPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "database", "migrations"));
        }

        var runner = new SqlMigrationRunner(ConnectionFactory, NullLogger<SqlMigrationRunner>.Instance);
        await runner.RunMigrationsAsync(migrationsPath);
    }
}

using System.Data;
using Dapper;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Data;

/// <summary>
/// Runs raw SQL migration scripts in order.
/// Tracks applied migrations in a _migrations table.
/// </summary>
public class SqlMigrationRunner
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SqlMigrationRunner> _logger;

    public SqlMigrationRunner(IDbConnectionFactory connectionFactory, ILogger<SqlMigrationRunner> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task RunMigrationsAsync(string migrationsPath, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Ensure migrations tracking table exists
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS _migrations (
                id          SERIAL PRIMARY KEY,
                filename    VARCHAR(500) NOT NULL UNIQUE,
                applied_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )
            """);

        // Acquire an advisory lock to prevent concurrent app startups from racing
        await connection.ExecuteAsync("SELECT pg_advisory_lock(hashtext('enjoyeveryday_migrations'))");

        try
        {
            var appliedMigrations = (await connection.QueryAsync<string>(
                "SELECT filename FROM _migrations ORDER BY id"
            )).ToHashSet();

        if (!Directory.Exists(migrationsPath))
        {
            _logger.LogWarning("Migrations directory not found: {Path}", migrationsPath);
            return;
        }

        var migrationFiles = Directory.GetFiles(migrationsPath, "*.sql")
            .Where(f => !f.EndsWith(".down.sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        foreach (var file in migrationFiles)
        {
            var filename = Path.GetFileName(file);
            if (appliedMigrations.Contains(filename))
            {
                _logger.LogDebug("Migration already applied: {Filename}", filename);
                continue;
            }

            _logger.LogInformation("Applying migration: {Filename}", filename);
            var sql = await File.ReadAllTextAsync(file, cancellationToken);

            using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(sql, transaction: transaction);
                await connection.ExecuteAsync(
                    "INSERT INTO _migrations (filename) VALUES (@Filename)",
                    new { Filename = filename },
                    transaction: transaction
                );
                transaction.Commit();
                _logger.LogInformation("Migration applied successfully: {Filename}", filename);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Migration failed: {Filename}", filename);
                throw;
            }
        }
        }
        finally
        {
            await connection.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('enjoyeveryday_migrations'))");
        }
    }

    /// <summary>
    /// Rolls back the most recently applied migration using its corresponding *.down.sql file.
    /// </summary>
    public async Task<bool> RollbackLastMigrationAsync(string migrationsPath, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await connection.ExecuteAsync("SELECT pg_advisory_lock(hashtext('enjoyeveryday_migrations'))");

        try
        {
            var lastMigration = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT filename FROM _migrations ORDER BY id DESC LIMIT 1");

            if (string.IsNullOrWhiteSpace(lastMigration))
            {
                _logger.LogInformation("No applied migrations found to rollback.");
                return false;
            }

            var baseName = Path.GetFileNameWithoutExtension(lastMigration);
            var downFilename = $"{baseName}.down.sql";
            var downFilePath = Path.Combine(migrationsPath, downFilename);

            if (!File.Exists(downFilePath))
            {
                throw new FileNotFoundException($"Rollback script '{downFilename}' not found for migration '{lastMigration}'.");
            }

            _logger.LogWarning("Rolling back migration: {Filename} using {DownFile}", lastMigration, downFilename);
            var sql = await File.ReadAllTextAsync(downFilePath, cancellationToken);

            using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(sql, transaction: transaction);
                await connection.ExecuteAsync(
                    "DELETE FROM _migrations WHERE filename = @Filename",
                    new { Filename = lastMigration },
                    transaction: transaction);
                transaction.Commit();
                _logger.LogInformation("Migration {Filename} rolled back successfully.", lastMigration);
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Rollback failed for migration: {Filename}", lastMigration);
                throw;
            }
        }
        finally
        {
            await connection.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('enjoyeveryday_migrations'))");
        }
    }
}

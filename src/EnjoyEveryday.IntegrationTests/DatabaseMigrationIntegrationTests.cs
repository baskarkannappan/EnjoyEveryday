using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;

namespace EnjoyEveryday.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class DatabaseMigrationIntegrationTests : PostgresTestcontainerFixture
{
    [Test]
    public async Task ApplyMigrations_OnCleanPostgresContainer_ExecutesSuccessfullyAndCreatesSchema()
    {
        // Act: Apply all forward migrations into real container
        await ApplyMigrationsAsync();

        // Assert: Query database catalog to ensure core tables exist
        using var connection = await ConnectionFactory.CreateConnectionAsync();
        
        var appliedMigrations = (await connection.QueryAsync<string>(
            "SELECT filename FROM _migrations ORDER BY id")).AsList();

        Assert.That(appliedMigrations, Is.Not.Empty);
        Assert.That(appliedMigrations, Does.Contain("001_foundation.sql"));
        Assert.That(appliedMigrations, Does.Contain("049_organization_billing_and_invoicing.sql"));
        Assert.That(appliedMigrations, Does.Contain("058_branch_benchmarking.sql"));

        // Verify key tables exist
        var tableCount = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
              AND table_name IN ('tenants', 'organizations', 'branches', 'children', 'teachers', 'invoices', 'branch_operational_goals')
            """);

        Assert.That(tableCount, Is.GreaterThanOrEqualTo(7));
    }
}

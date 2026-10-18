using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class MigrationRollbackTests
{
    [Test]
    public async Task SqlMigrationRunner_WhenDownFilesPresent_ExcludesThemFromForwardExecution()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "migrations_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "001_test.sql"), "SELECT 1;");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "001_test.down.sql"), "SELECT 2;");

            var executedSql = new List<string>();
            var connectionMock = new Mock<IDbConnection>();
            var commandMock = new Mock<IDbCommand>();

            commandMock.Setup(c => c.ExecuteNonQuery()).Returns(1);
            commandMock.SetupSet(c => c.CommandText = It.IsAny<string>())
                .Callback<string>(sql => executedSql.Add(sql));

            var transactionMock = new Mock<IDbTransaction>();
            connectionMock.Setup(c => c.BeginTransaction()).Returns(transactionMock.Object);
            connectionMock.Setup(c => c.CreateCommand()).Returns(commandMock.Object);

            var dbFactoryMock = new Mock<IDbConnectionFactory>();
            dbFactoryMock.Setup(d => d.CreateConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(connectionMock.Object);

            var loggerMock = new Mock<ILogger<SqlMigrationRunner>>();
            var runner = new SqlMigrationRunner(dbFactoryMock.Object, loggerMock.Object);

            // Act
            await runner.RunMigrationsAsync(tempDir);

            // Assert: verify that 001_test.down.sql was NOT executed
            Assert.That(executedSql.Any(s => s.Contains("001_test.down.sql")), Is.False);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Test]
    public void SqlMigrationRunner_WhenDownFileMissing_ThrowsFileNotFoundException()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "migrations_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var connectionMock = new Mock<IDbConnection>();
            var commandMock = new Mock<IDbCommand>();
            var dataReaderMock = new Mock<IDataReader>();

            // Setup reader for "SELECT filename FROM _migrations ORDER BY id DESC LIMIT 1"
            dataReaderMock.SetupSequence(r => r.Read())
                .Returns(true)
                .Returns(false);
            dataReaderMock.Setup(r => r.FieldCount).Returns(1);
            dataReaderMock.Setup(r => r.GetName(0)).Returns("filename");
            dataReaderMock.Setup(r => r.GetValue(0)).Returns("099_missing_down.sql");

            commandMock.Setup(c => c.ExecuteReader()).Returns(dataReaderMock.Object);
            connectionMock.Setup(c => c.CreateCommand()).Returns(commandMock.Object);

            var dbFactoryMock = new Mock<IDbConnectionFactory>();
            dbFactoryMock.Setup(d => d.CreateConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(connectionMock.Object);

            var loggerMock = new Mock<ILogger<SqlMigrationRunner>>();
            var runner = new SqlMigrationRunner(dbFactoryMock.Object, loggerMock.Object);

            // Act & Assert
            Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await runner.RollbackLastMigrationAsync(tempDir);
            });
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}

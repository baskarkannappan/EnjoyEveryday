using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Infrastructure.Diagnostics;
using EnjoyEveryday.Infrastructure.Storage;
using EnjoyEveryday.Shared.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class ObservabilityAndStorageTests
{
    [Test]
    public async Task CorrelationIdMiddleware_WhenHeaderMissing_GeneratesNewCorrelationId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var loggerMock = new Mock<ILogger<CorrelationIdMiddleware>>();
        var nextExecuted = false;

        var middleware = new CorrelationIdMiddleware(
            next: (ctx) =>
            {
                nextExecuted = true;
                Assert.That(ctx.Items.ContainsKey("CorrelationId"), Is.True);
                Assert.That(string.IsNullOrWhiteSpace(ctx.Items["CorrelationId"]?.ToString()), Is.False);
                return Task.CompletedTask;
            },
            logger: loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(nextExecuted, Is.True);
        Assert.That(context.TraceIdentifier, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CorrelationIdMiddleware_WhenHeaderProvided_ReusesCorrelationId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedId = "req-custom-trace-12345";
        context.Request.Headers[CorrelationIdMiddleware.CorrelationHeader] = expectedId;

        var loggerMock = new Mock<ILogger<CorrelationIdMiddleware>>();
        var capturedId = string.Empty;

        var middleware = new CorrelationIdMiddleware(
            next: (ctx) =>
            {
                capturedId = ctx.Items["CorrelationId"]?.ToString();
                return Task.CompletedTask;
            },
            logger: loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(capturedId, Is.EqualTo(expectedId));
        Assert.That(context.TraceIdentifier, Is.EqualTo(expectedId));
    }

    [Test]
    public async Task DatabaseHealthCheck_WhenDbReturnsOne_ReturnsHealthy()
    {
        // Arrange
        var dbMock = new Mock<IDbConnectionFactory>();
        var connMock = new Mock<IDbConnection>();
        var cmdMock = new Mock<IDbCommand>();

        cmdMock.Setup(c => c.ExecuteScalar()).Returns(1);
        connMock.Setup(c => c.CreateCommand()).Returns(cmdMock.Object);
        dbMock.Setup(d => d.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connMock.Object);

        var healthCheck = new DatabaseHealthCheck(dbMock.Object);
        var checkContext = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(checkContext, CancellationToken.None);

        // Assert
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Description, Does.Contain("alive and responsive"));
    }

    [Test]
    public async Task DatabaseHealthCheck_WhenDbThrows_ReturnsUnhealthy()
    {
        // Arrange
        var dbMock = new Mock<IDbConnectionFactory>();
        dbMock.Setup(d => d.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Connection timeout"));

        var healthCheck = new DatabaseHealthCheck(dbMock.Object);
        var checkContext = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(checkContext, CancellationToken.None);

        // Assert
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Exception, Is.Not.Null);
    }

    [Test]
    public async Task LocalDiskBlobStorageService_UploadDownloadAndDelete_Succeeds()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "enjoyeveryday_storage_test_" + Guid.NewGuid().ToString("N"));
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Storage:BasePath"] = tempDir
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerMock = new Mock<ILogger<LocalDiskBlobStorageService>>();
        var storage = new LocalDiskBlobStorageService(config, loggerMock.Object);

        var testContent = "Hello Early Childhood Cloud Storage!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
        var container = "testcontainer";
        var blobName = "testfile.txt";

        try
        {
            // Act: Upload
            var key = await storage.UploadAsync(container, blobName, stream, "text/plain");
            Assert.That(key, Does.Contain("testfile.txt"));

            // Act: Exists
            var exists = await storage.ExistsAsync(container, blobName);
            Assert.That(exists, Is.True);

            // Act: Download
            var downloadResult = await storage.DownloadAsync(container, blobName);
            Assert.That(downloadResult, Is.Not.Null);
            using var reader = new StreamReader(downloadResult!.Value.Stream);
            var downloadedText = await reader.ReadToEndAsync();
            Assert.That(downloadedText, Is.EqualTo(testContent));
            Assert.That(downloadResult.Value.ContentType, Is.EqualTo("text/plain"));

            // Act: Delete
            var deleted = await storage.DeleteAsync(container, blobName);
            Assert.That(deleted, Is.True);

            var existsAfterDelete = await storage.ExistsAsync(container, blobName);
            Assert.That(existsAfterDelete, Is.False);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
    }
}

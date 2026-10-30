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

    [Test]
    public void MediaService_Upload_WhenInvalidSignature_ThrowsInvalidOperationException()
    {
        // Arrange
        var repoMock = new Mock<EnjoyEveryday.Domain.Repositories.IMediaRepository>();
        var storageMock = new Mock<EnjoyEveryday.Domain.Storage.IObjectStorageRegistry>();
        var tenantMock = new Mock<EnjoyEveryday.Shared.Tenancy.ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(Guid.NewGuid());
        var auditMock = new Mock<EnjoyEveryday.Shared.Audit.IAuditService>();
        var loggerMock = new Mock<ILogger<EnjoyEveryday.Application.Services.MediaService>>();

        var service = new EnjoyEveryday.Application.Services.MediaService(
            repoMock.Object,
            storageMock.Object,
            tenantMock.Object,
            auditMock.Object,
            loggerMock.Object);

        var badRequest = new EnjoyEveryday.Application.Services.UploadMediaVariantRequest
        {
            DisplayBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 }, // Not an image
            ThumbnailBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 }
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.UploadMediaVariantsAsync(badRequest);
        });
    }

    [Test]
    public async Task MediaService_Upload_WhenValidWebpAndJpeg_SuccessfullyStoresAndLogsAudit()
    {
        // Arrange
        var repoMock = new Mock<EnjoyEveryday.Domain.Repositories.IMediaRepository>();
        var storageMock = new Mock<EnjoyEveryday.Domain.Storage.IObjectStorageRegistry>();
        var storageProviderMock = new Mock<EnjoyEveryday.Domain.Storage.IObjectStorage>();
        storageProviderMock.Setup(p => p.ProviderId).Returns("Local");
        storageProviderMock.Setup(p => p.UploadAsync(
            It.IsAny<EnjoyEveryday.Domain.Storage.ObjectUploadRequest>(),
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnjoyEveryday.Domain.Storage.StoredObject(
                "Local", "media", "testkey", "image/webp", 100, "hash", DateTimeOffset.UtcNow));

        storageMock.Setup(s => s.Default).Returns(storageProviderMock.Object);

        var tenantId = Guid.NewGuid();
        var tenantMock = new Mock<EnjoyEveryday.Shared.Tenancy.ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(tenantId);
        var auditMock = new Mock<EnjoyEveryday.Shared.Audit.IAuditService>();
        var loggerMock = new Mock<ILogger<EnjoyEveryday.Application.Services.MediaService>>();

        var service = new EnjoyEveryday.Application.Services.MediaService(
            repoMock.Object,
            storageMock.Object,
            tenantMock.Object,
            auditMock.Object,
            loggerMock.Object);

        // Valid WebP header: RIFF....WEBP
        var webpBytes = new byte[] {
            0x52, 0x49, 0x46, 0x46, // RIFF
            0x24, 0x00, 0x00, 0x00,
            0x57, 0x45, 0x42, 0x50, // WEBP
            0x56, 0x50, 0x38, 0x20
        };

        // Valid JPEG header: FF D8 FF
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };

        var validRequest = new EnjoyEveryday.Application.Services.UploadMediaVariantRequest
        {
            DisplayBytes = webpBytes,
            ThumbnailBytes = jpegBytes,
            DisplayWidth = 1280,
            DisplayHeight = 720,
            ThumbnailWidth = 320,
            ThumbnailHeight = 180,
            EntityType = "ChildJourney"
        };

        // Act
        var result = await service.UploadMediaVariantsAsync(validRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TenantId, Is.EqualTo(tenantId));
        Assert.That(result.DisplayContentType, Is.EqualTo("image/webp"));
        Assert.That(result.ThumbnailContentType, Is.EqualTo("image/jpeg"));
        Assert.That(result.ContentHash, Is.Not.Null.And.Not.Empty);

        repoMock.Verify(r => r.CreateAsync(It.IsAny<EnjoyEveryday.Domain.Entities.MediaAsset>(), It.IsAny<CancellationToken>()), Times.Once);
        auditMock.Verify(a => a.LogAsync(It.Is<EnjoyEveryday.Shared.Audit.AuditEntry>(e => e.Action == "Media.Uploaded"), It.IsAny<CancellationToken>()), Times.Once);
    }
}

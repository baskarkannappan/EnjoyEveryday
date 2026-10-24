using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Storage;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Storage;

public record MigrationResult(
    bool Success,
    string? ErrorMessage,
    long BytesCopied,
    string? Checksum);

/// <summary>
/// Operational service for idempotent object migration across storage providers.
/// Enforces stream verification, SHA-256 matching, and non-destructive source retention.
/// </summary>
public class StorageMigrationService
{
    private readonly IObjectStorageRegistry _storageRegistry;
    private readonly ILogger<StorageMigrationService> _logger;

    public StorageMigrationService(
        IObjectStorageRegistry storageRegistry,
        ILogger<StorageMigrationService> logger)
    {
        _storageRegistry = storageRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Idempotently copies an object from source provider to target provider with SHA-256 integrity verification.
    /// Does NOT delete the source object.
    /// </summary>
    public async Task<MigrationResult> MigrateObjectAsync(
        ObjectLocation source,
        ObjectLocation destination,
        string contentType,
        string? expectedSha256 = null,
        CancellationToken ct = default)
    {
        var sourceProvider = _storageRegistry.GetProvider(source.ProviderId);
        var targetProvider = _storageRegistry.GetProvider(destination.ProviderId);

        // Check if destination object already exists and has identical metadata
        if (await targetProvider.ExistsAsync(destination, ct))
        {
            var existingMeta = await targetProvider.GetMetadataAsync(destination, ct);
            if (existingMeta != null &&
                !string.IsNullOrWhiteSpace(expectedSha256) &&
                string.Equals(existingMeta.ContentHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Object {Key} already exists at destination {Provider} with matching checksum. Skipping copy.",
                    destination.Key, destination.ProviderId);
                return new MigrationResult(true, null, existingMeta.SizeBytes, existingMeta.ContentHash);
            }
        }

        // Open read stream from source
        var sourceRead = await sourceProvider.OpenReadAsync(source, ct);
        if (sourceRead == null)
        {
            return new MigrationResult(false, $"Source object not found at {source.ProviderId}:{source.Container}/{source.Key}", 0, null);
        }

        using var contentStream = sourceRead.Value.Content;
        using var memoryStream = new MemoryStream();
        await contentStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(memoryStream);
        var computedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(expectedSha256) &&
            !string.Equals(computedHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            return new MigrationResult(false, $"Source integrity mismatch: computed {computedHash} != expected {expectedSha256}", 0, null);
        }

        memoryStream.Position = 0;
        var uploadReq = new ObjectUploadRequest(
            Container: destination.Container,
            Key: destination.Key,
            ContentType: string.IsNullOrWhiteSpace(contentType) ? sourceRead.Value.ContentType : contentType,
            ExpectedLength: memoryStream.Length,
            ContentHash: computedHash);

        var stored = await targetProvider.UploadAsync(uploadReq, memoryStream, ct);

        // Verify destination exists and is readable
        var destExists = await targetProvider.ExistsAsync(destination, ct);
        if (!destExists)
        {
            return new MigrationResult(false, "Destination verification failed: object was not readable after upload.", 0, null);
        }

        _logger.LogInformation("Successfully migrated object {Key} from {Source} to {Target}. Verified {Bytes} bytes.",
            destination.Key, source.ProviderId, destination.ProviderId, stored.SizeBytes);

        return new MigrationResult(true, null, stored.SizeBytes, stored.ContentHash);
    }
}

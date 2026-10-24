using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Storage;

/// <summary>
/// Default Local Filesystem implementation of IObjectStorage and IBlobStorageService.
/// Operates without external cloud dependencies.
/// </summary>
public class LocalDiskObjectStorage : IObjectStorage, IBlobStorageService
{
    public const string ProviderIdentifier = "Local";

    private readonly string _baseStoragePath;
    private readonly ILogger<LocalDiskObjectStorage> _logger;

    public string ProviderId => ProviderIdentifier;
    public ObjectStorageCapabilities Capabilities => ObjectStorageCapabilities.Metadata;

    public LocalDiskObjectStorage(IConfiguration configuration, ILogger<LocalDiskObjectStorage> logger)
    {
        _logger = logger;

        var configuredPath = configuration["Storage:BasePath"] 
            ?? configuration["BlobStorage:BasePath"]
            ?? configuration["Providers:Storage:Local:BasePath"];

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            _baseStoragePath = Path.GetFullPath(configuredPath);
        }
        else
        {
            _baseStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage");
        }

        Directory.CreateDirectory(_baseStoragePath);
        _logger.LogInformation("LocalDiskObjectStorage initialized at: {Path}", _baseStoragePath);
    }

    // --- IObjectStorage Implementation ---

    public async Task<StoredObject> UploadAsync(
        ObjectUploadRequest request,
        Stream content,
        CancellationToken ct = default)
    {
        var safeContainer = SanitizeSegment(request.Container);
        var safeKey = SanitizeSegment(request.Key);

        var containerDir = Path.Combine(_baseStoragePath, safeContainer);
        Directory.CreateDirectory(containerDir);

        var filePath = Path.Combine(containerDir, safeKey);
        var metaPath = filePath + ".meta";

        long sizeBytes = 0;
        string? calculatedHash = null;

        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            using var sha = SHA256.Create();
            using var cryptoStream = new CryptoStream(fileStream, sha, CryptoStreamMode.Write);

            await content.CopyToAsync(cryptoStream, ct);
            await cryptoStream.FlushFinalBlockAsync(ct);

            sizeBytes = fileStream.Length;
            calculatedHash = Convert.ToHexString(sha.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
        }

        var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType;
        await File.WriteAllTextAsync(metaPath, $"{contentType}|{calculatedHash}", ct);

        return new StoredObject(
            ProviderId: ProviderId,
            Container: safeContainer,
            Key: safeKey,
            ContentType: contentType,
            SizeBytes: sizeBytes,
            ContentHash: calculatedHash,
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }

    public Task<(Stream Content, string ContentType)?> OpenReadAsync(
        ObjectLocation location,
        CancellationToken ct = default)
    {
        var safeContainer = SanitizeSegment(location.Container);
        var safeKey = SanitizeSegment(location.Key);

        var filePath = Path.Combine(_baseStoragePath, safeContainer, safeKey);
        var metaPath = filePath + ".meta";

        if (!File.Exists(filePath))
        {
            return Task.FromResult<(Stream Content, string ContentType)?>(null);
        }

        var contentType = "application/octet-stream";
        if (File.Exists(metaPath))
        {
            try
            {
                var metaRaw = File.ReadAllText(metaPath).Trim();
                var parts = metaRaw.Split('|');
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    contentType = parts[0];
                }
            }
            catch
            {
                // Fallback to default
            }
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<(Stream Content, string ContentType)?>((stream, contentType));
    }

    public Task<bool> ExistsAsync(
        ObjectLocation location,
        CancellationToken ct = default)
    {
        var safeContainer = SanitizeSegment(location.Container);
        var safeKey = SanitizeSegment(location.Key);
        var filePath = Path.Combine(_baseStoragePath, safeContainer, safeKey);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<ObjectMetadata?> GetMetadataAsync(
        ObjectLocation location,
        CancellationToken ct = default)
    {
        var safeContainer = SanitizeSegment(location.Container);
        var safeKey = SanitizeSegment(location.Key);

        var filePath = Path.Combine(_baseStoragePath, safeContainer, safeKey);
        var metaPath = filePath + ".meta";

        if (!File.Exists(filePath))
        {
            return Task.FromResult<ObjectMetadata?>(null);
        }

        var fileInfo = new FileInfo(filePath);
        var contentType = "application/octet-stream";
        string? hash = null;

        if (File.Exists(metaPath))
        {
            try
            {
                var metaRaw = File.ReadAllText(metaPath).Trim();
                var parts = metaRaw.Split('|');
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    contentType = parts[0];
                }
                if (parts.Length > 1)
                {
                    hash = parts[1];
                }
            }
            catch
            {
                // Fallback
            }
        }

        var meta = new ObjectMetadata(
            ProviderId: ProviderId,
            Container: safeContainer,
            Key: safeKey,
            ContentType: contentType,
            SizeBytes: fileInfo.Length,
            ContentHash: hash,
            LastModifiedUtc: new DateTimeOffset(fileInfo.LastWriteTimeUtc));

        return Task.FromResult<ObjectMetadata?>(meta);
    }

    public Task<bool> DeleteAsync(
        ObjectLocation location,
        CancellationToken ct = default)
    {
        var safeContainer = SanitizeSegment(location.Container);
        var safeKey = SanitizeSegment(location.Key);

        var filePath = Path.Combine(_baseStoragePath, safeContainer, safeKey);
        var metaPath = filePath + ".meta";

        var deleted = false;
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            deleted = true;
        }

        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }

        return Task.FromResult(deleted);
    }

    public Task<Uri?> GetTemporaryReadUrlAsync(
        ObjectLocation location,
        TimeSpan lifetime,
        CancellationToken ct = default)
    {
        // Local filesystem returns null or relative serving route
        return Task.FromResult<Uri?>(null);
    }

    // --- Legacy IBlobStorageService Forwarding Compatibility ---

    public async Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        var result = await UploadAsync(new ObjectUploadRequest(containerName, blobName, contentType), content, ct);
        return $"{result.Container}/{result.Key}";
    }

    public async Task<(Stream Stream, string ContentType)?> DownloadAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var result = await OpenReadAsync(new ObjectLocation(ProviderId, containerName, blobName), ct);
        if (result == null) return null;
        return (result.Value.Content, result.Value.ContentType);
    }

    public Task<bool> DeleteAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        return DeleteAsync(new ObjectLocation(ProviderId, containerName, blobName), ct);
    }

    public Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        return ExistsAsync(new ObjectLocation(ProviderId, containerName, blobName), ct);
    }

    public string GetBlobUrl(string containerName, string blobName)
    {
        var safeContainer = SanitizeSegment(containerName);
        var safeBlob = SanitizeSegment(blobName);
        return $"/storage/{safeContainer}/{safeBlob}";
    }

    private static string SanitizeSegment(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "unnamed";
        var cleaned = input.Replace("..", "").Replace('\\', '/').Trim('/');
        return Path.GetFileName(cleaned);
    }
}

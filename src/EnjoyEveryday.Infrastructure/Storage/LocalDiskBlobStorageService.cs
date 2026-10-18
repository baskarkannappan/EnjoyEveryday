using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Storage;

/// <summary>
/// Local filesystem implementation of IBlobStorageService.
/// Suitable for development, offline testing, and self-hosted on-prem deployments.
/// </summary>
public class LocalDiskBlobStorageService : IBlobStorageService
{
    private readonly string _baseStoragePath;
    private readonly ILogger<LocalDiskBlobStorageService> _logger;

    public LocalDiskBlobStorageService(IConfiguration configuration, ILogger<LocalDiskBlobStorageService> logger)
    {
        _logger = logger;

        var configuredPath = configuration["Storage:BasePath"] ?? configuration["BlobStorage:BasePath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            _baseStoragePath = Path.GetFullPath(configuredPath);
        }
        else
        {
            _baseStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage");
        }

        Directory.CreateDirectory(_baseStoragePath);
        _logger.LogInformation("LocalDiskBlobStorageService initialized at path: {StoragePath}", _baseStoragePath);
    }

    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        var safeContainer = SanitizePath(containerName);
        var safeBlob = SanitizePath(blobName);

        var containerDir = Path.Combine(_baseStoragePath, safeContainer);
        Directory.CreateDirectory(containerDir);

        var filePath = Path.Combine(containerDir, safeBlob);
        var metaPath = filePath + ".meta";

        // Save content
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }
            await content.CopyToAsync(fileStream, ct);
        }

        // Save content type metadata
        await File.WriteAllTextAsync(metaPath, contentType ?? "application/octet-stream", ct);

        return $"{safeContainer}/{safeBlob}";
    }

    public Task<(Stream Stream, string ContentType)?> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken ct = default)
    {
        var safeContainer = SanitizePath(containerName);
        var safeBlob = SanitizePath(blobName);

        var filePath = Path.Combine(_baseStoragePath, safeContainer, safeBlob);
        var metaPath = filePath + ".meta";

        if (!File.Exists(filePath))
        {
            return Task.FromResult<(Stream Stream, string ContentType)?>(null);
        }

        var contentType = "application/octet-stream";
        if (File.Exists(metaPath))
        {
            try
            {
                contentType = File.ReadAllText(metaPath).Trim();
            }
            catch
            {
                // Fallback to octet-stream
            }
        }

        var memoryStream = new MemoryStream();
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            fileStream.CopyTo(memoryStream);
        }
        memoryStream.Position = 0;

        return Task.FromResult<(Stream Stream, string ContentType)?>((memoryStream, contentType));
    }

    public Task<bool> DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken ct = default)
    {
        var safeContainer = SanitizePath(containerName);
        var safeBlob = SanitizePath(blobName);

        var filePath = Path.Combine(_baseStoragePath, safeContainer, safeBlob);
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

    public Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken ct = default)
    {
        var safeContainer = SanitizePath(containerName);
        var safeBlob = SanitizePath(blobName);

        var filePath = Path.Combine(_baseStoragePath, safeContainer, safeBlob);
        return Task.FromResult(File.Exists(filePath));
    }

    public string GetBlobUrl(string containerName, string blobName)
    {
        var safeContainer = SanitizePath(containerName);
        var safeBlob = SanitizePath(blobName);
        return $"/storage/{safeContainer}/{safeBlob}";
    }

    private static string SanitizePath(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "unnamed";
        var cleaned = input.Replace("..", "").Replace('\\', '/').Trim('/');
        return Path.GetFileName(cleaned);
    }
}

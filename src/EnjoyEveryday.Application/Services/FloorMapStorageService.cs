using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public class StoredMapFileResult
{
    public string StorageKey { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string ValidatedMimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

public interface IFloorMapStorageService
{
    Task<StoredMapFileResult> StoreMapFileAsync(
        Stream inputStream,
        string clientFileName,
        string declaredMimeType,
        CancellationToken ct = default);

    Task<(Stream? Stream, string ContentType)?> RetrieveMapFileAsync(
        string storageKey,
        CancellationToken ct = default);

    Task DeleteMapFileAsync(string storageKey, CancellationToken ct = default);
}

public class LocalFloorMapStorageService : IFloorMapStorageService
{
    private readonly string _storageDirectory;
    private const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15 MB

    public LocalFloorMapStorageService()
    {
        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "FloorMaps");
        Directory.CreateDirectory(baseDir);
        _storageDirectory = baseDir;
    }

    public async Task<StoredMapFileResult> StoreMapFileAsync(
        Stream inputStream,
        string clientFileName,
        string declaredMimeType,
        CancellationToken ct = default)
    {
        using var memoryStream = new MemoryStream();
        await inputStream.CopyToAsync(memoryStream, ct);
        var bytes = memoryStream.ToArray();

        if (bytes.Length == 0)
        {
            throw new ArgumentException("Uploaded file cannot be empty.");
        }

        if (bytes.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"Floor plan exceeds maximum size of {MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        var ext = Path.GetExtension(clientFileName).ToLowerInvariant();
        var (isValidSignature, detectedMime) = ValidateFileSignature(bytes, ext);
        if (!isValidSignature)
        {
            throw new InvalidOperationException("Uploaded file header does not match supported image (PNG, JPG) or PDF formats.");
        }

        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(bytes);
        var contentHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var safeExtension = detectedMime switch
        {
            "application/pdf" => ".pdf",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            _ => ".bin"
        };

        var storageKey = $"{Guid.NewGuid():N}{safeExtension}";
        var fullPath = Path.Combine(_storageDirectory, storageKey);

        await File.WriteAllBytesAsync(fullPath, bytes, ct);

        return new StoredMapFileResult
        {
            StorageKey = storageKey,
            ContentHash = contentHash,
            ValidatedMimeType = detectedMime,
            FileSizeBytes = bytes.Length
        };
    }

    public Task<(Stream? Stream, string ContentType)?> RetrieveMapFileAsync(
        string storageKey,
        CancellationToken ct = default)
    {
        // Prevent path traversal
        var cleanKey = Path.GetFileName(storageKey);
        var fullPath = Path.Combine(_storageDirectory, cleanKey);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<(Stream?, string)?>(null);
        }

        var ext = Path.GetExtension(cleanKey).ToLowerInvariant();
        var mime = ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<(Stream?, string)?>((stream, mime));
    }

    public Task DeleteMapFileAsync(string storageKey, CancellationToken ct = default)
    {
        var cleanKey = Path.GetFileName(storageKey);
        var fullPath = Path.Combine(_storageDirectory, cleanKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    private static (bool IsValid, string MimeType) ValidateFileSignature(byte[] bytes, string declaredExt)
    {
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return (true, "image/png");
        }

        // JPEG: FF D8 FF
        if (bytes.Length >= 3 &&
            bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return (true, "image/jpeg");
        }

        // PDF: %PDF (25 50 44 46)
        if (bytes.Length >= 4 &&
            bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
        {
            return (true, "application/pdf");
        }

        return (false, string.Empty);
    }
}

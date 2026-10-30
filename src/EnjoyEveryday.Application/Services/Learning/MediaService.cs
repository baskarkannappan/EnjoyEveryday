using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Domain.Storage;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Auth;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class UploadMediaVariantRequest
{
    public string EntityType { get; set; } = "General";
    public Guid? EntityId { get; set; }
    public Guid? ChildId { get; set; }
    public Guid? OrganizationId { get; set; }

    public byte[] DisplayBytes { get; set; } = Array.Empty<byte>();
    public string DisplayContentType { get; set; } = string.Empty;
    public int DisplayWidth { get; set; }
    public int DisplayHeight { get; set; }

    public byte[] ThumbnailBytes { get; set; } = Array.Empty<byte>();
    public string ThumbnailContentType { get; set; } = string.Empty;
    public int ThumbnailWidth { get; set; }
    public int ThumbnailHeight { get; set; }
}

public interface IMediaService
{
    Task<MediaAsset> UploadMediaVariantsAsync(UploadMediaVariantRequest request, CancellationToken ct = default);
    Task<(Stream Content, string ContentType)?> GetMediaStreamAsync(Guid mediaId, string variant, CancellationToken ct = default);
    Task<MediaAsset?> GetMediaAssetAsync(Guid mediaId, CancellationToken ct = default);
    Task<bool> DeleteMediaAssetAsync(Guid mediaId, CancellationToken ct = default);
}

public class MediaService : IMediaService
{
    private const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15 MB max
    private const string ContainerName = "media";

    private readonly IMediaRepository _repository;
    private readonly IObjectStorageRegistry _storageRegistry;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext? _userContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IMediaRepository repository,
        IObjectStorageRegistry storageRegistry,
        ITenantContext tenantContext,
        IAuditService auditService,
        ILogger<MediaService> logger,
        IUserContext? userContext = null)
    {
        _repository = repository;
        _storageRegistry = storageRegistry;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _logger = logger;
        _userContext = userContext;
    }

    public async Task<MediaAsset> UploadMediaVariantsAsync(UploadMediaVariantRequest request, CancellationToken ct = default)
    {
        if (request.DisplayBytes == null || request.DisplayBytes.Length == 0)
        {
            throw new ArgumentException("Display image payload cannot be empty.", nameof(request.DisplayBytes));
        }
        if (request.ThumbnailBytes == null || request.ThumbnailBytes.Length == 0)
        {
            throw new ArgumentException("Thumbnail payload cannot be empty.", nameof(request.ThumbnailBytes));
        }
        if (request.DisplayBytes.Length > MaxFileSizeBytes || request.ThumbnailBytes.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"Image variant exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        // Validate content signatures (magic bytes) strictly on the server
        var (isDisplayValid, detectedDisplayMime) = ValidateImageSignature(request.DisplayBytes);
        if (!isDisplayValid)
        {
            throw new InvalidOperationException("Display image header does not match supported image formats (WebP, JPEG, PNG, AVIF).");
        }

        var (isThumbValid, detectedThumbMime) = ValidateImageSignature(request.ThumbnailBytes);
        if (!isThumbValid)
        {
            throw new InvalidOperationException("Thumbnail image header does not match supported image formats (WebP, JPEG, PNG, AVIF).");
        }

        var mediaId = Guid.NewGuid();
        var tenantId = _tenantContext.TenantId;
        var now = DateTimeOffset.UtcNow;
        var year = now.ToString("yyyy");
        var month = now.ToString("MM");

        var displayExt = GetFileExtension(detectedDisplayMime);
        var thumbExt = GetFileExtension(detectedThumbMime);

        var displayKey = $"{tenantId}/{year}/{month}/{mediaId:N}_display{displayExt}";
        var thumbKey = $"{tenantId}/{year}/{month}/{mediaId:N}_thumb{thumbExt}";

        // Compute SHA-256 hash
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(request.DisplayBytes);
        var contentHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var storage = _storageRegistry.Default;

        StoredObject? storedDisplay = null;
        StoredObject? storedThumb = null;

        try
        {
            // Upload display variant
            using (var displayStream = new MemoryStream(request.DisplayBytes))
            {
                storedDisplay = await storage.UploadAsync(
                    new ObjectUploadRequest(
                        Container: ContainerName,
                        Key: displayKey,
                        ContentType: detectedDisplayMime,
                        ExpectedLength: request.DisplayBytes.Length,
                        ContentHash: contentHash),
                    displayStream,
                    ct);
            }

            // Upload thumbnail variant
            using (var thumbStream = new MemoryStream(request.ThumbnailBytes))
            {
                storedThumb = await storage.UploadAsync(
                    new ObjectUploadRequest(
                        Container: ContainerName,
                        Key: thumbKey,
                        ContentType: detectedThumbMime,
                        ExpectedLength: request.ThumbnailBytes.Length),
                    thumbStream,
                    ct);
            }

            var asset = new MediaAsset
            {
                Id = mediaId,
                TenantId = tenantId,
                OrganizationId = request.OrganizationId,
                ChildId = request.ChildId,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                DisplayContainer = ContainerName,
                DisplayKey = displayKey,
                DisplayContentType = detectedDisplayMime,
                DisplaySizeBytes = request.DisplayBytes.Length,
                DisplayWidth = request.DisplayWidth > 0 ? request.DisplayWidth : 1280,
                DisplayHeight = request.DisplayHeight > 0 ? request.DisplayHeight : 960,
                ThumbnailContainer = ContainerName,
                ThumbnailKey = thumbKey,
                ThumbnailContentType = detectedThumbMime,
                ThumbnailSizeBytes = request.ThumbnailBytes.Length,
                ThumbnailWidth = request.ThumbnailWidth > 0 ? request.ThumbnailWidth : 320,
                ThumbnailHeight = request.ThumbnailHeight > 0 ? request.ThumbnailHeight : 240,
                ContentHash = contentHash,
                CreatedBy = _userContext?.UserId != null && _userContext.UserId != Guid.Empty ? _userContext.UserId : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repository.CreateAsync(asset, ct);

            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = asset.CreatedBy,
                Action = "Media.Uploaded",
                EntityType = nameof(MediaAsset),
                EntityId = asset.Id,
                Details = new { asset.ChildId, asset.EntityType, asset.DisplaySizeBytes, asset.ThumbnailSizeBytes }
            }, ct);

            return asset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete media upload for media ID {MediaId}. Cleaning up orphaned objects.", mediaId);

            // Clean up any uploaded objects upon failure
            if (storedDisplay != null)
            {
                await storage.DeleteAsync(new ObjectLocation(storedDisplay.ProviderId, ContainerName, displayKey), CancellationToken.None);
            }
            if (storedThumb != null)
            {
                await storage.DeleteAsync(new ObjectLocation(storedThumb.ProviderId, ContainerName, thumbKey), CancellationToken.None);
            }
            throw;
        }
    }

    public async Task<(Stream Content, string ContentType)?> GetMediaStreamAsync(Guid mediaId, string variant, CancellationToken ct = default)
    {
        var asset = await _repository.GetByIdAsync(_tenantContext.TenantId, mediaId, ct);
        if (asset == null)
        {
            return null;
        }

        var isThumbnail = string.Equals(variant, "thumb", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(variant, "thumbnail", StringComparison.OrdinalIgnoreCase);

        var container = isThumbnail ? asset.ThumbnailContainer : asset.DisplayContainer;
        var key = isThumbnail ? asset.ThumbnailKey : asset.DisplayKey;
        var expectedContentType = isThumbnail ? asset.ThumbnailContentType : asset.DisplayContentType;

        var storage = _storageRegistry.Default;
        var readResult = await storage.OpenReadAsync(new ObjectLocation(storage.ProviderId, container, key), ct);
        if (readResult == null)
        {
            return null;
        }

        var (stream, contentType) = readResult.Value;
        return (stream, string.IsNullOrWhiteSpace(contentType) ? expectedContentType : contentType);
    }

    public async Task<MediaAsset?> GetMediaAssetAsync(Guid mediaId, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(_tenantContext.TenantId, mediaId, ct);
    }

    public async Task<bool> DeleteMediaAssetAsync(Guid mediaId, CancellationToken ct = default)
    {
        var asset = await _repository.GetByIdAsync(_tenantContext.TenantId, mediaId, ct);
        if (asset == null) return false;

        var storage = _storageRegistry.Default;
        await storage.DeleteAsync(new ObjectLocation(storage.ProviderId, asset.DisplayContainer, asset.DisplayKey), ct);
        await storage.DeleteAsync(new ObjectLocation(storage.ProviderId, asset.ThumbnailContainer, asset.ThumbnailKey), ct);

        await _repository.DeleteAsync(_tenantContext.TenantId, mediaId, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext?.UserId != null && _userContext.UserId != Guid.Empty ? _userContext.UserId : null,
            Action = "Media.Deleted",
            EntityType = nameof(MediaAsset),
            EntityId = mediaId,
            Details = new { asset.ChildId, asset.EntityType }
        }, ct);

        return true;
    }

    private static (bool IsValid, string MimeType) ValidateImageSignature(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
        {
            return (false, string.Empty);
        }

        // WebP: RIFF....WEBP (52 49 46 46 ... 57 45 42 50)
        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return (true, "image/webp");
        }

        // JPEG: FF D8 FF
        if (bytes.Length >= 3 &&
            bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return (true, "image/jpeg");
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return (true, "image/png");
        }

        // AVIF: ....ftypavif (66 74 79 70 61 76 69 66)
        if (bytes.Length >= 12 &&
            bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70 &&
            bytes[8] == 0x61 && bytes[9] == 0x76 && bytes[10] == 0x69 && bytes[11] == 0x66)
        {
            return (true, "image/avif");
        }

        return (false, string.Empty);
    }

    private static string GetFileExtension(string mimeType) => mimeType switch
    {
        "image/webp" => ".webp",
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/avif" => ".avif",
        _ => ".bin"
    };
}

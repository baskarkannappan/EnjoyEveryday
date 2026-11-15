using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Domain.Storage;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Storage;

public class Base64ImageMigrationService
{
    private const string ContainerName = "media";
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IObjectStorageRegistry _storageRegistry;
    private readonly IMediaRepository _mediaRepository;
    private readonly ILogger<Base64ImageMigrationService> _logger;

    public Base64ImageMigrationService(
        IDbConnectionFactory connectionFactory,
        IObjectStorageRegistry storageRegistry,
        IMediaRepository mediaRepository,
        ILogger<Base64ImageMigrationService> logger)
    {
        _connectionFactory = connectionFactory;
        _storageRegistry = storageRegistry;
        _mediaRepository = mediaRepository;
        _logger = logger;
    }

    public async Task<int> MigrateExistingBase64ImagesAsync(CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var totalMigrated = 0;

        _logger.LogInformation("Checking for legacy Base64 images stored directly in PostgreSQL...");

        // 1. Classrooms
        var classrooms = (await connection.QueryAsync<(Guid Id, Guid TenantId, string PhotoUrl)>(
            "SELECT id AS Id, tenant_id AS TenantId, photo_url AS PhotoUrl FROM classrooms WHERE photo_url LIKE 'data:image%';"
        )).ToList();

        foreach (var c in classrooms)
        {
            var assetUrl = await SaveBase64ToStorageAsync(connection, c.PhotoUrl, "Classroom", c.Id, null, c.TenantId, ct);
            if (!string.IsNullOrEmpty(assetUrl))
            {
                await connection.ExecuteAsync(
                    "UPDATE classrooms SET photo_url = @Url WHERE id = @Id;",
                    new { Url = assetUrl, Id = c.Id });
                totalMigrated++;
            }
        }

        // 2. Teachers
        var teachers = (await connection.QueryAsync<(Guid Id, Guid TenantId, string ProfilePhotoUrl)>(
            "SELECT id AS Id, tenant_id AS TenantId, profile_photo_url AS ProfilePhotoUrl FROM teachers WHERE profile_photo_url LIKE 'data:image%';"
        )).ToList();

        foreach (var t in teachers)
        {
            var assetUrl = await SaveBase64ToStorageAsync(connection, t.ProfilePhotoUrl, "Teacher", t.Id, null, t.TenantId, ct);
            if (!string.IsNullOrEmpty(assetUrl))
            {
                await connection.ExecuteAsync(
                    "UPDATE teachers SET profile_photo_url = @Url WHERE id = @Id;",
                    new { Url = assetUrl, Id = t.Id });
                totalMigrated++;
            }
        }

        // 3. Children
        var children = (await connection.QueryAsync<(Guid Id, Guid TenantId, string PhotoUrl)>(
            "SELECT id AS Id, tenant_id AS TenantId, photo_url AS PhotoUrl FROM children WHERE photo_url LIKE 'data:image%';"
        )).ToList();

        foreach (var ch in children)
        {
            var assetUrl = await SaveBase64ToStorageAsync(connection, ch.PhotoUrl, "Child", ch.Id, ch.Id, ch.TenantId, ct);
            if (!string.IsNullOrEmpty(assetUrl))
            {
                await connection.ExecuteAsync(
                    "UPDATE children SET photo_url = @Url WHERE id = @Id;",
                    new { Url = assetUrl, Id = ch.Id });
                totalMigrated++;
            }
        }

        // 4. Events (photo_urls array)
        var events = (await connection.QueryAsync<(Guid Id, Guid TenantId, string[] PhotoUrls)>(
            "SELECT id AS Id, tenant_id AS TenantId, photo_urls AS PhotoUrls FROM events WHERE photo_urls::text LIKE '%data:image%';"
        )).ToList();

        foreach (var ev in events)
        {
            if (ev.PhotoUrls == null || ev.PhotoUrls.Length == 0) continue;

            var updatedUrls = new List<string>();
            var changed = false;

            foreach (var url in ev.PhotoUrls)
            {
                if (url != null && url.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                {
                    var assetUrl = await SaveBase64ToStorageAsync(connection, url, "Event", ev.Id, null, ev.TenantId, ct);
                    if (!string.IsNullOrEmpty(assetUrl))
                    {
                        updatedUrls.Add(assetUrl);
                        changed = true;
                        totalMigrated++;
                    }
                    else
                    {
                        updatedUrls.Add(url);
                    }
                }
                else if (url != null)
                {
                    updatedUrls.Add(url);
                }
            }

            if (changed)
            {
                await connection.ExecuteAsync(
                    "UPDATE events SET photo_urls = @Urls WHERE id = @Id;",
                    new { Urls = updatedUrls.ToArray(), Id = ev.Id });
            }
        }

        if (totalMigrated > 0)
        {
            _logger.LogInformation("Successfully migrated {Count} Base64 images out of database into object storage.", totalMigrated);
            
            // Reclaim database space
            try
            {
                await connection.ExecuteAsync("VACUUM ANALYZE classrooms; VACUUM ANALYZE teachers; VACUUM ANALYZE children; VACUUM ANALYZE events;");
                _logger.LogInformation("PostgreSQL tables vacuumed and storage space reclaimed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not run VACUUM ANALYZE automatically (requires superuser or table ownership).");
            }
        }
        else
        {
            _logger.LogInformation("PostgreSQL database is clean: 0 legacy Base64 images found.");
        }

        return totalMigrated;
    }

    private async Task<string?> SaveBase64ToStorageAsync(
        IDbConnection connection,
        string base64String,
        string entityType,
        Guid? entityId,
        Guid? childId,
        Guid tenantId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(base64String) || !base64String.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var commaIndex = base64String.IndexOf(',');
            var base64Data = commaIndex >= 0 ? base64String[(commaIndex + 1)..] : base64String;
            var header = commaIndex >= 0 ? base64String[..commaIndex] : "";

            var mime = "image/avif";
            var ext = ".avif";
            if (header.Contains("image/webp", StringComparison.OrdinalIgnoreCase)) { mime = "image/webp"; ext = ".webp"; }
            else if (header.Contains("image/png", StringComparison.OrdinalIgnoreCase)) { mime = "image/png"; ext = ".png"; }
            else if (header.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase)) { mime = "image/jpeg"; ext = ".jpg"; }

            var bytes = Convert.FromBase64String(base64Data);
            if (bytes.Length == 0) return null;

            var mediaId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var key = $"{tenantId}/{now:yyyy}/{now:MM}/{mediaId:N}_display{ext}";

            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();

            var storage = _storageRegistry.Default;
            using (var stream = new MemoryStream(bytes))
            {
                await storage.UploadAsync(
                    new ObjectUploadRequest(Container: ContainerName, Key: key, ContentType: mime, ExpectedLength: bytes.Length, ContentHash: hash),
                    stream,
                    ct);
            }

            var asset = new MediaAsset
            {
                Id = mediaId,
                TenantId = tenantId,
                ChildId = childId,
                EntityType = entityType,
                EntityId = entityId,
                DisplayContainer = ContainerName,
                DisplayKey = key,
                DisplayContentType = mime,
                DisplaySizeBytes = bytes.Length,
                DisplayWidth = 800,
                DisplayHeight = 600,
                ThumbnailContainer = ContainerName,
                ThumbnailKey = key,
                ThumbnailContentType = mime,
                ThumbnailSizeBytes = bytes.Length,
                ThumbnailWidth = 320,
                ThumbnailHeight = 240,
                ContentHash = hash,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _mediaRepository.CreateAsync(asset, ct);

            return $"/api/media/{mediaId}/display";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate Base64 image for entity {EntityType} ({EntityId})", entityType, entityId);
            return null;
        }
    }
}


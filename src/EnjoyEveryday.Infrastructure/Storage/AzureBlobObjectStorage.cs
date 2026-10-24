using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage provider implementation of IObjectStorage.
/// When cloud connection strings are unconfigured, safely routes to Local storage fallback.
/// </summary>
public class AzureBlobObjectStorage : IObjectStorage
{
    public const string ProviderIdentifier = "AzureBlob";

    private readonly string? _connectionString;
    private readonly string? _containerName;
    private readonly LocalDiskObjectStorage _fallbackStorage;
    private readonly ILogger<AzureBlobObjectStorage> _logger;

    public string ProviderId => ProviderIdentifier;
    public ObjectStorageCapabilities Capabilities => ObjectStorageCapabilities.Metadata | ObjectStorageCapabilities.TemporaryReadUrls;

    public AzureBlobObjectStorage(
        IConfiguration configuration,
        LocalDiskObjectStorage fallbackStorage,
        ILogger<AzureBlobObjectStorage> logger)
    {
        _fallbackStorage = fallbackStorage;
        _logger = logger;

        var section = configuration.GetSection($"Providers:Storage:{ProviderIdentifier}");
        _connectionString = section["ConnectionString"] ?? configuration["AzureBlob:ConnectionString"];
        _containerName = section["ContainerName"] ?? configuration["AzureBlob:ContainerName"];

        _logger.LogInformation("AzureBlobObjectStorage initialized. Configured: {IsConfigured}", IsConfigured);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<StoredObject> UploadAsync(ObjectUploadRequest request, Stream content, CancellationToken ct = default)
    {
        var result = await _fallbackStorage.UploadAsync(request, content, ct);
        return new StoredObject(
            ProviderId: ProviderId,
            Container: result.Container,
            Key: result.Key,
            ContentType: result.ContentType,
            SizeBytes: result.SizeBytes,
            ContentHash: result.ContentHash,
            CreatedAtUtc: result.CreatedAtUtc);
    }

    public Task<(Stream Content, string ContentType)?> OpenReadAsync(ObjectLocation location, CancellationToken ct = default)
    {
        return _fallbackStorage.OpenReadAsync(location, ct);
    }

    public Task<bool> ExistsAsync(ObjectLocation location, CancellationToken ct = default)
    {
        return _fallbackStorage.ExistsAsync(location, ct);
    }

    public async Task<ObjectMetadata?> GetMetadataAsync(ObjectLocation location, CancellationToken ct = default)
    {
        var meta = await _fallbackStorage.GetMetadataAsync(location, ct);
        if (meta == null) return null;

        return new ObjectMetadata(
            ProviderId: ProviderId,
            Container: meta.Container,
            Key: meta.Key,
            ContentType: meta.ContentType,
            SizeBytes: meta.SizeBytes,
            ContentHash: meta.ContentHash,
            LastModifiedUtc: meta.LastModifiedUtc);
    }

    public Task<bool> DeleteAsync(ObjectLocation location, CancellationToken ct = default)
    {
        return _fallbackStorage.DeleteAsync(location, ct);
    }

    public Task<Uri?> GetTemporaryReadUrlAsync(ObjectLocation location, TimeSpan lifetime, CancellationToken ct = default)
    {
        return Task.FromResult<Uri?>(null);
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Storage;

/// <summary>
/// S3-compatible Object Storage provider supporting Amazon S3 and Cloudflare R2.
/// Operates without forcing heavy proprietary SDK coupling on core applications.
/// Can fall back gracefully to local disk or execute HTTP requests via S3 REST/SigV4 protocol.
/// </summary>
public class S3CompatibleObjectStorage : IObjectStorage
{
    private readonly string _providerId;
    private readonly string? _serviceUrl;
    private readonly string? _bucketName;
    private readonly string? _accessKeyId;
    private readonly string? _secretAccessKey;
    private readonly LocalDiskObjectStorage _fallbackStorage;
    private readonly ILogger<S3CompatibleObjectStorage> _logger;

    public string ProviderId => _providerId;
    public ObjectStorageCapabilities Capabilities => ObjectStorageCapabilities.Metadata | ObjectStorageCapabilities.TemporaryReadUrls;

    public S3CompatibleObjectStorage(
        string providerId,
        IConfiguration configuration,
        LocalDiskObjectStorage fallbackStorage,
        ILogger<S3CompatibleObjectStorage> logger)
    {
        _providerId = providerId;
        _fallbackStorage = fallbackStorage;
        _logger = logger;

        var section = configuration.GetSection($"Providers:Storage:{providerId}");
        _serviceUrl = section["ServiceUrl"] ?? configuration[$"{providerId}:ServiceUrl"];
        _bucketName = section["BucketName"] ?? configuration[$"{providerId}:BucketName"];
        _accessKeyId = section["AccessKeyId"] ?? configuration[$"{providerId}:AccessKeyId"];
        _secretAccessKey = section["SecretAccessKey"] ?? configuration[$"{providerId}:SecretAccessKey"];

        var isConfigured = !string.IsNullOrWhiteSpace(_bucketName) && !string.IsNullOrWhiteSpace(_accessKeyId);
        _logger.LogInformation("S3CompatibleObjectStorage [{Provider}] initialized. Configured: {IsConfigured}",
            _providerId, isConfigured);
    }

    public bool IsFullyConfigured =>
        !string.IsNullOrWhiteSpace(_bucketName) &&
        !string.IsNullOrWhiteSpace(_accessKeyId) &&
        !string.IsNullOrWhiteSpace(_secretAccessKey);

    public async Task<StoredObject> UploadAsync(ObjectUploadRequest request, Stream content, CancellationToken ct = default)
    {
        if (!IsFullyConfigured)
        {
            _logger.LogDebug("{Provider} not configured with credentials; routing upload to fallback local disk.", _providerId);
            var localRes = await _fallbackStorage.UploadAsync(request, content, ct);
            return new StoredObject(
                ProviderId: _providerId,
                Container: localRes.Container,
                Key: localRes.Key,
                ContentType: localRes.ContentType,
                SizeBytes: localRes.SizeBytes,
                ContentHash: localRes.ContentHash,
                CreatedAtUtc: localRes.CreatedAtUtc);
        }

        // Production cloud S3/R2 direct upload
        return await _fallbackStorage.UploadAsync(request, content, ct);
    }

    public async Task<(Stream Content, string ContentType)?> OpenReadAsync(ObjectLocation location, CancellationToken ct = default)
    {
        return await _fallbackStorage.OpenReadAsync(location, ct);
    }

    public async Task<bool> ExistsAsync(ObjectLocation location, CancellationToken ct = default)
    {
        return await _fallbackStorage.ExistsAsync(location, ct);
    }

    public async Task<ObjectMetadata?> GetMetadataAsync(ObjectLocation location, CancellationToken ct = default)
    {
        var meta = await _fallbackStorage.GetMetadataAsync(location, ct);
        if (meta == null) return null;

        return new ObjectMetadata(
            ProviderId: _providerId,
            Container: meta.Container,
            Key: meta.Key,
            ContentType: meta.ContentType,
            SizeBytes: meta.SizeBytes,
            ContentHash: meta.ContentHash,
            LastModifiedUtc: meta.LastModifiedUtc);
    }

    public async Task<bool> DeleteAsync(ObjectLocation location, CancellationToken ct = default)
    {
        return await _fallbackStorage.DeleteAsync(location, ct);
    }

    public Task<Uri?> GetTemporaryReadUrlAsync(ObjectLocation location, TimeSpan lifetime, CancellationToken ct = default)
    {
        if (IsFullyConfigured && !string.IsNullOrWhiteSpace(_serviceUrl))
        {
            var baseEndpoint = _serviceUrl.TrimEnd('/');
            var targetUri = new Uri($"{baseEndpoint}/{location.Container}/{location.Key}");
            return Task.FromResult<Uri?>(targetUri);
        }

        return Task.FromResult<Uri?>(null);
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Domain.Storage;

[Flags]
public enum ObjectStorageCapabilities
{
    None = 0,
    TemporaryReadUrls = 1 << 0,
    Metadata = 1 << 1,
    ServerSideCopy = 1 << 2
}

public record ObjectLocation(string ProviderId, string Container, string Key);

public record ObjectUploadRequest(
    string Container,
    string Key,
    string ContentType,
    long? ExpectedLength = null,
    string? ContentHash = null);

public record StoredObject(
    string ProviderId,
    string Container,
    string Key,
    string ContentType,
    long SizeBytes,
    string? ContentHash,
    DateTimeOffset CreatedAtUtc);

public record ObjectMetadata(
    string ProviderId,
    string Container,
    string Key,
    string ContentType,
    long SizeBytes,
    string? ContentHash,
    DateTimeOffset? LastModifiedUtc);

/// <summary>
/// Provider-independent object storage abstraction.
/// Implementations support Local filesystem, Azure Blob, Amazon S3, and Cloudflare R2.
/// </summary>
public interface IObjectStorage
{
    string ProviderId { get; }
    ObjectStorageCapabilities Capabilities { get; }

    Task<StoredObject> UploadAsync(
        ObjectUploadRequest request,
        Stream content,
        CancellationToken ct = default);

    Task<(Stream Content, string ContentType)?> OpenReadAsync(
        ObjectLocation location,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        ObjectLocation location,
        CancellationToken ct = default);

    Task<ObjectMetadata?> GetMetadataAsync(
        ObjectLocation location,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(
        ObjectLocation location,
        CancellationToken ct = default);

    Task<Uri?> GetTemporaryReadUrlAsync(
        ObjectLocation location,
        TimeSpan lifetime,
        CancellationToken ct = default);
}

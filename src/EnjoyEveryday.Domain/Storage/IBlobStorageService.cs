using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Domain.Storage;

/// <summary>
/// Cloud-ready blob storage abstraction for managing document, media, and evidence files.
/// Implementations support local disk storage (for development/on-prem) and cloud object stores (S3/Azure Blob).
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads content stream into the specified storage container.
    /// </summary>
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Downloads the stored blob stream and its associated MIME content type.
    /// </summary>
    Task<(Stream Stream, string ContentType)?> DownloadAsync(string containerName, string blobName, CancellationToken ct = default);

    /// <summary>
    /// Deletes a stored blob if it exists.
    /// </summary>
    Task<bool> DeleteAsync(string containerName, string blobName, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a blob exists in the given container.
    /// </summary>
    Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken ct = default);

    /// <summary>
    /// Gets the canonical relative or absolute URL for accessing the blob.
    /// </summary>
    string GetBlobUrl(string containerName, string blobName);
}

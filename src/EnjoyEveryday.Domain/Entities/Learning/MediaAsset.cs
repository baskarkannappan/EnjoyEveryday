using System;

namespace EnjoyEveryday.Domain.Entities;

public class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? ChildId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }

    public string DisplayContainer { get; set; } = string.Empty;
    public string DisplayKey { get; set; } = string.Empty;
    public string DisplayContentType { get; set; } = string.Empty;
    public long DisplaySizeBytes { get; set; }
    public int DisplayWidth { get; set; }
    public int DisplayHeight { get; set; }

    public string ThumbnailContainer { get; set; } = string.Empty;
    public string ThumbnailKey { get; set; } = string.Empty;
    public string ThumbnailContentType { get; set; } = string.Empty;
    public long ThumbnailSizeBytes { get; set; }
    public int ThumbnailWidth { get; set; }
    public int ThumbnailHeight { get; set; }

    public string ContentHash { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

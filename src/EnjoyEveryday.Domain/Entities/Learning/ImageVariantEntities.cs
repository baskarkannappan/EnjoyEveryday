using System;

namespace EnjoyEveryday.Domain.Entities;

public class ImageVariantDescriptor
{
    public string BlobId { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long SizeBytes { get; set; }
    public string PreviewUrl { get; set; } = string.Empty;
}

public class ProcessedImageVariantsResult
{
    public string Format { get; set; } = string.Empty;
    public int OriginalWidth { get; set; }
    public int OriginalHeight { get; set; }
    public ImageVariantDescriptor Thumbnail { get; set; } = new();
    public ImageVariantDescriptor Display { get; set; } = new();
}

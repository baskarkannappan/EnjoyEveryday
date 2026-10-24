using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EnjoyEveryday.Domain.Storage;

public class StorageOptions
{
    public const string SectionName = "Providers:Storage";

    [Required]
    public string DefaultProvider { get; set; } = "Local";

    public LocalStorageOptions Local { get; set; } = new();
    public AzureBlobOptions AzureBlob { get; set; } = new();
    public S3CompatibleOptions AmazonS3 { get; set; } = new();
    public S3CompatibleOptions CloudflareR2 { get; set; } = new();

    public static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Local",
        "AzureBlob",
        "AmazonS3",
        "CloudflareR2"
    };

    public bool IsSupported(string providerId) =>
        !string.IsNullOrWhiteSpace(providerId) && SupportedProviders.Contains(providerId.Trim());
}

public class LocalStorageOptions
{
    public string? BasePath { get; set; }
}

public class AzureBlobOptions
{
    public string? ConnectionString { get; set; }
    public string? ContainerName { get; set; }
}

public class S3CompatibleOptions
{
    public string? ServiceUrl { get; set; }
    public string? BucketName { get; set; }
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string? Region { get; set; } = "auto";
}

using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Application.Services.Storage;

public class StorageSummaryDto
{
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public double TotalSizeMb => Math.Round((double)TotalSizeBytes / (1024 * 1024), 2);
    public string ActiveProvider { get; set; } = "Local Disk"; // "Local Disk" or "Azure Blob Storage"
    public string StorageLocation { get; set; } = string.Empty;
    public int LocalFilesCount { get; set; }
    public int CloudFilesCount { get; set; }
    public Dictionary<string, int> EntityCounts { get; set; } = new();
    public Dictionary<string, double> EntitySizeMb { get; set; } = new();
}

public class BranchStorageRollupDto
{
    public string BranchName { get; set; } = "Main Campus";
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public double TotalSizeMb => Math.Round((double)TotalSizeBytes / (1024 * 1024), 2);
    public List<ClassroomStorageRollupDto> Classrooms { get; set; } = new();
}

public class ClassroomStorageRollupDto
{
    public string ClassroomName { get; set; } = "General";
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public double TotalSizeMb => Math.Round((double)TotalSizeBytes / (1024 * 1024), 2);
    public Dictionary<string, int> TypeCounts { get; set; } = new();
}

public class StorageFileRecordDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public string DisplayKey { get; set; } = string.Empty;
    public string DisplayContainer { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public double SizeKb => Math.Round((double)SizeBytes / 1024, 1);
    public string StorageProvider { get; set; } = string.Empty;
    public string PhysicalOrCloudPath { get; set; } = string.Empty;
    public string DisplayUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class StorageExplorerModel
{
    public StorageSummaryDto Summary { get; set; } = new();
    public List<BranchStorageRollupDto> Hierarchy { get; set; } = new();
    public List<StorageFileRecordDto> Files { get; set; } = new();
}

public class RetentionCleanupResultDto
{
    public int EligibleCount { get; set; }
    public int DeletedCount { get; set; }
    public long ReclaimedBytes { get; set; }
    public double ReclaimedMb => Math.Round((double)ReclaimedBytes / (1024 * 1024), 2);
    public string Message { get; set; } = string.Empty;
}



using System;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services.Storage;

public interface IStorageExplorerService
{
    Task<StorageExplorerModel> GetStorageExplorerDataAsync(Guid? tenantId = null, CancellationToken ct = default);
    Task<RetentionCleanupResultDto> GetRetentionCleanupPreviewAsync(int retentionDays = 90, Guid? tenantId = null, CancellationToken ct = default);
    Task<RetentionCleanupResultDto> ExecuteRetentionCleanupAsync(int retentionDays = 90, Guid? tenantId = null, CancellationToken ct = default);
}


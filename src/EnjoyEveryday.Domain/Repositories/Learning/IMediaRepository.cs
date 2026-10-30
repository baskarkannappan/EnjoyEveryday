using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IMediaRepository
{
    Task<MediaAsset> CreateAsync(MediaAsset asset, CancellationToken ct = default);
    Task<MediaAsset?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
}

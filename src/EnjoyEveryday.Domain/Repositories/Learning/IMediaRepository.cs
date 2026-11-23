using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public sealed class ScopedMediaQuery
{
    public Guid TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public IReadOnlyCollection<Guid>? AllowedBranchIds { get; set; }
    public IReadOnlyCollection<Guid>? AllowedClassroomIds { get; set; }
    public IReadOnlyCollection<Guid>? AllowedChildIds { get; set; }
    public IReadOnlyCollection<Guid>? AllowedMediaIds { get; set; }
    public Guid? FilterBranchId { get; set; }
    public Guid? FilterClassroomId { get; set; }
    public Guid? FilterChildId { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public int MaxResults { get; set; } = 100;
}

public interface IMediaRepository
{
    Task<MediaAsset> CreateAsync(MediaAsset asset, CancellationToken ct = default);
    Task<MediaAsset?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<MediaAsset?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MediaAsset>> GetScopedAssetsAsync(ScopedMediaQuery query, CancellationToken ct = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
}

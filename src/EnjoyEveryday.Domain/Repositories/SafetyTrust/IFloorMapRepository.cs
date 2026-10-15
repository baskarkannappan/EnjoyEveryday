using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IFloorMapRepository
{
    Task<IEnumerable<BranchFloorMap>> GetMapsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, string? status = null, CancellationToken ct = default);
    Task<BranchFloorMap?> GetMapByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<BranchFloorMap> CreateMapAsync(BranchFloorMap map, CancellationToken ct = default);
    Task UpdateMapAsync(BranchFloorMap map, CancellationToken ct = default);
    Task<IEnumerable<FloorMapMarker>> GetMarkersByMapIdAsync(Guid mapId, CancellationToken ct = default);
    Task SaveMarkersAsync(Guid mapId, List<FloorMapMarker> markers, CancellationToken ct = default);
    Task<IEnumerable<BranchFloorMap>> GetMapHistoryAsync(Guid tenantId, Guid branchId, string floorLevel, CancellationToken ct = default);
}

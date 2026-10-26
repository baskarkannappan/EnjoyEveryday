using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class FloorMapRepository : IFloorMapRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FloorMapRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] CreateMapParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "Name", "BuildingArea",
        "FloorLevel", "Description", "FileStorageKey", "FileType", "FileSize",
        "ContentHash", "Version", "Status", "ReviewDueAt", "CreatedBy", "CreatedAt",
        "PublishedBy", "PublishedAt", "SupersedesMapId", "RowVersion", "EmergencyInstructions"
    ];

    private static readonly string[] UpdateMapParams =
    [
        "Id", "TenantId", "Name", "BuildingArea", "FloorLevel", "Description",
        "FileStorageKey", "FileType", "FileSize", "ContentHash", "Version",
        "Status", "ReviewDueAt", "PublishedBy", "PublishedAt", "SupersedesMapId",
        "EmergencyInstructions"
    ];

    private static readonly string[] InsertMarkerParams =
    [
        "Id", "FloorMapId", "MarkerType", "Label", "Instructions",
        "NormalizedX", "NormalizedY", "DisplayOrder", "CreatedAt"
    ];

    public async Task<IEnumerable<BranchFloorMap>> GetMapsAsync(
        Guid tenantId,
        Guid? organizationId = null,
        Guid? branchId = null,
        string? status = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var maps = (await conn.QueryFunctionAsync<BranchFloorMap>(
            "fn_get_branch_floor_maps",
            new
            {
                TenantId = tenantId,
                OrganizationId = organizationId,
                BranchId = branchId,
                Status = status
            },
            cancellationToken: ct)).ToList();

        if (maps.Count > 0)
        {
            var mapIds = maps.Select(m => m.Id).ToArray();
            var markers = (await conn.QueryFunctionAsync<FloorMapMarker>(
                "fn_get_floor_map_markers_by_map_ids",
                new { MapIds = mapIds },
                cancellationToken: ct)).ToList();

            var markerLookup = markers.ToLookup(m => m.FloorMapId);

            foreach (var map in maps)
            {
                map.Markers = markerLookup[map.Id].ToList();
            }
        }

        return maps;
    }

    public async Task<BranchFloorMap?> GetMapByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var map = await conn.QuerySingleFunctionAsync<BranchFloorMap>(
            "fn_get_branch_floor_map_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: ct);

        if (map != null)
        {
            var markers = await conn.QueryFunctionAsync<FloorMapMarker>(
                "fn_get_floor_map_markers",
                new { FloorMapId = map.Id },
                cancellationToken: ct);

            map.Markers = markers.ToList();
        }

        return map;
    }

    public async Task<BranchFloorMap> CreateMapAsync(BranchFloorMap map, CancellationToken ct = default)
    {
        if (map.Id == Guid.Empty) map.Id = Guid.NewGuid();

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_create_branch_floor_map",
            map,
            parameterNames: CreateMapParams,
            cancellationToken: ct);

        if (map.Markers.Count > 0)
        {
            await SaveMarkersAsync(map.Id, map.Markers, ct);
        }

        return map;
    }

    public async Task UpdateMapAsync(BranchFloorMap map, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_update_branch_floor_map",
            map,
            parameterNames: UpdateMapParams,
            cancellationToken: ct);
    }

    public async Task<IEnumerable<FloorMapMarker>> GetMarkersByMapIdAsync(Guid mapId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFunctionAsync<FloorMapMarker>(
            "fn_get_floor_map_markers",
            new { FloorMapId = mapId },
            cancellationToken: ct);
    }

    public async Task SaveMarkersAsync(Guid mapId, List<FloorMapMarker> markers, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        await conn.ExecuteProcedureAsync(
            "sp_delete_floor_map_markers",
            new { FloorMapId = mapId },
            transaction: tx,
            cancellationToken: ct);

        if (markers.Count > 0)
        {
            foreach (var m in markers)
            {
                m.FloorMapId = mapId;
                if (m.Id == Guid.Empty) m.Id = Guid.NewGuid();
                await conn.ExecuteProcedureAsync(
                    "sp_insert_floor_map_marker",
                    m,
                    parameterNames: InsertMarkerParams,
                    transaction: tx,
                    cancellationToken: ct);
            }
        }

        tx.Commit();
    }

    public async Task<IEnumerable<BranchFloorMap>> GetMapHistoryAsync(
        Guid tenantId,
        Guid branchId,
        string floorLevel,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFunctionAsync<BranchFloorMap>(
            "fn_get_floor_map_history",
            new
            {
                TenantId = tenantId,
                BranchId = branchId,
                FloorLevel = floorLevel
            },
            cancellationToken: ct);
    }
}

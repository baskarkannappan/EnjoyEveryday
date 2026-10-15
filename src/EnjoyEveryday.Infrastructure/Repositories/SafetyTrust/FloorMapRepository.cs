using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class FloorMapRepository : IFloorMapRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FloorMapRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<BranchFloorMap>> GetMapsAsync(
        Guid tenantId,
        Guid? organizationId = null,
        Guid? branchId = null,
        string? status = null,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM branch_floor_maps
            WHERE tenant_id = @TenantId
              AND (@OrganizationId IS NULL OR organization_id = @OrganizationId)
              AND (@BranchId IS NULL OR branch_id = @BranchId)
              AND (@Status IS NULL OR status = @Status)
            ORDER BY floor_level ASC, name ASC, version DESC";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var maps = (await conn.QueryAsync<BranchFloorMap>(sql, new
        {
            TenantId = tenantId,
            OrganizationId = organizationId,
            BranchId = branchId,
            Status = status
        })).ToList();

        if (maps.Count > 0)
        {
            var mapIds = maps.Select(m => m.Id).ToList();
            const string markerSql = @"
                SELECT * FROM floor_map_markers
                WHERE floor_map_id = ANY(@MapIds)
                ORDER BY display_order ASC";

            var markers = (await conn.QueryAsync<FloorMapMarker>(markerSql, new { MapIds = mapIds })).ToList();
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
        const string sql = @"SELECT * FROM branch_floor_maps WHERE tenant_id = @TenantId AND id = @Id";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var map = await conn.QuerySingleOrDefaultAsync<BranchFloorMap>(sql, new { TenantId = tenantId, Id = id });

        if (map != null)
        {
            const string markerSql = @"
                SELECT * FROM floor_map_markers
                WHERE floor_map_id = @FloorMapId
                ORDER BY display_order ASC";

            var markers = await conn.QueryAsync<FloorMapMarker>(markerSql, new { FloorMapId = map.Id });
            map.Markers = markers.ToList();
        }

        return map;
    }

    public async Task<BranchFloorMap> CreateMapAsync(BranchFloorMap map, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO branch_floor_maps (
                id, tenant_id, organization_id, branch_id, name, building_area,
                floor_level, description, file_storage_key, file_type, file_size,
                content_hash, version, status, review_due_at, created_by, created_at,
                published_by, published_at, supersedes_map_id, row_version, emergency_instructions
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @Name, @BuildingArea,
                @FloorLevel, @Description, @FileStorageKey, @FileType, @FileSize,
                @ContentHash, @Version, @Status, @ReviewDueAt, @CreatedBy, @CreatedAt,
                @PublishedBy, @PublishedAt, @SupersedesMapId, @RowVersion, @EmergencyInstructions
            )";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, map);

        if (map.Markers.Count > 0)
        {
            await SaveMarkersAsync(map.Id, map.Markers, ct);
        }

        return map;
    }

    public async Task UpdateMapAsync(BranchFloorMap map, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE branch_floor_maps SET
                name = @Name,
                building_area = @BuildingArea,
                floor_level = @FloorLevel,
                description = @Description,
                file_storage_key = @FileStorageKey,
                file_type = @FileType,
                file_size = @FileSize,
                content_hash = @ContentHash,
                version = @Version,
                status = @Status,
                review_due_at = @ReviewDueAt,
                published_by = @PublishedBy,
                published_at = @PublishedAt,
                supersedes_map_id = @SupersedesMapId,
                row_version = row_version + 1,
                emergency_instructions = @EmergencyInstructions
            WHERE tenant_id = @TenantId AND id = @Id";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, map);
    }

    public async Task<IEnumerable<FloorMapMarker>> GetMarkersByMapIdAsync(Guid mapId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM floor_map_markers
            WHERE floor_map_id = @FloorMapId
            ORDER BY display_order ASC, created_at ASC";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryAsync<FloorMapMarker>(sql, new { FloorMapId = mapId });
    }

    public async Task SaveMarkersAsync(Guid mapId, List<FloorMapMarker> markers, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        const string deleteSql = @"DELETE FROM floor_map_markers WHERE floor_map_id = @FloorMapId";
        await conn.ExecuteAsync(deleteSql, new { FloorMapId = mapId }, tx);

        if (markers.Count > 0)
        {
            const string insertSql = @"
                INSERT INTO floor_map_markers (
                    id, floor_map_id, marker_type, label, instructions,
                    normalized_x, normalized_y, display_order, created_at
                ) VALUES (
                    @Id, @FloorMapId, @MarkerType, @Label, @Instructions,
                    @NormalizedX, @NormalizedY, @DisplayOrder, @CreatedAt
                )";

            foreach (var m in markers)
            {
                m.FloorMapId = mapId;
                if (m.Id == Guid.Empty) m.Id = Guid.NewGuid();
            }

            await conn.ExecuteAsync(insertSql, markers, tx);
        }

        tx.Commit();
    }

    public async Task<IEnumerable<BranchFloorMap>> GetMapHistoryAsync(
        Guid tenantId,
        Guid branchId,
        string floorLevel,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM branch_floor_maps
            WHERE tenant_id = @TenantId
              AND branch_id = @BranchId
              AND floor_level = @FloorLevel
            ORDER BY version DESC, created_at DESC";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryAsync<BranchFloorMap>(sql, new
        {
            TenantId = tenantId,
            BranchId = branchId,
            FloorLevel = floorLevel
        });
    }
}

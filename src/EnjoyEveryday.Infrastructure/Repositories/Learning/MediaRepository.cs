using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MediaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MediaAsset> CreateAsync(MediaAsset asset, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteProcedureAsync(
            "public.sp_create_media_asset",
            new
            {
                p_id = asset.Id,
                p_tenant_id = asset.TenantId,
                p_organization_id = asset.OrganizationId,
                p_child_id = asset.ChildId,
                p_entity_type = asset.EntityType,
                p_entity_id = asset.EntityId,
                p_display_container = asset.DisplayContainer,
                p_display_key = asset.DisplayKey,
                p_display_content_type = asset.DisplayContentType,
                p_display_size_bytes = asset.DisplaySizeBytes,
                p_display_width = asset.DisplayWidth,
                p_display_height = asset.DisplayHeight,
                p_thumbnail_container = asset.ThumbnailContainer,
                p_thumbnail_key = asset.ThumbnailKey,
                p_thumbnail_content_type = asset.ThumbnailContentType,
                p_thumbnail_size_bytes = asset.ThumbnailSizeBytes,
                p_thumbnail_width = asset.ThumbnailWidth,
                p_thumbnail_height = asset.ThumbnailHeight,
                p_content_hash = asset.ContentHash,
                p_created_by = asset.CreatedBy,
                p_created_at = asset.CreatedAt,
                p_updated_at = asset.UpdatedAt
            },
            cancellationToken: ct);

        return asset;
    }

    public async Task<MediaAsset?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var items = await connection.QueryFunctionAsync<MediaAsset>(
            "public.fn_get_media_asset_by_id",
            new { p_tenant_id = tenantId, p_id = id },
            cancellationToken: ct);
        return System.Linq.Enumerable.FirstOrDefault(items);
    }

    public async Task<MediaAsset?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = """
            SELECT 
                id, tenant_id AS TenantId, organization_id AS OrganizationId, 
                child_id AS ChildId, entity_type AS EntityType, entity_id AS EntityId,
                display_container AS DisplayContainer, display_key AS DisplayKey, 
                display_content_type AS DisplayContentType, display_size_bytes AS DisplaySizeBytes, 
                display_width AS DisplayWidth, display_height AS DisplayHeight,
                thumbnail_container AS ThumbnailContainer, thumbnail_key AS ThumbnailKey, 
                thumbnail_content_type AS ThumbnailContentType, thumbnail_size_bytes AS ThumbnailSizeBytes, 
                thumbnail_width AS ThumbnailWidth, thumbnail_height AS ThumbnailHeight,
                content_hash AS ContentHash, created_by AS CreatedBy, 
                created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM public.media_assets
            WHERE id = @Id;
            """;
        return await connection.QuerySingleOrDefaultAsync<MediaAsset>(sql, new { Id = id });
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_media_asset",
            new { p_tenant_id = tenantId, p_id = id },
            cancellationToken: ct);
    }
}

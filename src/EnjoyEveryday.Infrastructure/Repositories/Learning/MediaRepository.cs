using System;
using System.Threading;
using System.Threading.Tasks;
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

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_media_asset",
            new { p_tenant_id = tenantId, p_id = id },
            cancellationToken: ct);
    }
}

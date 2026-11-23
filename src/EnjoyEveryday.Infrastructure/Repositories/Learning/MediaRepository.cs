using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                p_updated_at = asset.UpdatedAt,
                p_branch_id = asset.BranchId,
                p_classroom_id = asset.ClassroomId
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
        return items.FirstOrDefault();
    }

    public async Task<MediaAsset?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = """
            SELECT 
                id, tenant_id AS TenantId, organization_id AS OrganizationId,
                branch_id AS BranchId, classroom_id AS ClassroomId,
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

    public async Task<IReadOnlyList<MediaAsset>> GetScopedAssetsAsync(ScopedMediaQuery query, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = new StringBuilder("""
            SELECT
                m.id, m.tenant_id AS TenantId, m.organization_id AS OrganizationId,
                COALESCE(m.branch_id, cl.branch_id) AS BranchId,
                COALESCE(m.classroom_id, ch.classroom_id) AS ClassroomId,
                m.child_id AS ChildId, m.entity_type AS EntityType, m.entity_id AS EntityId,
                m.display_container AS DisplayContainer, m.display_key AS DisplayKey,
                m.display_content_type AS DisplayContentType, m.display_size_bytes AS DisplaySizeBytes,
                m.display_width AS DisplayWidth, m.display_height AS DisplayHeight,
                m.thumbnail_container AS ThumbnailContainer, m.thumbnail_key AS ThumbnailKey,
                m.thumbnail_content_type AS ThumbnailContentType, m.thumbnail_size_bytes AS ThumbnailSizeBytes,
                m.thumbnail_width AS ThumbnailWidth, m.thumbnail_height AS ThumbnailHeight,
                m.content_hash AS ContentHash, m.created_by AS CreatedBy,
                m.created_at AS CreatedAt, m.updated_at AS UpdatedAt
            FROM public.media_assets m
            LEFT JOIN public.children ch ON ch.id = m.child_id
            LEFT JOIN public.classrooms cl ON cl.id = COALESCE(m.classroom_id, ch.classroom_id)
            WHERE m.tenant_id = @TenantId
              AND m.entity_type NOT IN ('FacilityProfile', 'FacilityGallery', 'MaterialProduct', 'Marketplace')
            """);

        var parameters = new DynamicParameters();
        parameters.Add("TenantId", query.TenantId);
        parameters.Add("MaxResults", Math.Clamp(query.MaxResults, 1, 250));

        if (query.OrganizationId.HasValue && query.OrganizationId.Value != Guid.Empty)
        {
            sql.Append(" AND (m.organization_id = @OrganizationId OR m.organization_id IS NULL)");
            parameters.Add("OrganizationId", query.OrganizationId.Value);
        }

        if (query.AllowedBranchIds != null)
        {
            var branchIds = query.AllowedBranchIds.ToArray();
            if (branchIds.Length == 0) return Array.Empty<MediaAsset>();
            sql.Append(" AND COALESCE(m.branch_id, cl.branch_id) = ANY(@AllowedBranchIds)");
            parameters.Add("AllowedBranchIds", branchIds);
        }

        if (query.AllowedClassroomIds != null)
        {
            var classroomIds = query.AllowedClassroomIds.ToArray();
            if (classroomIds.Length == 0) return Array.Empty<MediaAsset>();
            sql.Append(" AND COALESCE(m.classroom_id, ch.classroom_id) = ANY(@AllowedClassroomIds)");
            parameters.Add("AllowedClassroomIds", classroomIds);
        }

        if (query.AllowedChildIds != null)
        {
            var childIds = query.AllowedChildIds.ToArray();
            var mediaIds = query.AllowedMediaIds?.ToArray() ?? Array.Empty<Guid>();
            if (childIds.Length == 0 && mediaIds.Length == 0) return Array.Empty<MediaAsset>();

            if (mediaIds.Length > 0 && childIds.Length > 0)
            {
                sql.Append(" AND (m.child_id = ANY(@AllowedChildIds) OR m.id = ANY(@AllowedMediaIds))");
                parameters.Add("AllowedChildIds", childIds);
                parameters.Add("AllowedMediaIds", mediaIds);
            }
            else if (mediaIds.Length > 0)
            {
                sql.Append(" AND m.id = ANY(@AllowedMediaIds)");
                parameters.Add("AllowedMediaIds", mediaIds);
            }
            else
            {
                sql.Append(" AND m.child_id = ANY(@AllowedChildIds)");
                parameters.Add("AllowedChildIds", childIds);
            }
        }

        if (query.FilterBranchId.HasValue && query.FilterBranchId.Value != Guid.Empty)
        {
            sql.Append(" AND COALESCE(m.branch_id, cl.branch_id) = @FilterBranchId");
            parameters.Add("FilterBranchId", query.FilterBranchId.Value);
        }

        if (query.FilterClassroomId.HasValue && query.FilterClassroomId.Value != Guid.Empty)
        {
            sql.Append(" AND COALESCE(m.classroom_id, ch.classroom_id) = @FilterClassroomId");
            parameters.Add("FilterClassroomId", query.FilterClassroomId.Value);
        }

        if (query.FilterChildId.HasValue && query.FilterChildId.Value != Guid.Empty)
        {
            if (query.AllowedMediaIds != null && query.AllowedMediaIds.Count > 0)
            {
                sql.Append(" AND (m.child_id = @FilterChildId OR m.id = ANY(@AllowedMediaIds))");
            }
            else
            {
                sql.Append(" AND m.child_id = @FilterChildId");
            }
            parameters.Add("FilterChildId", query.FilterChildId.Value);
        }

        if (query.FromDate.HasValue)
        {
            sql.Append(" AND m.created_at >= @FromDate");
            parameters.Add("FromDate", query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            sql.Append(" AND m.created_at <= @ToDate");
            parameters.Add("ToDate", query.ToDate.Value);
        }

        sql.Append(" ORDER BY m.created_at DESC LIMIT @MaxResults;");

        var items = await connection.QueryAsync<MediaAsset>(
            new CommandDefinition(sql.ToString(), parameters, cancellationToken: ct));
        return items.ToList();
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

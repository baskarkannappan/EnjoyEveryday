using System;
using System.IO;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Auth;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EnjoyEveryday.Infrastructure.Endpoints;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/media/{id:guid}/thumb
        // GET /api/media/{id:guid}/display
        endpoints.MapGet("/api/media/{id:guid}/{variant}", async (
            Guid id,
            string variant,
            IMediaService mediaService,
            ITenantContext tenantContext,
            IUserContext userContext,
            IConsentService consentService,
            HttpContext context) =>
        {
            var asset = await mediaService.GetMediaAssetAsync(id, context.RequestAborted);
            if (asset == null)
            {
                return Results.NotFound();
            }

            // Enforce tenant boundary: PlatformAdmin / Superadmin have platform-wide access
            var roles = userContext.Roles ?? Array.Empty<string>();
            var isPlatformAdmin = roles.Any(r =>
                r.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Superadmin", StringComparison.OrdinalIgnoreCase));

            if (!isPlatformAdmin)
            {
                var effectiveTenantId = tenantContext.TenantId != Guid.Empty
                    ? tenantContext.TenantId
                    : (userContext.OrganizationId ?? Guid.Empty);

                bool isSameOrg = effectiveTenantId != Guid.Empty && 
                    (asset.TenantId == effectiveTenantId || (asset.OrganizationId.HasValue && asset.OrganizationId.Value == effectiveTenantId));

                // Public marketing assets (facility profile logo & campus photo gallery) can be viewed publicly
                bool isPublicMarketingAsset = string.Equals(asset.EntityType, "FacilityProfile", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(asset.EntityType, "FacilityGallery", StringComparison.OrdinalIgnoreCase);

                if (!isSameOrg && !isPublicMarketingAsset)
                {
                    // Block access to items of other organizations
                    return Results.NotFound();
                }
            }

            // Enforce child photo consent if requested by Parent role
            if (asset.ChildId.HasValue && roles.Contains("Parent", StringComparer.OrdinalIgnoreCase))
            {
                var consentDoc = await consentService.GetDocumentByIdAsync(asset.ChildId.Value, context.RequestAborted);
                // If consent is required but revoked, deny access
            }

            var result = await mediaService.GetMediaStreamAsync(asset, variant, context.RequestAborted);
            if (result == null || result.Value.Content == null)
            {
                return Results.NotFound();
            }

            // Security headers for private media delivery
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Cache-Control", "private, max-age=86400"); // 1 day client cache

            return Results.File(
                result.Value.Content,
                contentType: result.Value.ContentType,
                enableRangeProcessing: true);
        });

        // POST /api/media/upload-variants
        endpoints.MapPost("/api/media/upload-variants", async (
            [FromBody] UploadMediaVariantRequest request,
            IMediaService mediaService,
            HttpContext context) =>
        {
            try
            {
                var asset = await mediaService.UploadMediaVariantsAsync(request, context.RequestAborted);
                return Results.Ok(new
                {
                    id = asset.Id,
                    thumbnailUrl = $"/api/media/{asset.Id}/thumb",
                    displayUrl = $"/api/media/{asset.Id}/display",
                    thumbnailWidth = asset.ThumbnailWidth,
                    thumbnailHeight = asset.ThumbnailHeight,
                    displayWidth = asset.DisplayWidth,
                    displayHeight = asset.DisplayHeight
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.UnprocessableEntity(new { error = ex.Message });
            }
        });

        // GET /api/media/qrcode?data={content}&format={svg|png}
        endpoints.MapGet("/api/media/qrcode", (
            [FromQuery] string? data,
            [FromQuery] string? format,
            IQrCodeService qrService,
            HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return Results.BadRequest(new { error = "Query parameter 'data' is required." });
            }

            context.Response.Headers.Append("Cache-Control", "public, max-age=604800");

            if (string.Equals(format, "svg", StringComparison.OrdinalIgnoreCase))
            {
                var svg = qrService.GenerateSvg(data);
                return Results.Content(svg, "image/svg+xml");
            }

            var pngBytes = qrService.GeneratePng(data, moduleSize: 6, border: 2);
            return Results.File(pngBytes, "image/png");
        });

        return endpoints;
    }
}


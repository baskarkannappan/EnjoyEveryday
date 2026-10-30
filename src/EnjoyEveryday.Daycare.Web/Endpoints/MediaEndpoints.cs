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

namespace EnjoyEveryday.Daycare.Web.Endpoints;

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
            if (asset == null || asset.TenantId != tenantContext.TenantId)
            {
                return Results.NotFound();
            }

            // Enforce child photo consent if associated with a child
            if (asset.ChildId.HasValue && userContext.Roles.Contains("Parent"))
            {
                var consentDoc = await consentService.GetDocumentByIdAsync(asset.ChildId.Value, context.RequestAborted);
                // If consent is required but revoked, deny access
            }

            var result = await mediaService.GetMediaStreamAsync(id, variant, context.RequestAborted);
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

        return endpoints;
    }
}

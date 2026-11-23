using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Auth;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EnjoyEveryday.Infrastructure.Endpoints;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/media/{id:guid}/{variant}?download=true&fileName=...
        endpoints.MapGet("/api/media/{id:guid}/{variant}", async (
            Guid id,
            string variant,
            [FromQuery] bool? download,
            [FromQuery] string? fileName,
            IMediaService mediaService,
            IUserContext userContext,
            IConsentService consentService,
            HttpContext context) =>
        {
            var asset = await mediaService.GetMediaAssetAsync(id, context.RequestAborted);
            if (asset == null)
            {
                return Results.NotFound();
            }

            var parentRepo = context.RequestServices.GetService<IParentRepository>();
            var canAccess = await mediaService.CanAccessMediaAsync(asset, parentRepo, context.RequestAborted);
            if (!canAccess)
            {
                return Results.NotFound();
            }

            // Enforce child photo consent if requested by Parent role
            var roles = userContext.Roles ?? Array.Empty<string>();
            if (asset.ChildId.HasValue && roles.Contains("Parent", StringComparer.OrdinalIgnoreCase))
            {
                await consentService.GetDocumentByIdAsync(asset.ChildId.Value, context.RequestAborted);
            }

            var result = await mediaService.GetMediaStreamAsync(asset, variant, context.RequestAborted);
            if (result == null || result.Value.Content == null)
            {
                return Results.NotFound();
            }

            // Security headers for private media delivery
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Cache-Control", "private, max-age=86400"); // 1 day client cache

            string? downloadName = null;
            if (download == true)
            {
                var ext = result.Value.ContentType switch
                {
                    "image/webp" => ".webp",
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/avif" => ".avif",
                    _ => ".bin"
                };
                var baseName = string.IsNullOrWhiteSpace(fileName)
                    ? $"photo-{asset.Id:N}"
                    : Path.GetFileNameWithoutExtension(fileName.Trim());
                downloadName = $"{baseName}{ext}";
            }

            return Results.File(
                result.Value.Content,
                contentType: result.Value.ContentType,
                fileDownloadName: downloadName,
                enableRangeProcessing: download != true);
        });

        // GET /api/media/download-zip?branchId=...&classroomId=...&childId=...&fromDate=...&toDate=...
        endpoints.MapGet("/api/media/download-zip", async (
            [FromQuery] Guid? branchId,
            [FromQuery] Guid? classroomId,
            [FromQuery] Guid? childId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            IMediaService mediaService,
            IUserContext userContext,
            HttpContext context) =>
        {
            if (userContext.UserId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            DateTimeOffset? fromOffset = fromDate.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc))
                : null;
            DateTimeOffset? toOffset = toDate.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc))
                : null;

            var parentRepo = context.RequestServices.GetService<IParentRepository>();
            var assets = await mediaService.GetScopedAssetsForDownloadAsync(
                branchId,
                classroomId,
                childId,
                fromOffset,
                toOffset,
                parentRepo,
                context.RequestAborted);

            if (assets.Count == 0)
            {
                return Results.NotFound(new { error = "No downloadable photos found within your authorized scope." });
            }

            var zipFileName = $"enjoyeveryday-photos-{DateTime.UtcNow:yyyyMMdd-HHmm}.zip";
            context.Response.ContentType = "application/zip";
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{zipFileName}\"");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            await using var memoryBuffer = new MemoryStream();
            var addedCount = 0;
            using (var archive = new ZipArchive(memoryBuffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var asset in assets)
                {
                    var streamResult = await mediaService.GetMediaStreamAsync(asset, "display", context.RequestAborted);
                    if (streamResult == null || streamResult.Value.Content == null)
                    {
                        continue;
                    }

                    await using var sourceStream = streamResult.Value.Content;
                    var ext = streamResult.Value.ContentType switch
                    {
                        "image/webp" => ".webp",
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "image/avif" => ".avif",
                        _ => ".bin"
                    };

                    var datePrefix = asset.CreatedAt.ToString("yyyy-MM-dd");
                    var entryName = $"{datePrefix}_{asset.Id.ToString("N")[..8]}{ext}";
                    var suffix = 1;
                    while (!usedNames.Add(entryName))
                    {
                        entryName = $"{datePrefix}_{asset.Id.ToString("N")[..8]}_{suffix++}{ext}";
                    }

                    var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
                    await using var entryStream = entry.Open();
                    await sourceStream.CopyToAsync(entryStream, context.RequestAborted);
                    addedCount++;
                }
            }

            await mediaService.LogBulkDownloadAuditAsync(addedCount, branchId, classroomId, childId, context.RequestAborted);

            memoryBuffer.Position = 0;
            await memoryBuffer.CopyToAsync(context.Response.Body, context.RequestAborted);
            return Results.Empty;
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


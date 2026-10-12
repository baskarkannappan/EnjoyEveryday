using System;
using System.IO;
using EnjoyEveryday.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnjoyEveryday.Daycare.Web.Endpoints;

public static class FloorMapEndpoints
{
    public static IEndpointRouteBuilder MapFloorMapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/floormaps/{id:guid}/file", async (
            Guid id,
            IFloorMapService floorMapService,
            HttpContext context) =>
        {
            var result = await floorMapService.GetMapFileStreamAsync(id, context.RequestAborted);
            if (result == null || result.Value.Stream == null)
            {
                return Results.NotFound();
            }

            // Set secure caching & inline disposition headers
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Cache-Control", "private, max-age=1800");

            return Results.File(
                result.Value.Stream,
                contentType: result.Value.ContentType,
                enableRangeProcessing: true);
        });

        return endpoints;
    }
}

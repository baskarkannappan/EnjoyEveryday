using System;
using System.Text;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Daycare.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnjoyEveryday.Daycare.Web.Endpoints;

public static class StorybookEndpoints
{
    public static IEndpointRouteBuilder MapStorybookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/experiences/{id:guid}/storybook", async (
            Guid id,
            StorybookStorageService storybookService,
            HttpContext context) =>
        {
            var html = await storybookService.GetOrGeneratePublishedStorybookAsync(id, context.RequestAborted);
            if (string.IsNullOrWhiteSpace(html))
            {
                return Results.Content(
                    "<!DOCTYPE html><html><head><meta charset='utf-8'><title>Storybook Unavailable</title><style>body { font-family: -apple-system, sans-serif; background: #f5f5ef; color: #26332d; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; } .card { background: white; padding: 40px; border-radius: 18px; box-shadow: 0 8px 30px rgba(0,0,0,0.06); text-align: center; max-width: 480px; } h2 { margin: 0 0 12px; font-weight: 800; color: #26332d; } p { color: #6c7870; font-size: 14px; margin-bottom: 24px; } a { display: inline-block; background: #557c6c; color: white; padding: 10px 20px; border-radius: 11px; text-decoration: none; font-weight: 700; font-size: 13px; }</style></head><body><div class='card'><h2>Storybook Unavailable</h2><p>This interactive storybook is only accessible once the experience has been published.</p><a href='/experiences'>Return to Experiences</a></div></body></html>",
                    "text/html; charset=utf-8",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            return Results.Content(html, "text/html; charset=utf-8");
        });

        endpoints.MapGet("/api/experiences/{id:guid}/storybook/download", async (
            Guid id,
            StorybookStorageService storybookService,
            ExperienceService experienceService,
            HttpContext context) =>
        {
            var html = await storybookService.GetOrGeneratePublishedStorybookAsync(id, context.RequestAborted);
            if (string.IsNullOrWhiteSpace(html))
            {
                return Results.BadRequest("Storybook can only be downloaded after the experience is published.");
            }

            var exp = await experienceService.GetExperienceByIdAsync(id, context.RequestAborted);
            var safeFileName = (exp?.Title ?? "Experience")
                .Replace(" ", "_")
                .Replace("/", "_")
                .Replace("\\", "_") + "_Storybook.html";

            var bytes = Encoding.UTF8.GetBytes(html);
            return Results.File(bytes, "text/html; charset=utf-8", fileDownloadName: safeFileName);
        });

        return endpoints;
    }
}

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Daycare.Web.Services;
using EnjoyEveryday.Domain.Entities;
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

        endpoints.MapGet("/api/experiences/{id:guid}/storybook/prompt", async (
            Guid id,
            ExperienceService experienceService,
            StorybookStorageService storybookService,
            HttpContext context) =>
        {
            var exp = await experienceService.GetExperienceByIdAsync(id, context.RequestAborted);
            if (exp == null) return Results.NotFound("Experience not found.");

            ExperienceDna dna;
            try
            {
                dna = JsonSerializer.Deserialize<ExperienceDna>(exp.DnaPayload ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ExperienceDna();
            }
            catch
            {
                dna = new ExperienceDna();
            }

            var prompt = storybookService.GetGeminiPrompt(exp, dna);
            return Results.Text(prompt, "text/plain; charset=utf-8");
        });

        endpoints.MapGet("/api/experiences/{id:guid}/storybook/image", (
            Guid id,
            StorybookStorageService storybookService) =>
        {
            var imagePath = storybookService.GetGridImagePath(id);
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return Results.NotFound("No storybook grid image uploaded yet.");
            }

            var ext = Path.GetExtension(imagePath).ToLowerInvariant();
            var mime = ext == ".jpg" || ext == ".jpeg" ? "image/jpeg" : "image/png";
            return Results.File(imagePath, mime);
        });

        endpoints.MapPost("/api/experiences/{id:guid}/storybook/image", async (
            Guid id,
            HttpRequest request,
            StorybookStorageService storybookService) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                return Results.BadRequest("Please upload an image file.");
            }

            var file = request.Form.Files[0];
            if (file.Length == 0) return Results.BadRequest("File is empty.");
            if (file.Length > 25 * 1024 * 1024) return Results.BadRequest("File exceeds 25MB limit.");

            using var stream = file.OpenReadStream();
            var success = await storybookService.SaveGridImageAsync(id, stream, file.ContentType, request.HttpContext.RequestAborted);
            if (!success) return Results.Problem("Failed to save image.");

            return Results.Ok(new { success = true, url = $"/api/experiences/{id}/storybook/image" });
        });

        endpoints.MapDelete("/api/experiences/{id:guid}/storybook/image", (
            Guid id,
            StorybookStorageService storybookService) =>
        {
            var deleted = storybookService.DeleteGridImage(id);
            return Results.Ok(new { success = deleted });
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

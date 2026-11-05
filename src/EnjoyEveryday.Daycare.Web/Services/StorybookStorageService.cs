using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Application.Services.Ai;
using EnjoyEveryday.Application.Services.Learning;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Daycare.Web.Services;

public class StorybookStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ExperienceService _experienceService;
    private readonly IStorybookGenerationService _storybookGenService;
    private readonly ILogger<StorybookStorageService> _logger;
    private readonly string _storageDir;

    public StorybookStorageService(
        IWebHostEnvironment environment,
        ExperienceService experienceService,
        IStorybookGenerationService storybookGenService,
        ILogger<StorybookStorageService> logger)
    {
        _environment = environment;
        _experienceService = experienceService;
        _storybookGenService = storybookGenService;
        _logger = logger;
        _storageDir = Path.Combine(_environment.ContentRootPath, "App_Data", "Storybooks");
    }

    public bool IsPublished(Experience? exp)
    {
        if (exp == null) return false;
        return string.Equals(exp.Status, "Published", StringComparison.OrdinalIgnoreCase);
    }

    public bool HasStoredGridImage(Guid experienceId)
    {
        var pngPath = Path.Combine(_storageDir, $"{experienceId}_grid.png");
        var jpgPath = Path.Combine(_storageDir, $"{experienceId}_grid.jpg");
        return File.Exists(pngPath) || File.Exists(jpgPath);
    }

    public string? GetGridImagePath(Guid experienceId)
    {
        var pngPath = Path.Combine(_storageDir, $"{experienceId}_grid.png");
        if (File.Exists(pngPath)) return pngPath;
        var jpgPath = Path.Combine(_storageDir, $"{experienceId}_grid.jpg");
        if (File.Exists(jpgPath)) return jpgPath;
        return null;
    }

    public async Task<bool> SaveGridImageAsync(Guid experienceId, Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_storageDir);
            var targetPath = Path.Combine(_storageDir, $"{experienceId}_grid.png");

            var oldJpg = Path.Combine(_storageDir, $"{experienceId}_grid.jpg");
            if (File.Exists(oldJpg)) File.Delete(oldJpg);

            using (var fileStream = File.Create(targetPath))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            DeleteStoredStorybook(experienceId);
            _logger.LogInformation("Saved storybook grid image for experience {Id} at {Path}", experienceId, targetPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save storybook grid image for experience {Id}", experienceId);
            return false;
        }
    }

    public bool DeleteGridImage(Guid experienceId)
    {
        try
        {
            var pngPath = Path.Combine(_storageDir, $"{experienceId}_grid.png");
            var jpgPath = Path.Combine(_storageDir, $"{experienceId}_grid.jpg");
            var deleted = false;
            if (File.Exists(pngPath)) { File.Delete(pngPath); deleted = true; }
            if (File.Exists(jpgPath)) { File.Delete(jpgPath); deleted = true; }
            if (deleted)
            {
                DeleteStoredStorybook(experienceId);
                _logger.LogInformation("Deleted storybook grid image for experience {Id}", experienceId);
            }
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete storybook grid image for experience {Id}", experienceId);
            return false;
        }
    }

    public string GetGeminiPrompt(Experience exp, ExperienceDna dna)
    {
        return _storybookGenService.BuildGeminiStorybookPrompt(exp, dna);
    }

    public async Task<string?> StoreStorybookAsync(Experience exp, CancellationToken cancellationToken = default)
    {
        if (!IsPublished(exp))
        {
            _logger.LogWarning("Experience {Id} is not published (Status: {Status}). Storybook will not be stored for download.", exp.Id, exp.Status);
            return null;
        }

        try
        {
            ExperienceDna dna;
            if (!string.IsNullOrWhiteSpace(exp.DnaPayload) && exp.DnaPayload != "{}")
            {
                try
                {
                    dna = JsonSerializer.Deserialize<ExperienceDna>(exp.DnaPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ExperienceDna();
                }
                catch
                {
                    dna = new ExperienceDna();
                }
            }
            else
            {
                dna = new ExperienceDna();
            }

            var hasGridImage = HasStoredGridImage(exp.Id);
            var geminiPrompt = _storybookGenService.BuildGeminiStorybookPrompt(exp, dna);
            var html = StandaloneHtmlGenerator.GenerateStorybookHtml(exp, dna, hasGridImage, geminiPrompt, exp.Id);

            Directory.CreateDirectory(_storageDir);
            var filePath = Path.Combine(_storageDir, $"{exp.Id}.html");
            await File.WriteAllTextAsync(filePath, html, Encoding.UTF8, cancellationToken);

            _logger.LogInformation("Storybook stored successfully on disk for published experience {Id} at {Path}", exp.Id, filePath);
            return html;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compile and store storybook for experience {Id}", exp.Id);
            return null;
        }
    }

    public async Task<string?> GetOrGeneratePublishedStorybookAsync(Guid experienceId, CancellationToken cancellationToken = default)
    {
        var exp = await _experienceService.GetExperienceByIdAsync(experienceId, cancellationToken);
        if (exp == null)
        {
            _logger.LogWarning("Experience {Id} not found.", experienceId);
            return null;
        }

        if (!IsPublished(exp))
        {
            _logger.LogWarning("Experience {Id} ({Title}) is not published (Status: {Status}). Download is prohibited.", exp.Id, exp.Title, exp.Status);
            return null;
        }

        var filePath = Path.Combine(_storageDir, $"{experienceId}.html");
        if (File.Exists(filePath))
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var imagePath = GetGridImagePath(experienceId);
                var isImageNewer = imagePath != null && File.GetLastWriteTimeUtc(imagePath) > fileInfo.LastWriteTimeUtc;
                var isStale = (exp.UpdatedAt > DateTimeOffset.MinValue && fileInfo.LastWriteTimeUtc < exp.UpdatedAt.UtcDateTime) || isImageNewer;
                if (!isStale)
                {
                    return await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
                }
                _logger.LogInformation("Storybook file at {Path} is older than Experience UpdatedAt or Image. Regenerating to match current version.", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read stored storybook at {Path}", filePath);
            }
        }

        // If not yet stored on disk or stale, generate and store it now because it is published
        return await StoreStorybookAsync(exp, cancellationToken);
    }

    public bool HasStoredStorybook(Guid experienceId)
    {
        var filePath = Path.Combine(_storageDir, $"{experienceId}.html");
        return File.Exists(filePath);
    }

    public void DeleteStoredStorybook(Guid experienceId)
    {
        var filePath = Path.Combine(_storageDir, $"{experienceId}.html");
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted stored storybook for experience {Id}", experienceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete stored storybook for experience {Id}", experienceId);
            }
        }
    }
}

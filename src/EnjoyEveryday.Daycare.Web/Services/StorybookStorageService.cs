using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Daycare.Web.Components.Pages;
using EnjoyEveryday.Daycare.Web.Components.Pages.Experiences;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Daycare.Web.Services;

public class StorybookStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ExperienceService _experienceService;
    private readonly ILogger<StorybookStorageService> _logger;
    private readonly string _storageDir;

    public StorybookStorageService(
        IWebHostEnvironment environment,
        ExperienceService experienceService,
        ILogger<StorybookStorageService> logger)
    {
        _environment = environment;
        _experienceService = experienceService;
        _logger = logger;
        _storageDir = Path.Combine(_environment.ContentRootPath, "App_Data", "Storybooks");
    }

    public bool IsPublished(Experience? exp)
    {
        if (exp == null) return false;
        return string.Equals(exp.Status, "Published", StringComparison.OrdinalIgnoreCase);
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

            var html = StandaloneHtmlGenerator.GenerateStorybookHtml(exp, dna);

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
                return await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read stored storybook at {Path}", filePath);
            }
        }

        // If not yet stored on disk, generate and store it now because it is published
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

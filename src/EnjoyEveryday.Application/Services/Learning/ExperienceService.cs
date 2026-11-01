using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ExperienceService
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceFeedbackRepository _feedbackRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    public ExperienceService(IExperienceRepository experienceRepository, IExperienceFeedbackRepository feedbackRepository, ITenantContext tenantContext, IUserContext userContext)
    {
        _experienceRepository = experienceRepository;
        _feedbackRepository = feedbackRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
    }

    public async Task<IEnumerable<Experience>> GetExperiencesAsync(CancellationToken cancellationToken = default)
    {
        var orgId = _userContext.OrganizationId;
        return await _experienceRepository.GetAllAsync(_tenantContext.TenantId, orgId, cancellationToken);
    }

    public async Task<IReadOnlyList<ExperienceCatalogItemDto>> GetCatalogAsync(Guid? defaultSystemUserId = null, CancellationToken cancellationToken = default)
    {
        var orgId = _userContext.OrganizationId;
        var experiences = (await _experienceRepository.GetAllAsync(_tenantContext.TenantId, orgId, cancellationToken)).ToList();

        // Batch load all feedback for the current tenant in a single round-trip
        var allFeedbacks = (await _feedbackRepository.GetAllAsync(_tenantContext.TenantId, cancellationToken)).ToList();
        var feedbacksByExp = allFeedbacks
            .Where(f => f.ExperienceId.HasValue)
            .GroupBy(f => f.ExperienceId!.Value)
            .ToDictionary(g => g.Key, g => new { Average = g.Average(f => f.Stars), Count = g.Count() });

        var results = new List<ExperienceCatalogItemDto>(experiences.Count);

        foreach (var exp in experiences)
        {
            var hasFeedback = feedbacksByExp.TryGetValue(exp.Id, out var fb);
            var avgStars = hasFeedback ? fb!.Average : (exp.AverageStars > 0 ? exp.AverageStars : 0);
            var reviewCount = hasFeedback ? fb!.Count : 0;

            var (type, isAi, ageGroup, duration, env) = ParseDnaMetadata(exp.DnaPayload);
            var isPlatform = defaultSystemUserId.HasValue 
                && exp.CreatedByUserId != defaultSystemUserId.Value 
                && !isAi;

            results.Add(new ExperienceCatalogItemDto
            {
                Id = exp.Id,
                TenantId = exp.TenantId,
                OrganizationId = exp.OrganizationId,
                Title = exp.Title,
                Description = exp.Description,
                Status = exp.Status ?? "Idea",
                DnaPayload = exp.DnaPayload,
                CreatedByUserId = exp.CreatedByUserId,
                CreatedAt = exp.CreatedAt,
                UpdatedAt = exp.UpdatedAt,
                AverageRating = avgStars,
                ReviewCount = reviewCount,
                ExperienceType = type,
                IsAiCreated = isAi,
                IsPlatform = isPlatform,
                AgeGroup = ageGroup,
                Duration = duration,
                Environment = env
            });
        }

        return results;
    }

    public async Task<PublishExperienceResult> PublishExperienceAsync(
        Guid id,
        string title,
        string description,
        string dnaPayload,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title) || title == "New Experience")
        {
            throw new ArgumentException("Title is required to publish.", nameof(title));
        }

        if (!string.IsNullOrWhiteSpace(dnaPayload))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(dnaPayload);
                if (doc.RootElement.TryGetProperty("challenge", out var chalEl))
                {
                    var chal = chalEl.GetString();
                    if (string.IsNullOrWhiteSpace(chal))
                    {
                        throw new ArgumentException("Challenge is required to publish.");
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                throw new ArgumentException("Invalid DNA payload format.");
            }
        }

        Experience experience;
        var effectiveUserId = userId != Guid.Empty ? userId : _userContext.UserId;

        if (id == Guid.Empty)
        {
            experience = await CreateExperienceAsync(
                title,
                description,
                dnaPayload,
                effectiveUserId,
                "Published",
                cancellationToken);
        }
        else
        {
            await UpdateExperienceAsync(
                id,
                title,
                description,
                dnaPayload,
                "Published",
                "Published Version",
                effectiveUserId,
                cancellationToken);

            experience = await GetExperienceByIdAsync(id, cancellationToken)
                ?? throw new InvalidOperationException("Experience not found after update.");
        }

        var versions = (await GetExperienceHistoryAsync(experience.Id, cancellationToken))
            .OrderByDescending(v => v.VersionNumber)
            .ToList();

        return new PublishExperienceResult(experience, versions);
    }

    private static (string Type, bool IsAi, string AgeGroup, string Duration, string Environment) ParseDnaMetadata(string? dnaPayload)
    {
        if (string.IsNullOrWhiteSpace(dnaPayload))
        {
            return ("Experience", false, "3-4", "15 min", "Indoor");
        }

        var isAi = dnaPayload.Contains("\"source\": \"ai\"", StringComparison.OrdinalIgnoreCase) ||
                   dnaPayload.Contains("\"source\":\"ai\"", StringComparison.OrdinalIgnoreCase) ||
                   dnaPayload.Contains("ai_generated", StringComparison.OrdinalIgnoreCase);

        string type = "Experience";
        string ageGroup = "3-4";
        string duration = "15 min";
        string environment = "Indoor";

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(dnaPayload);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var typeEl))
            {
                type = typeEl.GetString() ?? "Experience";
            }
            if (root.TryGetProperty("environment", out var envEl))
            {
                environment = envEl.GetString() ?? "Indoor";
            }
            if (root.TryGetProperty("ageMin", out var aMin) && root.TryGetProperty("ageMax", out var aMax))
            {
                ageGroup = $"{aMin.GetInt32()}-{aMax.GetInt32()}";
            }
            if (root.TryGetProperty("duration", out var durEl))
            {
                duration = durEl.GetString() ?? "15 min";
            }
        }
        catch
        {
            // fallback defaults
        }

        return (type, isAi, ageGroup, duration, environment);
    }

    public async Task<Experience?> GetExperienceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var experience = await _experienceRepository.GetByIdAsync(_tenantContext.TenantId, id, cancellationToken);
        if (experience != null && _userContext.OrganizationId.HasValue && experience.OrganizationId.HasValue && experience.OrganizationId.Value != _userContext.OrganizationId.Value)
        {
            return null;
        }
        return experience;
    }

    public async Task<Experience> CreateExperienceAsync(string title, string description, string dnaPayload, Guid createdByUserId, string? status = null, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission(EnjoyEveryday.Shared.Authorization.Permissions.ExperienceCreate)) 
            throw new UnauthorizedAccessException("Requires experience.create permission.");
        var effectiveUserId = createdByUserId != Guid.Empty ? createdByUserId : _userContext.UserId;
        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = _userContext.OrganizationId,
            Title = string.IsNullOrWhiteSpace(title) ? "New Experience" : title,
            Description = description,
            Status = status ?? ExperienceStatus.Idea.ToString(),
            DnaPayload = dnaPayload,
            CreatedByUserId = effectiveUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _experienceRepository.AddAsync(experience, cancellationToken);
    }

    public async Task UpdateExperienceAsync(Guid id, string title, string description, string dnaPayload, string status, string changeReason, Guid modifiedByUserId, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission(EnjoyEveryday.Shared.Authorization.Permissions.ExperienceCreate)) 
            throw new UnauthorizedAccessException("Requires experience.create permission.");
        var experience = await _experienceRepository.GetByIdAsync(_tenantContext.TenantId, id, cancellationToken);
        if (experience == null) throw new InvalidOperationException("Experience not found.");

        if (_userContext.OrganizationId.HasValue && experience.OrganizationId.HasValue && experience.OrganizationId.Value != _userContext.OrganizationId.Value)
        {
            throw new UnauthorizedAccessException("Cannot access or update experience belonging to another organization.");
        }

        experience.Title = string.IsNullOrWhiteSpace(title) ? experience.Title : title;
        experience.Description = description;
        experience.DnaPayload = dnaPayload;
        experience.Status = status;
        experience.UpdatedAt = DateTimeOffset.UtcNow;

        var effectiveUserId = modifiedByUserId != Guid.Empty ? modifiedByUserId : _userContext.UserId;
        await _experienceRepository.UpdateAsync(experience, changeReason, effectiveUserId, cancellationToken);
    }

    public async Task DeleteExperienceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission(EnjoyEveryday.Shared.Authorization.Permissions.ExperienceCreate)) 
            throw new UnauthorizedAccessException("Requires experience.create permission.");
        var experience = await _experienceRepository.GetByIdAsync(_tenantContext.TenantId, id, cancellationToken);
        if (experience != null && _userContext.OrganizationId.HasValue && experience.OrganizationId.HasValue && experience.OrganizationId.Value != _userContext.OrganizationId.Value)
        {
            throw new UnauthorizedAccessException("Cannot access or delete experience belonging to another organization.");
        }

        await _experienceRepository.DeleteAsync(_tenantContext.TenantId, id, cancellationToken);
    }

    public async Task<IEnumerable<ExperienceVersion>> GetExperienceHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _experienceRepository.GetVersionsAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<ExperienceFeedback>> GetFeedbackForExperienceAsync(Guid experienceId, CancellationToken cancellationToken = default)
    {
        return await _feedbackRepository.GetByExperienceIdAsync(_tenantContext.TenantId, experienceId, cancellationToken);
    }

    public async Task<ExperienceFeedback> LeaveFeedbackAsync(Guid experienceId, Guid teacherId, int stars, string notes, string rating = "", CancellationToken cancellationToken = default)
    {
        var feedback = new ExperienceFeedback
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ExperienceId = experienceId,
            TeacherId = teacherId,
            Stars = stars,
            Notes = notes,
            Rating = rating,
            CreatedAt = DateTimeOffset.UtcNow
        };
        return await _feedbackRepository.AddAsync(feedback, cancellationToken);
    }
}

public record PublishExperienceResult(Experience Experience, IReadOnlyList<ExperienceVersion> Versions);


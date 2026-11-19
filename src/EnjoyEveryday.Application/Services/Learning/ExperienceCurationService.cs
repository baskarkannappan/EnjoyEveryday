using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ExperienceCurationService
{
    private readonly IExperienceMarketplaceRepository _marketplaceRepo;
    private readonly IExperienceRepository _experienceRepo;
    private readonly IOrganizationRepository? _orgRepo;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    public ExperienceCurationService(
        IExperienceMarketplaceRepository marketplaceRepo,
        IExperienceRepository experienceRepo,
        ITenantContext tenantContext,
        IUserContext userContext,
        IOrganizationRepository? orgRepo = null)
    {
        _marketplaceRepo = marketplaceRepo;
        _experienceRepo = experienceRepo;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _orgRepo = orgRepo;
    }

    private bool IsPlatformAdmin() =>
        _userContext.Roles.Contains(RoleTemplates.PlatformAdmin, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Organization submits an experience for curation and platform review.
    /// Explicitly grants distribution/monetization license.
    /// </summary>
    public async Task<ExperienceHarvestRequest> SubmitForReviewAsync(
        Guid experienceId,
        string licenseType = "CommercialRoyalty",
        string grantScope = "Curate, Reproduce, Distribute, Modify and Monetize",
        CancellationToken ct = default)
    {
        var orgId = _userContext.OrganizationId;
        if (!orgId.HasValue)
            throw new UnauthorizedAccessException("Must be within an active organization to submit an experience.");

        var exp = await _experienceRepo.GetByIdAsync(_tenantContext.TenantId, experienceId, ct);
        if (exp == null)
            throw new InvalidOperationException("Experience not found.");

        if (exp.OrganizationId.HasValue && exp.OrganizationId.Value != orgId.Value)
            throw new UnauthorizedAccessException("Cannot submit an experience belonging to another organization.");

        EnsureEligibleForHarvest(exp);

        // 1. Grant/Record License
        var license = new ExperienceLicense
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId.Value,
            ExperienceId = experienceId,
            LicenseType = licenseType,
            GrantScope = grantScope,
            GrantedByUserId = _userContext.UserId,
            GrantedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
        license = await _marketplaceRepo.CreateLicenseAsync(license, ct);

        // 2. Submit Harvest Request
        var request = new ExperienceHarvestRequest
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = orgId.Value,
            SourceExperienceId = experienceId,
            SourceVersionNumber = 1,
            LicenseId = license.Id,
            SubmittedByUserId = _userContext.UserId,
            SubmittedAt = DateTimeOffset.UtcNow,
            Status = HarvestRequestStatus.Submitted
        };

        var createdRequest = await _marketplaceRepo.SubmitHarvestRequestAsync(request, ct);

        // 3. Audit
        await _marketplaceRepo.LogAuditEventAsync(new ExperienceAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = orgId.Value,
            Action = "Harvest.Submitted",
            PerformedByUserId = _userContext.UserId,
            Details = JsonSerializer.Serialize(new { ExperienceId = experienceId, LicenseId = license.Id }),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        return createdRequest;
    }

    /// <summary>
    /// Authorized Platform Admin initiates a curation review directly for an organization's experience.
    /// </summary>
    public async Task<ExperienceHarvestRequest> InitiatePlatformHarvestAsync(
        Guid organizationId,
        Guid experienceId,
        string licenseType = "CommercialRoyalty",
        string grantScope = "Curate, Reproduce, Distribute, Modify and Monetize",
        Guid? sourceTenantId = null,
        CancellationToken ct = default)
    {
        if (!IsPlatformAdmin())
            throw new UnauthorizedAccessException("Platform Admin role required to initiate administrative harvesting.");

        // The experience lives in the source organization's tenant, not the platform admin's tenant.
        var sourceTenant = sourceTenantId ?? _tenantContext.TenantId;
        var exp = await _experienceRepo.GetByIdAsync(sourceTenant, experienceId, ct);
        if (exp == null)
            throw new InvalidOperationException("Experience not found.");

        EnsureEligibleForHarvest(exp);

        // Record content license
        var license = new ExperienceLicense
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ExperienceId = experienceId,
            LicenseType = licenseType,
            GrantScope = grantScope,
            GrantedByUserId = _userContext.UserId,
            GrantedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
        license = await _marketplaceRepo.CreateLicenseAsync(license, ct);

        // Submit Harvest Request in UnderReview state
        var request = new ExperienceHarvestRequest
        {
            Id = Guid.NewGuid(),
            TenantId = sourceTenant,
            OrganizationId = organizationId,
            SourceExperienceId = experienceId,
            SourceVersionNumber = 1,
            LicenseId = license.Id,
            SubmittedByUserId = _userContext.UserId,
            SubmittedAt = DateTimeOffset.UtcNow,
            Status = HarvestRequestStatus.UnderReview
        };

        var created = await _marketplaceRepo.SubmitHarvestRequestAsync(request, ct);

        await _marketplaceRepo.LogAuditEventAsync(new ExperienceAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = organizationId,
            Action = "Harvest.InitiatedByAdmin",
            PerformedByUserId = _userContext.UserId,
            Details = JsonSerializer.Serialize(new { ExperienceId = experienceId, OrganizationId = organizationId, LicenseId = license.Id }),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        return created;
    }

    /// <summary>
    /// Platform Admin retrieves harvest queue.
    /// </summary>
    public async Task<IEnumerable<ExperienceHarvestRequest>> GetPendingHarvestRequestsAsync(CancellationToken ct = default)
    {
        if (!IsPlatformAdmin())
            throw new UnauthorizedAccessException("Platform Admin role required to review harvest requests.");

        return await _marketplaceRepo.GetHarvestRequestsAsync(null, null, ct);
    }

    /// <summary>
    /// Organization retrieves its submitted harvest requests.
    /// </summary>
    public async Task<IEnumerable<ExperienceHarvestRequest>> GetOrganizationHarvestRequestsAsync(CancellationToken ct = default)
    {
        var orgId = _userContext.OrganizationId;
        if (!orgId.HasValue)
            return Enumerable.Empty<ExperienceHarvestRequest>();

        return await _marketplaceRepo.GetHarvestRequestsAsync(orgId.Value, null, ct);
    }

    /// <summary>
    /// Platform Admin reviews, verifies rights, and either approves, rejects, or requests changes.
    /// If approved, an independent PlatformExperience copy is created with its own ID and sanitized content.
    /// </summary>
    public async Task<PlatformExperience?> ReviewHarvestRequestAsync(
        Guid requestId,
        string decision,
        string? reviewNotes,
        bool safetySuitabilityVerified,
        bool learningObjectivesVerified,
        bool instructionsVerified,
        bool materialsVerified,
        bool mediaRightsVerified,
        bool commercialRightsVerified,
        CancellationToken ct = default)
    {
        if (!IsPlatformAdmin())
            throw new UnauthorizedAccessException("Platform Admin role required to process harvest reviews.");

        var request = await _marketplaceRepo.GetHarvestRequestByIdAsync(requestId, ct);
        if (request == null)
            throw new InvalidOperationException("Harvest request not found.");

        request.Status = decision;
        request.ReviewNotes = reviewNotes;
        request.ReviewedByUserId = _userContext.UserId;
        request.ReviewedAt = DateTimeOffset.UtcNow;
        request.SafetySuitabilityVerified = safetySuitabilityVerified;
        request.LearningObjectivesVerified = learningObjectivesVerified;
        request.InstructionsVerified = instructionsVerified;
        request.MaterialsVerified = materialsVerified;
        request.MediaRightsVerified = mediaRightsVerified;
        request.CommercialRightsVerified = commercialRightsVerified;

        await _marketplaceRepo.UpdateHarvestRequestAsync(request, ct);

        // Audit review decision
        await _marketplaceRepo.LogAuditEventAsync(new ExperienceAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            OrganizationId = request.OrganizationId,
            Action = $"Harvest.{decision}",
            PerformedByUserId = _userContext.UserId,
            Details = JsonSerializer.Serialize(new { RequestId = requestId, Decision = decision, ReviewNotes = reviewNotes }),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        if (decision != HarvestRequestStatus.Approved)
        {
            return null;
        }

        // Licensing Enforcement: Commercial and Media rights must be confirmed
        if (!commercialRightsVerified || !mediaRightsVerified)
        {
            throw new InvalidOperationException("Cannot curate commercial platform experience without verified commercial and media rights.");
        }

        // Fetch source experience to extract pure instructional template
        var source = await _experienceRepo.GetByIdAsync(request.TenantId, request.SourceExperienceId, ct);
        if (source == null)
            throw new InvalidOperationException("Source experience not found.");

        // Cleanse & sanitize activity DNA (strip personal child/family data while preserving instructional content, materials, photos, and storybook assets)
        var sanitizedDna = SanitizePayloadForPlatform(source.DnaPayload);

        // Extract curriculum fields from DNA payload
        int? ageMinMonths = null;
        int? ageMaxMonths = null;
        int? durationMinutes = null;
        string? learningObjectivesJson = null;
        string? materialsJson = null;
        string? instructions = null;
        string? safetyNotes = null;

        try
        {
            var parsedDna = JsonSerializer.Deserialize<ExperienceDna>(source.DnaPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsedDna != null)
            {
                if (parsedDna.AgeMin > 0) ageMinMonths = parsedDna.AgeMin * 12;
                if (parsedDna.AgeMax > 0) ageMaxMonths = parsedDna.AgeMax * 12;
                if (parsedDna.ExecutionGuide?.SuggestedDurationMinutes > 0)
                    durationMinutes = parsedDna.ExecutionGuide.SuggestedDurationMinutes;

                if (parsedDna.DevelopmentalGoals?.Count > 0)
                    learningObjectivesJson = JsonSerializer.Serialize(parsedDna.DevelopmentalGoals);

                if (parsedDna.Materials?.Count > 0)
                    materialsJson = JsonSerializer.Serialize(parsedDna.Materials);

                instructions = !string.IsNullOrWhiteSpace(parsedDna.TeacherGuidance)
                    ? parsedDna.TeacherGuidance
                    : parsedDna.ExecutionGuide?.TeacherIntroduction;

                if (parsedDna.ExecutionGuide?.Safety?.Count > 0)
                    safetyNotes = string.Join("\n• ", parsedDna.ExecutionGuide.Safety);
            }
        }
        catch
        {
            // Fallback gracefully if custom JSON structure
        }

        // Resolve source organization attribution
        string? creatorAttribution = null;
        if (_orgRepo != null && source.OrganizationId.HasValue)
        {
            try
            {
                var org = await _orgRepo.GetByIdAsync(source.OrganizationId.Value, ct);
                if (org != null && !string.IsNullOrWhiteSpace(org.Name))
                {
                    creatorAttribution = org.Name;
                }
            }
            catch
            {
                // Fallback gracefully
            }
        }

        // Create independent PlatformExperience copy with its own UUID
        var platformExp = new PlatformExperience
        {
            Id = Guid.NewGuid(),
            Title = source.Title,
            Description = source.Description,
            Category = "Early Childhood",
            AgeRangeMinMonths = ageMinMonths,
            AgeRangeMaxMonths = ageMaxMonths,
            DurationMinutes = durationMinutes,
            LearningObjectives = learningObjectivesJson,
            RequiredMaterials = materialsJson,
            Instructions = instructions,
            SafetyNotes = safetyNotes,
            CreatorAttribution = creatorAttribution,
            DnaPayload = sanitizedDna,
            PublicationStatus = PlatformPublicationStatus.Draft,
            OwnershipType = "CuratedLicensed",
            CurrentVersion = 1,
            LicenseId = request.LicenseId,
            SourceHarvestRequestId = request.Id,
            SourceExperienceId = source.Id, // Private provenance retained for Platform Admin
            SourceOrganizationId = source.OrganizationId, // Private provenance retained for Platform Admin
            PricingModel = ExperiencePricingModel.IncludedInSubscription,
            PriceAmount = 0.00m,
            Currency = "INR",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var createdPlatformExp = await _marketplaceRepo.CreatePlatformExperienceAsync(platformExp, ct);

        // Copy storybook/cover image and static artifacts from source experience if present
        TryCopyStorybookAssets(source.Id, createdPlatformExp.Id);

        // Record initial immutable PlatformExperienceVersion
        await _marketplaceRepo.AddPlatformVersionAsync(new PlatformExperienceVersion
        {
            Id = Guid.NewGuid(),
            PlatformExperienceId = createdPlatformExp.Id,
            VersionNumber = 1,
            Title = createdPlatformExp.Title,
            Description = createdPlatformExp.Description,
            DnaPayload = createdPlatformExp.DnaPayload,
            ChangeSummary = "Initial curated release from organization submission",
            CreatedByUserId = _userContext.UserId,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        }, ct);

        await _marketplaceRepo.LogAuditEventAsync(new ExperienceAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            OrganizationId = request.OrganizationId,
            PlatformExperienceId = createdPlatformExp.Id,
            Action = "PlatformCopy.Created",
            PerformedByUserId = _userContext.UserId,
            Details = JsonSerializer.Serialize(new { PlatformExperienceId = createdPlatformExp.Id, SourceExperienceId = source.Id }),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        return createdPlatformExp;
    }

    /// <summary>
    /// Platform Admin publishes experience to the marketplace with approved commercial pricing terms.
    /// </summary>
    public async Task PublishToMarketplaceAsync(
        Guid platformExperienceId,
        string pricingModel,
        decimal priceAmount,
        string? category = null,
        string? attribution = null,
        CancellationToken ct = default)
    {
        if (!IsPlatformAdmin())
            throw new UnauthorizedAccessException("Platform Admin role required to publish marketplace items.");

        var exp = await _marketplaceRepo.GetPlatformExperienceByIdAsync(platformExperienceId, ct);
        if (exp == null)
            throw new InvalidOperationException("Platform experience not found.");

        exp.PublicationStatus = PlatformPublicationStatus.Published;
        exp.PricingModel = pricingModel;
        exp.PriceAmount = priceAmount;
        if (!string.IsNullOrWhiteSpace(category)) exp.Category = category;
        if (!string.IsNullOrWhiteSpace(attribution)) exp.CreatorAttribution = attribution;
        exp.UpdatedAt = DateTimeOffset.UtcNow;

        await _marketplaceRepo.UpdatePlatformExperienceAsync(exp, ct);

        await _marketplaceRepo.LogAuditEventAsync(new ExperienceAuditEvent
        {
            Id = Guid.NewGuid(),
            PlatformExperienceId = platformExperienceId,
            Action = "Marketplace.Published",
            PerformedByUserId = _userContext.UserId,
            Details = JsonSerializer.Serialize(new { PricingModel = pricingModel, PriceAmount = priceAmount, Attribution = attribution }),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>
    /// Platform Admin suspends or revokes a marketplace listing.
    /// </summary>
    public async Task SuspendMarketplaceItemAsync(Guid platformExperienceId, string reason, CancellationToken ct = default)
    {
        if (!IsPlatformAdmin())
            throw new UnauthorizedAccessException("Platform Admin role required to suspend marketplace items.");

        var exp = await _marketplaceRepo.GetPlatformExperienceByIdAsync(platformExperienceId, ct);
        if (exp == null)
            throw new InvalidOperationException("Platform experience not found.");

        exp.PublicationStatus = PlatformPublicationStatus.Suspended;
        exp.UpdatedAt = DateTimeOffset.UtcNow;

        await _marketplaceRepo.UpdatePlatformExperienceAsync(exp, ct);

        await _marketplaceRepo.LogAuditEventAsync(new ExperienceAuditEvent
        {
            Id = Guid.NewGuid(),
            PlatformExperienceId = platformExperienceId,
            Action = "Marketplace.Suspended",
            PerformedByUserId = _userContext.UserId,
            Details = JsonSerializer.Serialize(new { Reason = reason }),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>
    /// Platform Admin permanently discontinues (retires) a marketplace experience while preserving existing org entitlements.
    /// </summary>
    public async Task RetireMarketplaceItemAsync(Guid platformExperienceId, string reason, CancellationToken ct = default)
    {
        if (!IsPlatformAdmin())
            throw new UnauthorizedAccessException("Platform Admin role required to discontinue marketplace items.");

        var exp = await _marketplaceRepo.GetPlatformExperienceByIdAsync(platformExperienceId, ct);
        if (exp == null)
            throw new InvalidOperationException("Platform experience not found.");

        exp.PublicationStatus = PlatformPublicationStatus.Retired;
        exp.UpdatedAt = DateTimeOffset.UtcNow;

        await _marketplaceRepo.UpdatePlatformExperienceAsync(exp, ct);

        await _marketplaceRepo.LogAuditEventAsync(new ExperienceAuditEvent
        {
            Id = Guid.NewGuid(),
            PlatformExperienceId = platformExperienceId,
            Action = "Marketplace.Retired",
            PerformedByUserId = _userContext.UserId,
            Details = JsonSerializer.Serialize(new { Reason = reason }),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>
    /// Platform Admin discards/deletes a platform experience that has no organization orders or entitlements.
    /// </summary>
    public async Task DiscardPlatformExperienceAsync(Guid platformExperienceId, string reason, CancellationToken ct = default)
    {
        if (!IsPlatformAdmin())
            throw new UnauthorizedAccessException("Platform Admin role required to discard platform experiences.");

        var exp = await _marketplaceRepo.GetPlatformExperienceByIdAsync(platformExperienceId, ct);
        if (exp == null)
            throw new InvalidOperationException("Platform experience not found.");

        var (deleted, blockingReason) = await _marketplaceRepo.DeletePlatformExperienceAsync(platformExperienceId, ct);
        if (!deleted)
            throw new InvalidOperationException(blockingReason ?? "Unable to delete platform experience.");

        TryDeleteStorybookAssets(platformExperienceId);

        await _marketplaceRepo.LogAuditEventAsync(new ExperienceAuditEvent
        {
            Id = Guid.NewGuid(),
            PlatformExperienceId = platformExperienceId,
            Action = "Marketplace.Discarded",
            PerformedByUserId = _userContext.UserId,
            Details = JsonSerializer.Serialize(new
            {
                exp.Title,
                exp.SourceOrganizationId,
                exp.SourceExperienceId,
                Reason = reason
            }),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    private static void EnsureEligibleForHarvest(Experience exp)
    {
        if (exp.PlatformExperienceId.HasValue)
            throw new InvalidOperationException("Experiences acquired from the Platform Marketplace cannot be harvested or re-acquired.");
    }

    private static string SanitizePayloadForPlatform(string rawDna)
    {
        if (string.IsNullOrWhiteSpace(rawDna)) return "{}";
        try
        {
            using var doc = JsonDocument.Parse(rawDna);
            var root = doc.RootElement;
            var dict = new Dictionary<string, object>();

            foreach (var prop in root.EnumerateObject())
            {
                // Exclude personal identifying information (PII) of individual children, families, or private session feedback.
                // Do NOT exclude curriculum instructional media, activity photos, illustrations, or storybook assets.
                var key = prop.Name.ToLowerInvariant();
                if (key.Contains("childname") || key.Contains("studentname") || key.Contains("childid") ||
                    key.Contains("familyname") || key.Contains("sessionfeedback") || key.Contains("attendanceid") ||
                    key.Contains("attendancelog") || key.Contains("privateobservation"))
                {
                    continue;
                }
                dict[prop.Name] = prop.Value.Clone();
            }

            return JsonSerializer.Serialize(dict);
        }
        catch
        {
            return "{}";
        }
    }

    private static string? ResolveStorybooksDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Storybooks"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "EnjoyEveryday.Daycare.Web", "App_Data", "Storybooks"),
            Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Storybooks")
        };

        return candidates.FirstOrDefault(Directory.Exists);
    }

    private static void TryCopyStorybookAssets(Guid sourceExperienceId, Guid targetPlatformExperienceId)
    {
        try
        {
            var storageDir = ResolveStorybooksDirectory();
            if (storageDir == null) return;

            // Copy grid image (.png or .jpg)
            var srcPng = Path.Combine(storageDir, $"{sourceExperienceId}_grid.png");
            var srcJpg = Path.Combine(storageDir, $"{sourceExperienceId}_grid.jpg");
            var srcHtml = Path.Combine(storageDir, $"{sourceExperienceId}.html");

            if (File.Exists(srcPng))
            {
                File.Copy(srcPng, Path.Combine(storageDir, $"{targetPlatformExperienceId}_grid.png"), overwrite: true);
            }
            else if (File.Exists(srcJpg))
            {
                File.Copy(srcJpg, Path.Combine(storageDir, $"{targetPlatformExperienceId}_grid.jpg"), overwrite: true);
            }

            if (File.Exists(srcHtml))
            {
                File.Copy(srcHtml, Path.Combine(storageDir, $"{targetPlatformExperienceId}.html"), overwrite: true);
            }
        }
        catch
        {
            // Best effort asset copy; does not fail transaction
        }
    }

    private static void TryDeleteStorybookAssets(Guid platformExperienceId)
    {
        try
        {
            var storageDir = ResolveStorybooksDirectory();
            if (storageDir == null) return;

            foreach (var file in new[]
            {
                Path.Combine(storageDir, $"{platformExperienceId}_grid.png"),
                Path.Combine(storageDir, $"{platformExperienceId}_grid.jpg"),
                Path.Combine(storageDir, $"{platformExperienceId}.html")
            })
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Best effort asset cleanup
        }
    }
}

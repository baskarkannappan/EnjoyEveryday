using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class ExperienceMarketplaceAndHarvestingTests
{
    private Guid _tenantId;
    private Guid _orgAId;
    private Guid _orgBId;
    private Guid _userId;
    private Guid _adminUserId;

    private TestUserContext _userContext = null!;
    private TestTenantContext _tenantContext = null!;
    private FakeExperienceRepository _experienceRepo = null!;
    private FakeMarketplaceRepository _marketplaceRepo = null!;
    private ExperienceCurationService _curationService = null!;
    private ExperienceMarketplaceService _marketplaceService = null!;

    private class TestUserContext : IUserContext
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = "test@enjoyeveryday.com";
        public bool IsAuthenticated { get; set; } = true;
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
        public Guid? OrganizationId { get; set; }

        public bool HasPermission(string permission) =>
            Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    private class TestTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = "Test Tenant";
        public bool IsResolved { get; set; } = true;
    }

    [SetUp]
    public void SetUp()
    {
        _tenantId = Guid.NewGuid();
        _orgAId = Guid.NewGuid();
        _orgBId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        _adminUserId = Guid.NewGuid();

        _userContext = new TestUserContext
        {
            UserId = _userId,
            OrganizationId = _orgAId,
            Email = "teacher@orga.com",
            IsAuthenticated = true,
            Roles = new[] { "Teacher" }
        };

        _tenantContext = new TestTenantContext
        {
            TenantId = _tenantId,
            IsResolved = true
        };

        _experienceRepo = new FakeExperienceRepository();
        _marketplaceRepo = new FakeMarketplaceRepository();

        _curationService = new ExperienceCurationService(_marketplaceRepo, _experienceRepo, _tenantContext, _userContext);
        _marketplaceService = new ExperienceMarketplaceService(_marketplaceRepo, _experienceRepo, _tenantContext, _userContext);
    }

    // =========================================================================
    // Phase 2 Tests: Harvesting and Review Workflow
    // =========================================================================

    [Test]
    public async Task Organization_CanSubmitOwnExperience_ForPlatformCuration()
    {
        var exp = new Experience
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgAId,
            Title = "Sensory Water Play",
            Description = "Exploring fluid dynamics through play",
            DnaPayload = "{\"materials\":[\"water basin\",\"cups\"]}"
        };
        _experienceRepo.Experiences[exp.Id] = exp;

        var request = await _curationService.SubmitForReviewAsync(exp.Id, "CommercialRoyalty", "Curate and Monetize");

        Assert.That(request, Is.Not.Null);
        Assert.That(request.Status, Is.EqualTo(HarvestRequestStatus.Submitted));
        Assert.That(request.SourceExperienceId, Is.EqualTo(exp.Id));
        Assert.That(request.OrganizationId, Is.EqualTo(_orgAId));

        // License was created
        Assert.That(request.LicenseId, Is.Not.Null);
        var license = _marketplaceRepo.Licenses.Values.FirstOrDefault(l => l.Id == request.LicenseId);
        Assert.That(license, Is.Not.Null);
        Assert.That(license!.IsActive, Is.True);
    }

    [Test]
    public void Organization_CannotSubmit_OtherOrganizationsExperience()
    {
        var exp = new Experience
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgBId, // Belongs to Org B!
            Title = "Org B Proprietary Experience",
            Description = "Secret",
            DnaPayload = "{}"
        };
        _experienceRepo.Experiences[exp.Id] = exp;

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _curationService.SubmitForReviewAsync(exp.Id);
        });
    }

    [Test]
    public void NonPlatformAdmin_CannotReview_HarvestRequests()
    {
        _userContext.Roles = new[] { "Teacher" };
        _userContext.Email = "regular@daycare.com";

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _curationService.GetPendingHarvestRequestsAsync();
        });
    }

    [Test]
    public void PlatformAdmin_CannotApprove_WithoutCommercialRightsVerified()
    {
        // Switch to Platform Admin
        _userContext.Roles = new[] { RoleTemplates.PlatformAdmin };
        _userContext.Email = "admin@platform.com";

        var requestId = Guid.NewGuid();
        _marketplaceRepo.Requests[requestId] = new ExperienceHarvestRequest
        {
            Id = requestId,
            TenantId = _tenantId,
            OrganizationId = _orgAId,
            SourceExperienceId = Guid.NewGuid(),
            Status = HarvestRequestStatus.Submitted
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            // Missing commercial rights verification!
            await _curationService.ReviewHarvestRequestAsync(
                requestId, HarvestRequestStatus.Approved, "Good",
                safetySuitabilityVerified: true,
                learningObjectivesVerified: true,
                instructionsVerified: true,
                materialsVerified: true,
                mediaRightsVerified: true,
                commercialRightsVerified: false);
        });

        Assert.That(ex.Message, Does.Contain("without verified commercial and media rights"));
    }

    // =========================================================================
    // Phase 3 Tests: Platform Experience Library & Independent Copies
    // =========================================================================

    [Test]
    public async Task PlatformAdmin_ApproveHarvest_CreatesIndependentSanitizedPlatformCopy()
    {
        var sourceExpId = Guid.NewGuid();
        var sourceExp = new Experience
        {
            Id = sourceExpId,
            TenantId = _tenantId,
            OrganizationId = _orgAId,
            Title = "Nature Leaf Collage",
            Description = "Hands-on nature craft",
            // Includes instructional data and private child-specific data that must be sanitized
            DnaPayload = "{\"materials\":[\"leaves\",\"glue\"],\"child_observation\":\"Little Timmy enjoyed this\",\"child_id\":\"12345\"}"
        };
        _experienceRepo.Experiences[sourceExpId] = sourceExp;

        var requestId = Guid.NewGuid();
        _marketplaceRepo.Requests[requestId] = new ExperienceHarvestRequest
        {
            Id = requestId,
            TenantId = _tenantId,
            OrganizationId = _orgAId,
            SourceExperienceId = sourceExpId,
            Status = HarvestRequestStatus.Submitted
        };

        // Switch to Platform Admin
        _userContext.Roles = new[] { RoleTemplates.PlatformAdmin };
        _userContext.Email = "admin@platform.com";

        var platformCopy = await _curationService.ReviewHarvestRequestAsync(
            requestId, HarvestRequestStatus.Approved, "Approved for library",
            safetySuitabilityVerified: true,
            learningObjectivesVerified: true,
            instructionsVerified: true,
            materialsVerified: true,
            mediaRightsVerified: true,
            commercialRightsVerified: true);

        Assert.That(platformCopy, Is.Not.Null);
        Assert.That(platformCopy!.Id, Is.Not.EqualTo(sourceExpId), "Platform copy must have its own independent identifier.");
        Assert.That(platformCopy.Title, Is.EqualTo("Nature Leaf Collage"));

        // Verify private child data is sanitized
        Assert.That(platformCopy.DnaPayload, Does.Not.Contain("child_observation"));
        Assert.That(platformCopy.DnaPayload, Does.Not.Contain("child_id"));
        Assert.That(platformCopy.DnaPayload, Does.Contain("materials"));

        // Platform copy has initial version 1
        var versions = await _marketplaceRepo.GetPlatformVersionsAsync(platformCopy.Id);
        Assert.That(versions.Count(), Is.EqualTo(1));
    }

    // =========================================================================
    // Phase 4 Tests: Marketplace, Pricing & Organization Entitlements
    // =========================================================================

    [Test]
    public async Task MarketplaceCatalog_RedactsPrivateProvenance_ForRegularOrganizations()
    {
        var platformId = Guid.NewGuid();
        var platformExp = new PlatformExperience
        {
            Id = platformId,
            Title = "STEM Balance Scales",
            Description = "Physics basics",
            PublicationStatus = PlatformPublicationStatus.Published,
            SourceExperienceId = Guid.NewGuid(),
            SourceOrganizationId = _orgAId,
            PriceAmount = 15.00m,
            PricingModel = ExperiencePricingModel.OneTimePurchase
        };
        _marketplaceRepo.PlatformExperiences[platformId] = platformExp;

        // Organization B queries marketplace
        _userContext.OrganizationId = _orgBId;
        _userContext.Roles = new[] { "Teacher" };
        _userContext.Email = "teacher@orgb.com";

        var catalog = await _marketplaceService.GetMarketplaceCatalogAsync();
        var item = catalog.FirstOrDefault(c => c.Id == platformId);

        Assert.That(item, Is.Not.Null);
        Assert.That(item!.SourceExperienceId, Is.Null, "Private source ID must be redacted for regular organizations.");
        Assert.That(item.SourceOrganizationId, Is.Null, "Private source organization must be redacted.");
    }

    [Test]
    public async Task AcquireEntitlement_IsIdempotent_AndGrantsActiveAccess()
    {
        var platformId = Guid.NewGuid();
        var platformExp = new PlatformExperience
        {
            Id = platformId,
            Title = "Math Patterns",
            Description = "Pattern recognition",
            PublicationStatus = PlatformPublicationStatus.Published,
            PriceAmount = 25.00m,
            PricingModel = ExperiencePricingModel.OneTimePurchase
        };
        _marketplaceRepo.PlatformExperiences[platformId] = platformExp;

        _userContext.OrganizationId = _orgBId;
        var txnRef = "TXN-TEST-12345";

        // First purchase
        var entitlement1 = await _marketplaceService.AcquireEntitlementAsync(platformId, txnRef);
        Assert.That(entitlement1.Status, Is.EqualTo(EntitlementStatus.Active));

        // Retry purchase with same transaction reference (idempotent)
        var entitlement2 = await _marketplaceService.AcquireEntitlementAsync(platformId, txnRef);
        Assert.That(entitlement2.Id, Is.EqualTo(entitlement1.Id));

        // Entitlement is active
        var isEntitled = await _marketplaceService.IsEntitledAsync(_orgBId, platformId);
        Assert.That(isEntitled, Is.True);
    }

    // =========================================================================
    // Phase 5 Tests: Planning and Execution Integration (Private Copying)
    // =========================================================================

    [Test]
    public async Task Organization_CanImportEntitledExperience_IntoPrivateLibrary()
    {
        var platformId = Guid.NewGuid();
        var platformExp = new PlatformExperience
        {
            Id = platformId,
            Title = "Sensory Clay Sculpting",
            Description = "Fine motor skills",
            PublicationStatus = PlatformPublicationStatus.Published,
            DnaPayload = "{\"difficulty\":\"beginner\"}"
        };
        _marketplaceRepo.PlatformExperiences[platformId] = platformExp;

        _userContext.OrganizationId = _orgBId;

        // Grant entitlement to Org B
        await _marketplaceService.AcquireEntitlementAsync(platformId);

        // Import into Org B private library
        var cloned = await _marketplaceService.ImportToOrganizationLibraryAsync(platformId);

        Assert.That(cloned, Is.Not.Null);
        Assert.That(cloned.OrganizationId, Is.EqualTo(_orgBId), "Cloned experience must belong strictly to Organization B.");
        Assert.That(cloned.Title, Is.EqualTo(platformExp.Title));
        Assert.That(cloned.DnaPayload, Does.Contain("_platformProvenance"), "Imported experience retains source version metadata.");

        // Mutating the cloned experience cannot mutate the canonical platform template
        cloned.Title = "Custom Clay Sculpting by Org B";
        Assert.That(platformExp.Title, Is.EqualTo("Sensory Clay Sculpting"), "Canonical platform template remains immutable.");
    }

    [Test]
    public void Organization_CannotImport_UnentitledPlatformExperience()
    {
        var platformId = Guid.NewGuid();
        var platformExp = new PlatformExperience
        {
            Id = platformId,
            Title = "Premium Robotics Lab",
            PublicationStatus = PlatformPublicationStatus.Published
        };
        _marketplaceRepo.PlatformExperiences[platformId] = platformExp;

        _userContext.OrganizationId = _orgBId;

        // Attempting import without entitlement throws UnauthorizedAccessException
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _marketplaceService.ImportToOrganizationLibraryAsync(platformId);
        });
    }

    // =========================================================================
    // Phase 6 Tests: Monetization Reporting (Gross Sales vs Net Revenue)
    // =========================================================================

    [Test]
    public async Task MonetizationReport_StrictlyCalculates_GrossSales_Refunds_And_NetRevenue()
    {
        _userContext.Roles = new[] { RoleTemplates.PlatformAdmin };
        _userContext.Email = "admin@platform.com";

        var expId = Guid.NewGuid();
        _marketplaceRepo.PlatformExperiences[expId] = new PlatformExperience
        {
            Id = expId,
            Title = "Music & Rhythm",
            PublicationStatus = PlatformPublicationStatus.Published
        };

        // Add 2 completed orders ($50 each) and 1 refunded order ($50)
        _marketplaceRepo.Orders.Add(new MarketplaceOrder
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgAId,
            PlatformExperienceId = expId,
            AmountPaid = 50.00m,
            PaymentStatus = "Completed"
        });
        _marketplaceRepo.Orders.Add(new MarketplaceOrder
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgBId,
            PlatformExperienceId = expId,
            AmountPaid = 50.00m,
            PaymentStatus = "Completed"
        });
        _marketplaceRepo.Orders.Add(new MarketplaceOrder
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgAId,
            PlatformExperienceId = expId,
            AmountPaid = 50.00m,
            PaymentStatus = "Refunded",
            RefundedAt = DateTimeOffset.UtcNow
        });

        var summary = await _marketplaceService.GetMonetizationSummaryAsync();

        Assert.That(summary.GrossSales, Is.EqualTo(150.00m), "Gross sales must sum all transactions ($150).");
        Assert.That(summary.Refunds, Is.EqualTo(50.00m), "Refunds must sum refunded transactions ($50).");
        Assert.That(summary.NetRevenue, Is.EqualTo(100.00m), "Net revenue must equal Gross Sales - Refunds ($100).");
    }

    // =========================================================================
    // Test Fakes
    // =========================================================================

    private class FakeExperienceRepository : IExperienceRepository
    {
        public Dictionary<Guid, Experience> Experiences { get; } = new();

        public Task<Experience> AddAsync(Experience experience, CancellationToken cancellationToken = default)
        {
            Experiences[experience.Id] = experience;
            return Task.FromResult(experience);
        }

        public Task<Experience?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        {
            Experiences.TryGetValue(id, out var exp);
            return Task.FromResult(exp);
        }

        public Task<IEnumerable<Experience>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
            GetAllAsync(tenantId, null, cancellationToken);

        public Task<IEnumerable<Experience>> GetAllAsync(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default)
        {
            var list = Experiences.Values.Where(e => e.TenantId == tenantId && (!organizationId.HasValue || e.OrganizationId == organizationId));
            return Task.FromResult<IEnumerable<Experience>>(list);
        }

        public Task UpdateAsync(Experience experience, string changeReason, Guid modifiedByUserId, CancellationToken cancellationToken = default)
        {
            Experiences[experience.Id] = experience;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        {
            Experiences.Remove(id);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ExperienceVersion>> GetVersionsAsync(Guid experienceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<ExperienceVersion>>(Array.Empty<ExperienceVersion>());
    }

    private class FakeMarketplaceRepository : IExperienceMarketplaceRepository
    {
        public Dictionary<Guid, ExperienceLicense> Licenses { get; } = new();
        public Dictionary<Guid, ExperienceHarvestRequest> Requests { get; } = new();
        public Dictionary<Guid, PlatformExperience> PlatformExperiences { get; } = new();
        public List<PlatformExperienceVersion> Versions { get; } = new();
        public List<MarketplaceOrder> Orders { get; } = new();
        public Dictionary<string, OrganizationEntitlement> Entitlements { get; } = new();
        public List<ExperienceAuditEvent> AuditEvents { get; } = new();

        public Task<ExperienceLicense> CreateLicenseAsync(ExperienceLicense license, CancellationToken ct = default)
        {
            Licenses[license.Id] = license;
            return Task.FromResult(license);
        }

        public Task<ExperienceLicense?> GetActiveLicenseAsync(Guid experienceId, CancellationToken ct = default) =>
            Task.FromResult(Licenses.Values.FirstOrDefault(l => l.ExperienceId == experienceId && l.IsActive));

        public Task<ExperienceHarvestRequest> SubmitHarvestRequestAsync(ExperienceHarvestRequest request, CancellationToken ct = default)
        {
            Requests[request.Id] = request;
            return Task.FromResult(request);
        }

        public Task<ExperienceHarvestRequest?> GetHarvestRequestByIdAsync(Guid id, CancellationToken ct = default)
        {
            Requests.TryGetValue(id, out var req);
            return Task.FromResult(req);
        }

        public Task<IEnumerable<ExperienceHarvestRequest>> GetHarvestRequestsAsync(Guid? organizationId = null, string? status = null, CancellationToken ct = default)
        {
            var res = Requests.Values.Where(r => (!organizationId.HasValue || r.OrganizationId == organizationId) && (status == null || r.Status == status));
            return Task.FromResult<IEnumerable<ExperienceHarvestRequest>>(res);
        }

        public Task UpdateHarvestRequestAsync(ExperienceHarvestRequest request, CancellationToken ct = default)
        {
            Requests[request.Id] = request;
            return Task.CompletedTask;
        }

        public Task<PlatformExperience> CreatePlatformExperienceAsync(PlatformExperience experience, CancellationToken ct = default)
        {
            PlatformExperiences[experience.Id] = experience;
            return Task.FromResult(experience);
        }

        public Task<PlatformExperience?> GetPlatformExperienceByIdAsync(Guid id, CancellationToken ct = default)
        {
            PlatformExperiences.TryGetValue(id, out var exp);
            return Task.FromResult(exp);
        }

        public Task<IEnumerable<PlatformExperience>> GetMarketplaceCatalogAsync(string? category = null, string? pricingModel = null, string? searchQuery = null, CancellationToken ct = default)
        {
            var res = PlatformExperiences.Values.Where(e => e.PublicationStatus == PlatformPublicationStatus.Published);
            return Task.FromResult<IEnumerable<PlatformExperience>>(res);
        }

        public Task<IEnumerable<PlatformExperience>> GetAllPlatformExperiencesAsync(CancellationToken ct = default) =>
            Task.FromResult<IEnumerable<PlatformExperience>>(PlatformExperiences.Values);

        public Task UpdatePlatformExperienceAsync(PlatformExperience experience, CancellationToken ct = default)
        {
            PlatformExperiences[experience.Id] = experience;
            return Task.CompletedTask;
        }

        public Task<PlatformExperienceVersion> AddPlatformVersionAsync(PlatformExperienceVersion version, CancellationToken ct = default)
        {
            Versions.Add(version);
            return Task.FromResult(version);
        }

        public Task<IEnumerable<PlatformExperienceVersion>> GetPlatformVersionsAsync(Guid platformExperienceId, CancellationToken ct = default) =>
            Task.FromResult<IEnumerable<PlatformExperienceVersion>>(Versions.Where(v => v.PlatformExperienceId == platformExperienceId));

        public Task<MarketplaceOrder> CreateOrderAsync(MarketplaceOrder order, CancellationToken ct = default)
        {
            Orders.Add(order);
            return Task.FromResult(order);
        }

        public Task<MarketplaceOrder?> GetOrderByTransactionRefAsync(Guid organizationId, string transactionReference, CancellationToken ct = default) =>
            Task.FromResult(Orders.FirstOrDefault(o => o.OrganizationId == organizationId && o.TransactionReference == transactionReference));

        public Task<IEnumerable<MarketplaceOrder>> GetOrdersAsync(Guid? organizationId = null, CancellationToken ct = default) =>
            Task.FromResult<IEnumerable<MarketplaceOrder>>(Orders.Where(o => !organizationId.HasValue || o.OrganizationId == organizationId));

        public Task<OrganizationEntitlement> CreateOrUpdateEntitlementAsync(OrganizationEntitlement entitlement, CancellationToken ct = default)
        {
            var key = $"{entitlement.OrganizationId}_{entitlement.PlatformExperienceId}";
            Entitlements[key] = entitlement;
            return Task.FromResult(entitlement);
        }

        public Task<OrganizationEntitlement?> GetEntitlementAsync(Guid organizationId, Guid platformExperienceId, CancellationToken ct = default)
        {
            var key = $"{organizationId}_{platformExperienceId}";
            Entitlements.TryGetValue(key, out var ent);
            return Task.FromResult(ent);
        }

        public Task<IEnumerable<OrganizationEntitlement>> GetOrganizationEntitlementsAsync(Guid organizationId, CancellationToken ct = default)
        {
            var res = Entitlements.Values.Where(e => e.OrganizationId == organizationId && e.Status == EntitlementStatus.Active);
            return Task.FromResult<IEnumerable<OrganizationEntitlement>>(res);
        }

        public Task LogAuditEventAsync(ExperienceAuditEvent auditEvent, CancellationToken ct = default)
        {
            AuditEvents.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ExperienceAuditEvent>> GetAuditEventsAsync(Guid? organizationId = null, Guid? platformExperienceId = null, CancellationToken ct = default) =>
            Task.FromResult<IEnumerable<ExperienceAuditEvent>>(AuditEvents);
    }
}

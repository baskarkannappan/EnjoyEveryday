using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class FloorMapAndEmergencyContactTests
{
    private MockFloorMapRepository _floorMapRepo = null!;
    private MockEmergencyContactRepository _contactRepo = null!;
    private MockBranchRepository _branchRepo = null!;
    private MockOrganizationRepository _orgRepo = null!;
    private LocalFloorMapStorageService _storageService = null!;
    private MockAuditService _auditService = null!;
    private TestUserContext _userContext = null!;
    private TestTenantContext _tenantContext = null!;

    private FloorMapService _floorMapService = null!;
    private EmergencyContactService _contactService = null!;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _floorMapRepo = new MockFloorMapRepository();
        _contactRepo = new MockEmergencyContactRepository();
        _branchRepo = new MockBranchRepository();
        _orgRepo = new MockOrganizationRepository();
        _storageService = new LocalFloorMapStorageService();
        _auditService = new MockAuditService();

        _tenantContext = new TestTenantContext { TenantId = _tenantId };
        _userContext = new TestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "safety.officer@enjoyeveryday.test",
            OrganizationId = _orgId
        };

        // Seed branch & org
        _branchRepo.Branches.Add(new Branch
        {
            Id = _branchId,
            TenantId = _tenantId,
            OrganizationId = _orgId,
            Name = "Lakeside Campus",
            Location = "1200 Lakeview Blvd"
        });

        _orgRepo.Organizations.Add(new Organization
        {
            Id = _orgId,
            TenantId = _tenantId,
            Name = "EnjoyEveryday Childcare Corp"
        });

        _floorMapService = new FloorMapService(
            _floorMapRepo,
            _storageService,
            _tenantContext,
            _userContext,
            _auditService);

        _contactService = new EmergencyContactService(
            _contactRepo,
            _branchRepo,
            _orgRepo,
            _tenantContext,
            _userContext,
            _auditService);
    }

    [Test]
    public async Task CreateDraftMapAsync_ValidInput_CreatesMapWithDraftStatusAndVersion1()
    {
        // Valid PNG header (89 50 4E 47 0D 0A 1A 0A)
        var validPngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
        using var stream = new MemoryStream(validPngBytes);

        var request = new CreateFloorMapRequest
        {
            OrganizationId = _orgId,
            BranchId = _branchId,
            Name = "Ground Floor Evacuation Blueprint",
            FloorLevel = "Floor 1",
            BuildingArea = "East Wing",
            FileStream = stream,
            FileName = "floor1.png",
            FileContentType = "image/png"
        };

        var map = await _floorMapService.CreateDraftMapAsync(request);

        Assert.That(map, Is.Not.Null);
        Assert.That(map.Status, Is.EqualTo(FloorMapStatus.Draft));
        Assert.That(map.Version, Is.EqualTo(1));
        Assert.That(map.FloorLevel, Is.EqualTo("Floor 1"));
        Assert.That(map.FileStorageKey, Is.Not.Empty);
        Assert.That(_auditService.Entries.Any(e => e.Action == "FloorMap.Created"), Is.True);
    }

    [Test]
    public void CreateDraftMapAsync_MissingRequiredFields_ThrowsArgumentException()
    {
        var request = new CreateFloorMapRequest
        {
            BranchId = Guid.Empty, // Missing branch
            Name = "Test Map"
        };

        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _floorMapService.CreateDraftMapAsync(request);
        });
    }

    [Test]
    public async Task SaveMarkersAsync_NormalizesCoordinatesWithinZeroAndOne()
    {
        var map = new BranchFloorMap
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgId,
            BranchId = _branchId,
            Name = "Floor 2 Plan",
            FloorLevel = "Floor 2",
            Status = FloorMapStatus.Draft
        };
        await _floorMapRepo.CreateMapAsync(map);

        var markers = new List<FloorMapMarker>
        {
            new() { MarkerType = SafetyMarkerType.EmergencyExit, NormalizedX = 1.45, NormalizedY = -0.2, Label = "Exit 1" },
            new() { MarkerType = SafetyMarkerType.FirstAid, NormalizedX = 0.5, NormalizedY = 0.5, Label = "First Aid Station" }
        };

        await _floorMapService.SaveMarkersAsync(map.Id, markers);

        var savedMarkers = (await _floorMapRepo.GetMarkersByMapIdAsync(map.Id)).ToList();
        Assert.That(savedMarkers.Count, Is.EqualTo(2));
        Assert.That(savedMarkers[0].NormalizedX, Is.EqualTo(1.0)); // Clamped from 1.45
        Assert.That(savedMarkers[0].NormalizedY, Is.EqualTo(0.0)); // Clamped from -0.2
        Assert.That(savedMarkers[1].NormalizedX, Is.EqualTo(0.5));
    }

    [Test]
    public async Task PublishMapAsync_PendingReviewWithMarkers_PublishesAndSupersedesPriorMap()
    {
        // 1. Existing published map v1
        var v1Map = new BranchFloorMap
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgId,
            BranchId = _branchId,
            Name = "Ground Floor Map v1",
            FloorLevel = "Floor 1",
            Version = 1,
            Status = FloorMapStatus.Published,
            FileStorageKey = "stored_v1.png"
        };
        await _floorMapRepo.CreateMapAsync(v1Map);

        // 2. New map v2 in PendingReview
        var v2Map = new BranchFloorMap
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgId,
            BranchId = _branchId,
            Name = "Ground Floor Map v2",
            FloorLevel = "Floor 1",
            Version = 2,
            Status = FloorMapStatus.PendingReview,
            FileStorageKey = "stored_v2.png",
            Markers = new List<FloorMapMarker>
            {
                new() { MarkerType = SafetyMarkerType.EmergencyExit, Label = "Exit", NormalizedX = 0.1, NormalizedY = 0.1 }
            }
        };
        await _floorMapRepo.CreateMapAsync(v2Map);

        var published = await _floorMapService.PublishMapAsync(v2Map.Id);

        Assert.That(published.Status, Is.EqualTo(FloorMapStatus.Published));
        Assert.That(published.PublishedBy, Is.Not.Null);
        Assert.That(published.SupersedesMapId, Is.EqualTo(v1Map.Id));

        var updatedV1 = await _floorMapRepo.GetMapByIdAsync(_tenantId, v1Map.Id);
        Assert.That(updatedV1!.Status, Is.EqualTo(FloorMapStatus.Superseded));
    }

    [Test]
    public async Task PublishMapAsync_WithoutMarkers_ThrowsInvalidOperationException()
    {
        var emptyMap = new BranchFloorMap
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgId,
            BranchId = _branchId,
            Name = "Empty Map",
            FloorLevel = "Floor 1",
            Status = FloorMapStatus.PendingReview,
            FileStorageKey = "file.png",
            Markers = new List<FloorMapMarker>() // Empty markers
        };
        await _floorMapRepo.CreateMapAsync(emptyMap);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _floorMapService.PublishMapAsync(emptyMap.Id);
        });
    }

    [Test]
    public async Task UpdateDraftMapAsync_WhenMapIsPublished_ThrowsInvalidOperationException()
    {
        var publishedMap = new BranchFloorMap
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgId,
            BranchId = _branchId,
            Name = "Immutable Published Map",
            FloorLevel = "Floor 1",
            Status = FloorMapStatus.Published
        };
        await _floorMapRepo.CreateMapAsync(publishedMap);

        var updateReq = new UpdateFloorMapRequest
        {
            Id = publishedMap.Id,
            Name = "Altered Name"
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _floorMapService.UpdateDraftMapAsync(updateReq);
        });
    }

    [Test]
    public async Task CreateNewDraftFromPublishedAsync_IncrementsVersionAndClonesMarkers()
    {
        var publishedMap = new BranchFloorMap
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgId,
            BranchId = _branchId,
            Name = "Original Floor 1",
            FloorLevel = "Floor 1",
            Version = 1,
            Status = FloorMapStatus.Published,
            FileStorageKey = "plan.png",
            Markers = new List<FloorMapMarker>
            {
                new() { MarkerType = SafetyMarkerType.AED, Label = "AED Lobby", NormalizedX = 0.25, NormalizedY = 0.3 }
            }
        };
        await _floorMapRepo.CreateMapAsync(publishedMap);

        var newDraft = await _floorMapService.CreateNewDraftFromPublishedAsync(publishedMap.Id);

        Assert.That(newDraft.Status, Is.EqualTo(FloorMapStatus.Draft));
        Assert.That(newDraft.Version, Is.EqualTo(2));
        Assert.That(newDraft.SupersedesMapId, Is.EqualTo(publishedMap.Id));
        Assert.That(newDraft.Markers.Count, Is.EqualTo(1));
        Assert.That(newDraft.Markers[0].Label, Is.EqualTo("AED Lobby"));
    }

    [Test]
    public void StorageService_InvalidFileHeader_RejectsUpload()
    {
        // Malformed or executable bytes
        var bogusBytes = Encoding.UTF8.GetBytes("Not A Real Image Or PDF File Header Content");
        using var stream = new MemoryStream(bogusBytes);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _storageService.StoreMapFileAsync(stream, "fake.png", "image/png");
        });
    }

    [Test]
    public async Task StorageService_ValidPdfHeader_AcceptsAndComputesSha256()
    {
        // %PDF header
        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7 sample content");
        using var stream = new MemoryStream(pdfBytes);

        var result = await _storageService.StoreMapFileAsync(stream, "plan.pdf", "application/pdf");

        Assert.That(result.ValidatedMimeType, Is.EqualTo("application/pdf"));
        Assert.That(result.ContentHash, Is.Not.Empty);
        Assert.That(result.StorageKey.EndsWith(".pdf"), Is.True);
    }

    [Test]
    public async Task EmergencyContact_Create_StartsAsUnverified()
    {
        var contact = new EmergencyContact
        {
            Name = "Local Fire Station #4",
            PhoneNumber = "(555) 019-9911",
            Category = EmergencyContactCategory.FireService,
            Scope = EmergencyContactScope.Branch,
            BranchId = _branchId
        };

        var created = await _contactService.CreateContactAsync(contact);

        Assert.That(created.VerificationStatus, Is.EqualTo(VerificationStatus.Unverified));
        Assert.That(created.VerifiedBy, Is.Null);
    }

    [Test]
    public async Task EmergencyContact_Verify_UpdatesVerificationStatusAndDate()
    {
        var contact = new EmergencyContact
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgId,
            BranchId = _branchId,
            Name = "Children's Pediatric ER",
            PhoneNumber = "(555) 019-5000",
            Category = EmergencyContactCategory.NearestHospital,
            VerificationStatus = VerificationStatus.Unverified
        };
        await _contactRepo.CreateContactAsync(contact);

        await _contactService.VerifyContactAsync(contact.Id, "auditor@enjoyeveryday.com", DateTime.UtcNow.AddMonths(6));

        var updated = await _contactRepo.GetContactByIdAsync(_tenantId, contact.Id);
        Assert.That(updated!.VerificationStatus, Is.EqualTo(VerificationStatus.Verified));
        Assert.That(updated.VerifiedBy, Is.EqualTo("auditor@enjoyeveryday.com"));
        Assert.That(updated.ReviewDueAt, Is.Not.Null);
    }

    [Test]
    public async Task EmergencyContact_UpdatePhoneNumber_ResetsVerificationToUnverified()
    {
        var contact = new EmergencyContact
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgId,
            BranchId = _branchId,
            Name = "Poison Helpline",
            PhoneNumber = "1-800-222-1222",
            Category = EmergencyContactCategory.PoisonControl,
            VerificationStatus = VerificationStatus.Verified,
            VerifiedBy = "admin@enjoyeveryday.com"
        };
        await _contactRepo.CreateContactAsync(contact);

        // Edit telephone number
        contact.PhoneNumber = "1-800-222-9999";
        await _contactService.UpdateContactAsync(contact);

        var updated = await _contactRepo.GetContactByIdAsync(_tenantId, contact.Id);
        Assert.That(updated!.VerificationStatus, Is.EqualTo(VerificationStatus.Unverified));
        Assert.That(updated.VerifiedBy, Is.Null);
    }

    [Test]
    public async Task GeneratePrintableCard_ParentRole_RedactsPrivateStaffNumbersAndAltPhones()
    {
        // Add public emergency dispatch
        await _contactRepo.CreateContactAsync(new EmergencyContact
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            BranchId = _branchId,
            Scope = EmergencyContactScope.Jurisdiction,
            Category = EmergencyContactCategory.EmergencyServices,
            Name = "Emergency Dispatch (911)",
            PhoneNumber = "911",
            VerificationStatus = VerificationStatus.Verified,
            IsActive = true
        });

        // Add internal on-call staff
        await _contactRepo.CreateContactAsync(new EmergencyContact
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            BranchId = _branchId,
            Scope = EmergencyContactScope.Branch,
            Category = EmergencyContactCategory.DesignatedOnCall,
            Name = "Internal On-Call Nurse",
            PhoneNumber = "(555) 777-8888",
            AlternatePhoneNumber = "(555) 777-9999",
            VerificationStatus = VerificationStatus.Verified,
            IsActive = true
        });

        var parentCard = await _contactService.GeneratePrintableCardAsync(_branchId, EmergencyCardRole.Parent);

        // On-call staff must NOT appear on parent card
        Assert.That(parentCard.Contacts.Any(c => c.Category == EmergencyContactCategory.DesignatedOnCall), Is.False);

        // 911 must appear, and alternate phone numbers are redacted
        var dispatch = parentCard.Contacts.FirstOrDefault(c => c.Category == EmergencyContactCategory.EmergencyServices);
        Assert.That(dispatch, Is.Not.Null);
        Assert.That(dispatch!.AlternatePhoneNumber, Is.Null);
    }

    [Test]
    public async Task GeneratePrintableCard_MissingCriticalNumbers_EmitsWarnings()
    {
        // Branch with no 911 or poison control configured
        var card = await _contactService.GeneratePrintableCardAsync(_branchId, EmergencyCardRole.Administrator);

        Assert.That(card.Warnings.Count, Is.GreaterThan(0));
        Assert.That(card.Warnings.Any(w => w.Contains("emergency services", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(card.Warnings.Any(w => w.Contains("Poison Control", StringComparison.OrdinalIgnoreCase)), Is.True);
    }
}

// In-Memory Test Repositories & Contexts

public class MockFloorMapRepository : IFloorMapRepository
{
    public List<BranchFloorMap> Maps { get; } = new();

    public Task<IEnumerable<BranchFloorMap>> GetMapsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, string? status = null, CancellationToken ct = default)
    {
        var result = Maps.Where(m => m.TenantId == tenantId &&
            (!organizationId.HasValue || m.OrganizationId == organizationId) &&
            (!branchId.HasValue || m.BranchId == branchId) &&
            (status == null || m.Status == status));
        return Task.FromResult<IEnumerable<BranchFloorMap>>(result.ToList());
    }

    public Task<BranchFloorMap?> GetMapByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var map = Maps.FirstOrDefault(m => m.TenantId == tenantId && m.Id == id);
        return Task.FromResult(map);
    }

    public Task<BranchFloorMap> CreateMapAsync(BranchFloorMap map, CancellationToken ct = default)
    {
        Maps.Add(map);
        return Task.FromResult(map);
    }

    public Task UpdateMapAsync(BranchFloorMap map, CancellationToken ct = default)
    {
        var idx = Maps.FindIndex(m => m.Id == map.Id);
        if (idx >= 0) Maps[idx] = map;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<FloorMapMarker>> GetMarkersByMapIdAsync(Guid mapId, CancellationToken ct = default)
    {
        var map = Maps.FirstOrDefault(m => m.Id == mapId);
        return Task.FromResult<IEnumerable<FloorMapMarker>>(map?.Markers ?? new List<FloorMapMarker>());
    }

    public Task SaveMarkersAsync(Guid mapId, List<FloorMapMarker> markers, CancellationToken ct = default)
    {
        var map = Maps.FirstOrDefault(m => m.Id == mapId);
        if (map != null)
        {
            map.Markers = markers;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<BranchFloorMap>> GetMapHistoryAsync(Guid tenantId, Guid branchId, string floorLevel, CancellationToken ct = default)
    {
        var result = Maps.Where(m => m.TenantId == tenantId && m.BranchId == branchId && m.FloorLevel == floorLevel);
        return Task.FromResult<IEnumerable<BranchFloorMap>>(result.ToList());
    }
}

public class MockEmergencyContactRepository : IEmergencyContactRepository
{
    public List<EmergencyContact> Contacts { get; } = new();

    public Task<IEnumerable<EmergencyContact>> GetContactsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, bool activeOnly = false, CancellationToken ct = default)
    {
        var result = Contacts.Where(c => c.TenantId == tenantId && (!activeOnly || c.IsActive));
        return Task.FromResult<IEnumerable<EmergencyContact>>(result.ToList());
    }

    public Task<EmergencyContact?> GetContactByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var found = Contacts.FirstOrDefault(c => c.TenantId == tenantId && c.Id == id);
        if (found == null) return Task.FromResult<EmergencyContact?>(null);
        return Task.FromResult<EmergencyContact?>(new EmergencyContact
        {
            Id = found.Id,
            TenantId = found.TenantId,
            OrganizationId = found.OrganizationId,
            BranchId = found.BranchId,
            Scope = found.Scope,
            Category = found.Category,
            Name = found.Name,
            PhoneNumber = found.PhoneNumber,
            AlternatePhoneNumber = found.AlternatePhoneNumber,
            CountryRegion = found.CountryRegion,
            Address = found.Address,
            Instructions = found.Instructions,
            VerificationStatus = found.VerificationStatus,
            VerifiedBy = found.VerifiedBy,
            VerifiedAt = found.VerifiedAt,
            ReviewDueAt = found.ReviewDueAt,
            IsActive = found.IsActive,
            DisplayOrder = found.DisplayOrder,
            CreatedAt = found.CreatedAt,
            UpdatedAt = found.UpdatedAt
        });
    }

    public Task<EmergencyContact> CreateContactAsync(EmergencyContact contact, CancellationToken ct = default)
    {
        Contacts.Add(new EmergencyContact
        {
            Id = contact.Id,
            TenantId = contact.TenantId,
            OrganizationId = contact.OrganizationId,
            BranchId = contact.BranchId,
            Scope = contact.Scope,
            Category = contact.Category,
            Name = contact.Name,
            PhoneNumber = contact.PhoneNumber,
            AlternatePhoneNumber = contact.AlternatePhoneNumber,
            CountryRegion = contact.CountryRegion,
            Address = contact.Address,
            Instructions = contact.Instructions,
            VerificationStatus = contact.VerificationStatus,
            VerifiedBy = contact.VerifiedBy,
            VerifiedAt = contact.VerifiedAt,
            ReviewDueAt = contact.ReviewDueAt,
            IsActive = contact.IsActive,
            DisplayOrder = contact.DisplayOrder,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        });
        return Task.FromResult(contact);
    }

    public Task UpdateContactAsync(EmergencyContact contact, CancellationToken ct = default)
    {
        var idx = Contacts.FindIndex(c => c.Id == contact.Id);
        if (idx >= 0) Contacts[idx] = contact;
        return Task.CompletedTask;
    }

    public Task DeleteContactAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        Contacts.RemoveAll(c => c.TenantId == tenantId && c.Id == id);
        return Task.CompletedTask;
    }

    public Task VerifyContactAsync(Guid tenantId, Guid id, string verifiedBy, DateTime verifiedAt, DateTime? reviewDueAt, CancellationToken ct = default)
    {
        var contact = Contacts.FirstOrDefault(c => c.TenantId == tenantId && c.Id == id);
        if (contact != null)
        {
            contact.VerificationStatus = VerificationStatus.Verified;
            contact.VerifiedBy = verifiedBy;
            contact.VerifiedAt = verifiedAt;
            contact.ReviewDueAt = reviewDueAt;
        }
        return Task.CompletedTask;
    }
}

public class MockBranchRepository : IBranchRepository
{
    public List<Branch> Branches { get; } = new();
    public Task<Branch?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default) =>
        Task.FromResult(Branches.FirstOrDefault(b => b.TenantId == tenantId && b.Id == id));
    public Task<IEnumerable<Branch>> GetByOrganizationIdAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<Branch>>(Branches.Where(b => b.TenantId == tenantId && b.OrganizationId == organizationId).ToList());
    public Task<Branch> AddAsync(Branch branch, CancellationToken ct = default) { Branches.Add(branch); return Task.FromResult(branch); }
    public Task UpdateAsync(Branch branch, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default) => Task.CompletedTask;
}

public class MockOrganizationRepository : IOrganizationRepository
{
    public List<Organization> Organizations { get; } = new();
    public Task<IEnumerable<Organization>> GetAllAsync(Guid tenantId, CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<Organization>>(Organizations.Where(o => o.TenantId == tenantId).ToList());
    public Task<Organization?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default) =>
        Task.FromResult(Organizations.FirstOrDefault(o => o.TenantId == tenantId && o.Id == id));
    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Organizations.FirstOrDefault(o => o.Id == id));
    public Task<Organization> AddAsync(Organization org, CancellationToken ct = default) { Organizations.Add(org); return Task.FromResult(org); }
    public Task UpdateAsync(Organization org, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default) => Task.CompletedTask;
}

public class MockAuditService : IAuditService
{
    public List<AuditEntry> Entries { get; } = new();
    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

public class TestTenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public string TenantName => "Test Tenant";
    public bool IsResolved => true;
}

public class TestUserContext : IUserContext
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsAuthenticated => true;
    public IEnumerable<string> Roles => new[] { "Admin" };
    public IEnumerable<string> Permissions => new[] { "all" };
    public Guid? OrganizationId { get; set; }
    public bool HasPermission(string permission) => true;
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class CreateFloorMapRequest
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BuildingArea { get; set; }
    public string FloorLevel { get; set; } = "Floor 1";
    public string? Description { get; set; }
    public string? EmergencyInstructions { get; set; }
    public DateTime? ReviewDueAt { get; set; }
    public Stream? FileStream { get; set; }
    public string? FileName { get; set; }
    public string? FileContentType { get; set; }
}

public class UpdateFloorMapRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BuildingArea { get; set; }
    public string FloorLevel { get; set; } = "Floor 1";
    public string? Description { get; set; }
    public string? EmergencyInstructions { get; set; }
    public DateTime? ReviewDueAt { get; set; }
    public Stream? NewFileStream { get; set; }
    public string? NewFileName { get; set; }
    public string? NewFileContentType { get; set; }
    public List<FloorMapMarker>? Markers { get; set; }
}

public interface IFloorMapService
{
    Task<IEnumerable<BranchFloorMap>> GetMapsAsync(Guid? organizationId = null, Guid? branchId = null, string? status = null, CancellationToken ct = default);
    Task<BranchFloorMap?> GetMapByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<BranchFloorMap>> GetMapHistoryAsync(Guid branchId, string floorLevel, CancellationToken ct = default);
    Task<BranchFloorMap> CreateDraftMapAsync(CreateFloorMapRequest request, CancellationToken ct = default);
    Task<BranchFloorMap> UpdateDraftMapAsync(UpdateFloorMapRequest request, CancellationToken ct = default);
    Task SaveMarkersAsync(Guid mapId, List<FloorMapMarker> markers, CancellationToken ct = default);
    Task<BranchFloorMap> SubmitForReviewAsync(Guid mapId, CancellationToken ct = default);
    Task<BranchFloorMap> PublishMapAsync(Guid mapId, CancellationToken ct = default);
    Task<BranchFloorMap> CreateNewDraftFromPublishedAsync(Guid publishedMapId, CancellationToken ct = default);
    Task<BranchFloorMap> RetireMapAsync(Guid mapId, CancellationToken ct = default);
    Task<(Stream? Stream, string ContentType)?> GetMapFileStreamAsync(Guid mapId, CancellationToken ct = default);
}

public class FloorMapService : IFloorMapService
{
    private readonly IFloorMapRepository _repository;
    private readonly IFloorMapStorageService _storageService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly IResourceAuthorizer _authorizer;

    public FloorMapService(
        IFloorMapRepository repository,
        IFloorMapStorageService storageService,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService)
        : this(repository, storageService, tenantContext, userContext, auditService, new NullResourceAuthorizer())
    {
    }

    public FloorMapService(
        IFloorMapRepository repository,
        IFloorMapStorageService storageService,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        IResourceAuthorizer authorizer)
    {
        _repository = repository;
        _storageService = storageService;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _authorizer = authorizer;
    }

    /// <summary>Published maps are an emergency resource (view); every other state is a management artifact.</summary>
    private async Task<bool> CanReadAsync(BranchFloorMap map, CancellationToken ct)
    {
        if (map.TenantId != _tenantContext.TenantId) return false;
        var permission = map.Status == FloorMapStatus.Published ? Permissions.SafetyMapView : Permissions.SafetyMapManage;
        return await _authorizer.CanAccessBranchAsync(map.BranchId, permission, ct);
    }

    private async Task<BranchFloorMap> LoadForAsync(Guid id, string permission, CancellationToken ct)
    {
        var map = await _repository.GetMapByIdAsync(_tenantContext.TenantId, id, ct);
        // Missing and forbidden look identical to the caller.
        if (map == null) throw new UnauthorizedAccessException("Access denied.");
        await _authorizer.RequireBranchAsync(map.BranchId, permission, ct);
        return map;
    }

    public async Task<IEnumerable<BranchFloorMap>> GetMapsAsync(
        Guid? organizationId = null,
        Guid? branchId = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var effectiveOrgId = _userContext.OrganizationId ?? organizationId;
        if (effectiveOrgId == null) throw new UnauthorizedAccessException("Access denied.");
        if (organizationId.HasValue && organizationId != effectiveOrgId)
            throw new UnauthorizedAccessException("Access denied.");
        var maps = await _repository.GetMapsAsync(_tenantContext.TenantId, effectiveOrgId, branchId, status, ct);
        var visible = new List<BranchFloorMap>();
        foreach (var m in maps)
            if (await CanReadAsync(m, ct)) visible.Add(m);
        return visible;
    }

    public async Task<BranchFloorMap?> GetMapByIdAsync(Guid id, CancellationToken ct = default)
    {
        var map = await _repository.GetMapByIdAsync(_tenantContext.TenantId, id, ct);
        if (map == null) return null;
        if (!await CanReadAsync(map, ct)) throw new UnauthorizedAccessException("Access denied.");
        return map;
    }

    public async Task<IEnumerable<BranchFloorMap>> GetMapHistoryAsync(
        Guid branchId,
        string floorLevel,
        CancellationToken ct = default)
    {
        await _authorizer.RequireBranchAsync(branchId, Permissions.SafetyMapManage, ct);
        return await _repository.GetMapHistoryAsync(_tenantContext.TenantId, branchId, floorLevel, ct);
    }

    public async Task<BranchFloorMap> CreateDraftMapAsync(CreateFloorMapRequest request, CancellationToken ct = default)
    {
        if (request.BranchId == Guid.Empty)
            throw new ArgumentException("Branch selection is required.");

        // Scope first: a caller without manage rights on THIS branch learns nothing about validation state.
        await _authorizer.RequireBranchAsync(request.BranchId, Permissions.SafetyMapManage, ct);
        if (request.OrganizationId != Guid.Empty && request.OrganizationId != _userContext.OrganizationId)
            throw new UnauthorizedAccessException("Access denied.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Map name is required.");

        if (string.IsNullOrWhiteSpace(request.FloorLevel))
            throw new ArgumentException("Floor level is required.");

        if (request.FileStream == null || string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("Floor plan file (PDF, PNG, or JPG) is required.");

        var storedFile = await _storageService.StoreMapFileAsync(
            request.FileStream,
            request.FileName,
            request.FileContentType ?? "application/octet-stream",
            ct);

        var map = new BranchFloorMap
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = request.OrganizationId != Guid.Empty ? request.OrganizationId : (_userContext.OrganizationId ?? Guid.Empty),
            BranchId = request.BranchId,
            Name = request.Name.Trim(),
            BuildingArea = request.BuildingArea?.Trim(),
            FloorLevel = request.FloorLevel.Trim(),
            Description = request.Description?.Trim(),
            EmergencyInstructions = request.EmergencyInstructions?.Trim(),
            FileStorageKey = storedFile.StorageKey,
            FileType = storedFile.ValidatedMimeType,
            FileSize = storedFile.FileSizeBytes,
            ContentHash = storedFile.ContentHash,
            Version = 1,
            Status = FloorMapStatus.Draft,
            ReviewDueAt = request.ReviewDueAt ?? DateTime.UtcNow.AddMonths(12),
            CreatedBy = _userContext.Email.Length > 0 ? _userContext.Email : "Safety Admin",
            CreatedAt = DateTime.UtcNow,
            RowVersion = 1
        };

        var created = await _repository.CreateMapAsync(map, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "FloorMap.Created",
            EntityType = nameof(BranchFloorMap),
            EntityId = created.Id,
            Details = new { created.BranchId, created.FloorLevel, created.Name, created.Version }
        }, ct);

        return created;
    }

    public async Task<BranchFloorMap> UpdateDraftMapAsync(UpdateFloorMapRequest request, CancellationToken ct = default)
    {
        var existing = await LoadForAsync(request.Id, Permissions.SafetyMapManage, ct);

        if (existing.Status == FloorMapStatus.Published)
            throw new InvalidOperationException("Published floor maps are immutable. Create a new draft revision to make updates.");

        if (existing.Status == FloorMapStatus.Retired)
            throw new InvalidOperationException("Retired floor maps cannot be edited.");

        existing.Name = request.Name.Trim();
        existing.BuildingArea = request.BuildingArea?.Trim();
        existing.FloorLevel = request.FloorLevel.Trim();
        existing.Description = request.Description?.Trim();
        existing.EmergencyInstructions = request.EmergencyInstructions?.Trim();
        if (request.ReviewDueAt.HasValue) existing.ReviewDueAt = request.ReviewDueAt;

        if (request.NewFileStream != null && !string.IsNullOrWhiteSpace(request.NewFileName))
        {
            var oldStorageKey = existing.FileStorageKey;
            var storedFile = await _storageService.StoreMapFileAsync(
                request.NewFileStream,
                request.NewFileName,
                request.NewFileContentType ?? "application/octet-stream",
                ct);

            existing.FileStorageKey = storedFile.StorageKey;
            existing.FileType = storedFile.ValidatedMimeType;
            existing.FileSize = storedFile.FileSizeBytes;
            existing.ContentHash = storedFile.ContentHash;

            if (!string.IsNullOrEmpty(oldStorageKey))
            {
                await _storageService.DeleteMapFileAsync(oldStorageKey, ct);
            }
        }

        await _repository.UpdateMapAsync(existing, ct);

        if (request.Markers != null)
        {
            await SaveMarkersAsync(existing.Id, request.Markers, ct);
            existing.Markers = request.Markers;
        }

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "FloorMap.Updated",
            EntityType = nameof(BranchFloorMap),
            EntityId = existing.Id,
            Details = new { existing.Name, existing.FloorLevel }
        }, ct);

        return existing;
    }

    public async Task SaveMarkersAsync(Guid mapId, List<FloorMapMarker> markers, CancellationToken ct = default)
    {
        var existing = await LoadForAsync(mapId, Permissions.SafetyMapManage, ct);

        // Normalize & validate coordinates
        var order = 1;
        foreach (var m in markers)
        {
            m.FloorMapId = mapId;
            m.NormalizedX = Math.Clamp(m.NormalizedX, 0.0, 1.0);
            m.NormalizedY = Math.Clamp(m.NormalizedY, 0.0, 1.0);
            m.DisplayOrder = order++;
            if (string.IsNullOrWhiteSpace(m.Label))
                m.Label = m.MarkerType;
        }

        await _repository.SaveMarkersAsync(mapId, markers, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "FloorMap.MarkersUpdated",
            EntityType = nameof(BranchFloorMap),
            EntityId = mapId,
            Details = new { MarkerCount = markers.Count }
        }, ct);
    }

    public async Task<BranchFloorMap> SubmitForReviewAsync(Guid mapId, CancellationToken ct = default)
    {
        var existing = await LoadForAsync(mapId, Permissions.SafetyMapManage, ct);

        if (existing.Status != FloorMapStatus.Draft)
            throw new InvalidOperationException($"Only Draft floor maps can be submitted for review. Current status: {existing.Status}");

        existing.Status = FloorMapStatus.PendingReview;
        await _repository.UpdateMapAsync(existing, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "FloorMap.SubmittedForReview",
            EntityType = nameof(BranchFloorMap),
            EntityId = existing.Id,
            Details = new { existing.Name, existing.Version }
        }, ct);

        return existing;
    }

    public async Task<BranchFloorMap> PublishMapAsync(Guid mapId, CancellationToken ct = default)
    {
        var existing = await LoadForAsync(mapId, Permissions.SafetyMapPublish, ct);

        if (existing.Status != FloorMapStatus.PendingReview && existing.Status != FloorMapStatus.Draft)
            throw new InvalidOperationException($"Map cannot be published from status: {existing.Status}");

        // Publication validation:
        if (string.IsNullOrWhiteSpace(existing.FileStorageKey))
            throw new InvalidOperationException("Cannot publish a floor map without an uploaded floor plan document.");

        if (existing.Markers.Count == 0)
            throw new InvalidOperationException("Cannot publish a floor map without safety markers. Please configure emergency exits, routes, or equipment.");

        // Check if there is an existing published map for the same branch and floor
        var activeMaps = await _repository.GetMapsAsync(_tenantContext.TenantId, existing.OrganizationId, existing.BranchId, FloorMapStatus.Published, ct);
        var currentlyPublished = activeMaps.FirstOrDefault(m => m.FloorLevel.Equals(existing.FloorLevel, StringComparison.OrdinalIgnoreCase) && m.Id != existing.Id);

        if (currentlyPublished != null)
        {
            currentlyPublished.Status = FloorMapStatus.Superseded;
            await _repository.UpdateMapAsync(currentlyPublished, ct);

            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = _tenantContext.TenantId,
                UserId = _userContext.UserId,
                Action = "FloorMap.Superseded",
                EntityType = nameof(BranchFloorMap),
                EntityId = currentlyPublished.Id,
                Details = new { SupersededByMapId = existing.Id, currentlyPublished.Version }
            }, ct);

            existing.SupersedesMapId = currentlyPublished.Id;
        }

        existing.Status = FloorMapStatus.Published;
        existing.PublishedBy = _userContext.Email.Length > 0 ? _userContext.Email : "Safety Administrator";
        existing.PublishedAt = DateTime.UtcNow;

        await _repository.UpdateMapAsync(existing, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "FloorMap.Published",
            EntityType = nameof(BranchFloorMap),
            EntityId = existing.Id,
            Details = new { existing.Name, existing.Version, existing.PublishedBy, existing.PublishedAt }
        }, ct);

        return existing;
    }

    public async Task<BranchFloorMap> CreateNewDraftFromPublishedAsync(Guid publishedMapId, CancellationToken ct = default)
    {
        var published = await LoadForAsync(publishedMapId, Permissions.SafetyMapManage, ct);

        // Clone to new draft
        var newDraft = new BranchFloorMap
        {
            Id = Guid.NewGuid(),
            TenantId = published.TenantId,
            OrganizationId = published.OrganizationId,
            BranchId = published.BranchId,
            Name = published.Name,
            BuildingArea = published.BuildingArea,
            FloorLevel = published.FloorLevel,
            Description = published.Description,
            EmergencyInstructions = published.EmergencyInstructions,
            FileStorageKey = published.FileStorageKey,
            FileType = published.FileType,
            FileSize = published.FileSize,
            ContentHash = published.ContentHash,
            Version = published.Version + 1,
            Status = FloorMapStatus.Draft,
            ReviewDueAt = DateTime.UtcNow.AddMonths(12),
            CreatedBy = _userContext.Email.Length > 0 ? _userContext.Email : "Safety Admin",
            CreatedAt = DateTime.UtcNow,
            SupersedesMapId = published.Id,
            RowVersion = 1,
            Markers = published.Markers.Select(m => new FloorMapMarker
            {
                Id = Guid.NewGuid(),
                MarkerType = m.MarkerType,
                Label = m.Label,
                Instructions = m.Instructions,
                NormalizedX = m.NormalizedX,
                NormalizedY = m.NormalizedY,
                DisplayOrder = m.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        };

        var created = await _repository.CreateMapAsync(newDraft, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "FloorMap.NewDraftVersionCreated",
            EntityType = nameof(BranchFloorMap),
            EntityId = created.Id,
            Details = new { PriorVersion = published.Version, NewVersion = created.Version }
        }, ct);

        return created;
    }

    public async Task<BranchFloorMap> RetireMapAsync(Guid mapId, CancellationToken ct = default)
    {
        var existing = await LoadForAsync(mapId, Permissions.SafetyMapManage, ct);

        existing.Status = FloorMapStatus.Retired;
        await _repository.UpdateMapAsync(existing, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "FloorMap.Retired",
            EntityType = nameof(BranchFloorMap),
            EntityId = existing.Id,
            Details = new { existing.Name, existing.Version }
        }, ct);

        return existing;
    }

    public async Task<(Stream? Stream, string ContentType)?> GetMapFileStreamAsync(Guid mapId, CancellationToken ct = default)
    {
        var existing = await _repository.GetMapByIdAsync(_tenantContext.TenantId, mapId, ct);
        if (existing == null) return null;
        if (!await CanReadAsync(existing, ct)) throw new UnauthorizedAccessException("Access denied.");

        var fileResult = await _storageService.RetrieveMapFileAsync(existing.FileStorageKey, ct);
        if (fileResult == null) return null;

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "FloorMap.FileAccessed",
            EntityType = nameof(BranchFloorMap),
            EntityId = existing.Id,
            Details = new { existing.FileStorageKey, existing.FileType }
        }, ct);

        return fileResult;
    }
}

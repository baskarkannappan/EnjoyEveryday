using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public interface IDaycareAnnouncementService
{
    Task<IEnumerable<DaycareAnnouncement>> GetAnnouncementsAsync(Guid? organizationId = null, Guid? branchId = null, bool activeOnly = true, int limit = 20, CancellationToken ct = default);
    Task<IEnumerable<DaycareAnnouncement>> GetAnnouncementsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, bool activeOnly = true, int limit = 20, CancellationToken ct = default);
    Task<DaycareAnnouncement> CreateAnnouncementAsync(DaycareAnnouncement announcement, CancellationToken ct = default);
    Task<DaycareAnnouncement> CreateAnnouncementAsync(string title, string content, string tag, bool isUrgent, Guid organizationId, Guid? branchId, string authorName, string authorRole, CancellationToken ct = default);
    Task<bool> DeactivateAnnouncementAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeactivateAnnouncementAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<bool> ReactivateAnnouncementAsync(Guid id, CancellationToken ct = default);
    Task<bool> ReactivateAnnouncementAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<bool> DeleteAnnouncementAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteAnnouncementAsync(Guid tenantId, Guid id, CancellationToken ct = default);
}

public class DaycareAnnouncementService : IDaycareAnnouncementService
{
    private readonly IDaycareAnnouncementRepository _announcementRepository;
    private readonly ITenantContext _tenantContext;

    public DaycareAnnouncementService(IDaycareAnnouncementRepository announcementRepository, ITenantContext tenantContext)
    {
        _announcementRepository = announcementRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<DaycareAnnouncement>> GetAnnouncementsAsync(Guid? organizationId = null, Guid? branchId = null, bool activeOnly = true, int limit = 20, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : Guid.Parse("10000000-0000-0000-0000-000000000001");
        return await _announcementRepository.GetAnnouncementsAsync(tenantId, organizationId, branchId, activeOnly, limit, ct);
    }

    public async Task<IEnumerable<DaycareAnnouncement>> GetAnnouncementsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, bool activeOnly = true, int limit = 20, CancellationToken ct = default)
    {
        var effectiveTenantId = tenantId != Guid.Empty ? tenantId : _tenantContext.TenantId;
        if (effectiveTenantId == Guid.Empty) effectiveTenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        return await _announcementRepository.GetAnnouncementsAsync(effectiveTenantId, organizationId, branchId, activeOnly, limit, ct);
    }

    public async Task<DaycareAnnouncement> CreateAnnouncementAsync(DaycareAnnouncement announcement, CancellationToken ct = default)
    {
        if (announcement.Id == Guid.Empty) announcement.Id = Guid.NewGuid();
        if (announcement.TenantId == Guid.Empty)
        {
            announcement.TenantId = _tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : Guid.Parse("10000000-0000-0000-0000-000000000001");
        }
        if (announcement.CreatedAt == default) announcement.CreatedAt = DateTime.UtcNow;
        if (announcement.UpdatedAt == default) announcement.UpdatedAt = DateTime.UtcNow;

        return await _announcementRepository.CreateAsync(announcement, ct);
    }

    public async Task<DaycareAnnouncement> CreateAnnouncementAsync(string title, string content, string tag, bool isUrgent, Guid organizationId, Guid? branchId, string authorName, string authorRole, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : Guid.Parse("10000000-0000-0000-0000-000000000001");
        var announcement = new DaycareAnnouncement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = organizationId,
            BranchId = branchId,
            Title = title?.Trim() ?? "Announcement",
            Content = content?.Trim() ?? string.Empty,
            Tag = string.IsNullOrWhiteSpace(tag) ? "Notice" : tag.Trim(),
            IsUrgent = isUrgent,
            AuthorName = authorName,
            AuthorRole = authorRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _announcementRepository.CreateAsync(announcement, ct);
    }

    public async Task<bool> DeactivateAnnouncementAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : Guid.Parse("10000000-0000-0000-0000-000000000001");
        return await _announcementRepository.DeactivateAsync(id, tenantId, ct);
    }

    public async Task<bool> DeactivateAnnouncementAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var effectiveTenantId = tenantId != Guid.Empty ? tenantId : _tenantContext.TenantId;
        return await _announcementRepository.DeactivateAsync(id, effectiveTenantId, ct);
    }

    public async Task<bool> ReactivateAnnouncementAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : Guid.Parse("10000000-0000-0000-0000-000000000001");
        return await _announcementRepository.ReactivateAsync(id, tenantId, ct);
    }

    public async Task<bool> ReactivateAnnouncementAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var effectiveTenantId = tenantId != Guid.Empty ? tenantId : _tenantContext.TenantId;
        return await _announcementRepository.ReactivateAsync(id, effectiveTenantId, ct);
    }

    public async Task<bool> DeleteAnnouncementAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : Guid.Parse("10000000-0000-0000-0000-000000000001");
        return await _announcementRepository.DeleteAsync(id, tenantId, ct);
    }

    public async Task<bool> DeleteAnnouncementAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var effectiveTenantId = tenantId != Guid.Empty ? tenantId : _tenantContext.TenantId;
        return await _announcementRepository.DeleteAsync(id, effectiveTenantId, ct);
    }
}

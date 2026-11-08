using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IDaycareAnnouncementRepository
{
    Task<IEnumerable<DaycareAnnouncement>> GetAnnouncementsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, bool activeOnly = true, int limit = 20, CancellationToken ct = default);
    Task<DaycareAnnouncement> CreateAsync(DaycareAnnouncement announcement, CancellationToken ct = default);
    Task<bool> DeactivateAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);
    Task<bool> ReactivateAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
}


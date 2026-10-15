using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IEmergencyContactRepository
{
    Task<IEnumerable<EmergencyContact>> GetContactsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, bool activeOnly = false, CancellationToken ct = default);
    Task<EmergencyContact?> GetContactByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<EmergencyContact> CreateContactAsync(EmergencyContact contact, CancellationToken ct = default);
    Task UpdateContactAsync(EmergencyContact contact, CancellationToken ct = default);
    Task DeleteContactAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task VerifyContactAsync(Guid tenantId, Guid id, string verifiedBy, DateTime verifiedAt, DateTime? reviewDueAt, CancellationToken ct = default);
}

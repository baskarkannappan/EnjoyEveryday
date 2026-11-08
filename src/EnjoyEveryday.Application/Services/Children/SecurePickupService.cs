using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class AuthorizedPickupDto
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public Guid? PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public string? PickupCode { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPrimaryGuardian { get; set; }
    public string? Notes { get; set; }
    public bool IsRegisteredPickup { get; set; }
}

public class CustodyRestrictionDto
{
    public Guid Id { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string RestrictionType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}

public class ChildPickupSecurityProfile
{
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool HasCustodyRestrictions => CustodyRestrictions.Any();
    public List<CustodyRestrictionDto> CustodyRestrictions { get; set; } = new();
    public List<AuthorizedPickupDto> AuthorizedPickups { get; set; } = new();
    public ChildAttendance? TodayAttendance { get; set; }
}

public class RecordChildDepartureRequest
{
    public Guid ChildId { get; set; }
    public Guid? AttendanceId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ClassroomId { get; set; }
    public string PickupPersonName { get; set; } = string.Empty;
    public string PickupPersonRelationship { get; set; } = string.Empty;
    public string PickupVerificationMethod { get; set; } = "PhotoIdVerified"; // PhotoIdVerified, KnownGuardian, DigitalCodeVerified, EmergencyAuthorized
    public Guid? AuthorizedPickupId { get; set; }
    public TimeSpan? DepartureTime { get; set; }
    public string? StaffNotes { get; set; }
}

public class RecordChildDepartureResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset DepartureTimestamp { get; set; }
    public Guid? AttendanceId { get; set; }
}

public interface ISecurePickupService
{
    Task<ChildPickupSecurityProfile?> GetChildPickupSecurityProfileAsync(Guid childId, DateTime? date = null, CancellationToken ct = default);
    Task<ChildPickupSecurityProfile?> GetChildPickupSecurityProfileAsync(Guid tenantId, Guid childId, DateTime? date = null, CancellationToken ct = default);
    Task<RecordChildDepartureResponse> VerifyAndRecordChildDepartureAsync(RecordChildDepartureRequest request, CancellationToken ct = default);
    Task<IEnumerable<AuthorizedPickupDto>> GetAuthorizedPickupsForChildAsync(Guid childId, CancellationToken ct = default);
    Task<IEnumerable<AuthorizedPickupDto>> GetAuthorizedPickupsForChildAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task AddAuthorizedPickupAsync(AuthorizedPickup pickup, CancellationToken ct = default);
    Task<AuthorizedPickupDto> SaveAuthorizedPickupAsync(Guid tenantId, Guid childId, AuthorizedPickupDto dto, CancellationToken ct = default);
    Task<bool> DeleteAuthorizedPickupAsync(Guid tenantId, Guid childId, Guid pickupId, CancellationToken ct = default);
}

public class SecurePickupService : ISecurePickupService
{
    private readonly ISecurePickupRepository _pickupRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext? _userContext;
    private readonly IAuditService _auditService;
    private readonly IEndOfDayService? _endOfDayService;

    public SecurePickupService(
        ISecurePickupRepository pickupRepository,
        IAttendanceRepository attendanceRepository,
        ITenantContext tenantContext,
        IUserContext? userContext = null,
        IAuditService? auditService = null,
        IEndOfDayService? endOfDayService = null)
    {
        _pickupRepository = pickupRepository;
        _attendanceRepository = attendanceRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService ?? new NullAuditService();
        _endOfDayService = endOfDayService;
    }

    public async Task<ChildPickupSecurityProfile?> GetChildPickupSecurityProfileAsync(Guid childId, DateTime? date = null, CancellationToken ct = default)
    {
        return await GetChildPickupSecurityProfileAsync(_tenantContext.TenantId, childId, date, ct);
    }

    public async Task<ChildPickupSecurityProfile?> GetChildPickupSecurityProfileAsync(Guid tenantId, Guid childId, DateTime? date = null, CancellationToken ct = default)
    {
        DateTime targetDate = date ?? DateTime.Today;
        var effectiveTenantId = tenantId != Guid.Empty ? tenantId : _tenantContext.TenantId;

        // 1. Fetch child profile
        var child = await _pickupRepository.GetChildPickupHeaderAsync(effectiveTenantId, childId, ct);
        if (child == null) return null;

        // 2. Fetch custody restrictions
        var rawRestrictions = await _pickupRepository.GetActiveCustodyRestrictionsAsync(effectiveTenantId, childId, ct);
        var restrictions = rawRestrictions.Select(r => new CustodyRestrictionDto
        {
            Id = r.Id,
            PersonName = r.PersonName,
            RestrictionType = r.RestrictionType,
            Notes = r.Notes,
            EffectiveFrom = r.EffectiveFrom,
            EffectiveTo = r.EffectiveTo
        }).ToList();

        // 3. Fetch explicit authorized pickups
        var rawPickups = await _pickupRepository.GetAuthorizedPickupsAsync(effectiveTenantId, childId, ct);
        var pickups = rawPickups.Select(p => new AuthorizedPickupDto
        {
            Id = p.Id,
            ChildId = p.ChildId,
            PersonId = p.PersonId,
            FullName = p.FullName,
            Relationship = p.Relationship ?? "Authorized Pickup",
            Phone = p.Phone,
            PhotoUrl = p.PhotoUrl,
            PickupCode = p.PickupCode,
            IsActive = p.IsActive,
            IsPrimaryGuardian = (p.Relationship ?? "").Contains("Parent", StringComparison.OrdinalIgnoreCase) 
                             || (p.Relationship ?? "").Contains("Mother", StringComparison.OrdinalIgnoreCase)
                             || (p.Relationship ?? "").Contains("Father", StringComparison.OrdinalIgnoreCase),
            Notes = p.Notes,
            IsRegisteredPickup = true
        }).ToList();

        // 4. Fetch family relationships (parents & legal guardians)
        var rawFamily = await _pickupRepository.GetFamilyPickupRelationshipsAsync(effectiveTenantId, childId, ct);
        foreach (var f in rawFamily)
        {
            Guid personId = f.PersonId;
            if (!pickups.Any(p => p.PersonId == personId))
            {
                pickups.Insert(0, new AuthorizedPickupDto
                {
                    Id = Guid.NewGuid(),
                    ChildId = childId,
                    PersonId = personId,
                    FullName = f.FullName,
                    Relationship = f.Relationship ?? "Legal Guardian",
                    Phone = f.Phone,
                    PhotoUrl = f.PhotoUrl,
                    IsActive = true,
                    IsPrimaryGuardian = f.IsPrimary,
                    Notes = "Verified legal guardian relationship.",
                    IsRegisteredPickup = false
                });
            }
        }

        // If still no pickups, ensure child has parent placeholder
        if (!pickups.Any())
        {
            pickups.Add(new AuthorizedPickupDto
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                FullName = "Authorized Parent / Guardian",
                Relationship = "Legal Guardian",
                IsActive = true,
                IsPrimaryGuardian = true,
                Notes = "Primary guardian visual verification required.",
                IsRegisteredPickup = false
            });
        }

        // 5. Fetch today's attendance record
        var todayAtt = await _pickupRepository.GetTodayAttendanceAsync(effectiveTenantId, childId, targetDate, ct);

        return new ChildPickupSecurityProfile
        {
            ChildId = childId,
            ChildName = $"{child.FirstName} {child.LastName}".Trim(),
            ClassroomName = child.ClassroomName ?? "Classroom",
            BranchName = child.BranchName ?? "Main Branch",
            PhotoUrl = child.PhotoUrl,
            CustodyRestrictions = restrictions,
            AuthorizedPickups = pickups,
            TodayAttendance = todayAtt
        };
    }

    public async Task<RecordChildDepartureResponse> VerifyAndRecordChildDepartureAsync(RecordChildDepartureRequest request, CancellationToken ct = default)
    {
        // 0. Check if daycare is closed
        var now = DateTime.Now;
        if (_endOfDayService != null && _endOfDayService.IsDaycareClosedForDate(now.Date, request.BranchId, true, out var eodClosedReason))
        {
            return new RecordChildDepartureResponse
            {
                Success = false,
                ErrorMessage = $"Daycare is closed: {eodClosedReason}"
            };
        }

        // 1. Check for custody restriction block
        var activeRestrictions = await _pickupRepository.GetActiveCustodyRestrictionsAsync(_tenantContext.TenantId, request.ChildId, ct);
        
        foreach (var r in activeRestrictions)
        {
            string rName = r.PersonName;
            if (!string.IsNullOrWhiteSpace(rName) && request.PickupPersonName.Contains(rName, StringComparison.OrdinalIgnoreCase))
            {
                return new RecordChildDepartureResponse
                {
                    Success = false,
                    ErrorMessage = $"SECURITY RESTRICTION: Child cannot be released to {request.PickupPersonName}. A legal custody restriction is on file. Please contact center director immediately."
                };
            }
        }

        // 2. Query or create attendance record for today
        var existingAtt = await _pickupRepository.GetTodayAttendanceAsync(_tenantContext.TenantId, request.ChildId, DateTime.Today, ct);

        var departureTime = request.DepartureTime ?? DateTime.Now.TimeOfDay;
        Guid attId = existingAtt?.Id ?? request.AttendanceId ?? Guid.NewGuid();

        // 3. Verify that AuthorizedPickupId exists in database before saving to prevent foreign key violation
        Guid? validAuthorizedPickupId = null;
        if (request.AuthorizedPickupId.HasValue && request.AuthorizedPickupId.Value != Guid.Empty)
        {
            var exists = await _pickupRepository.AuthorizedPickupExistsAsync(_tenantContext.TenantId, request.AuthorizedPickupId.Value, ct);
            if (exists)
            {
                validAuthorizedPickupId = request.AuthorizedPickupId.Value;
            }
        }

        var attendanceRecord = existingAtt ?? new ChildAttendance
        {
            Id = attId,
            TenantId = _tenantContext.TenantId,
            BranchId = request.BranchId,
            ClassroomId = request.ClassroomId,
            ChildId = request.ChildId,
            AttendanceDate = DateTime.Today,
            ArrivalTime = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(30)),
            CreatedAt = DateTimeOffset.UtcNow
        };

        attendanceRecord.Status = "CheckedOut";
        attendanceRecord.DepartureTime = departureTime;
        attendanceRecord.PickupPersonName = request.PickupPersonName;
        attendanceRecord.PickupPersonRelationship = request.PickupPersonRelationship;
        attendanceRecord.PickupVerificationMethod = request.PickupVerificationMethod;
        attendanceRecord.PickupNotes = request.StaffNotes;
        attendanceRecord.AuthorizedPickupId = validAuthorizedPickupId;
        attendanceRecord.DepartureVerifiedByUserId = _userContext?.UserId;
        attendanceRecord.UpdatedAt = DateTimeOffset.UtcNow;

        await _attendanceRepository.RecordChildAttendanceAsync(attendanceRecord, ct);

        // 3. Audit trail
        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext?.UserId,
            Action = "ChildDepartureVerified",
            EntityType = "ChildAttendance",
            EntityId = attId,
            Details = new
            {
                request.ChildId,
                request.PickupPersonName,
                request.PickupPersonRelationship,
                request.PickupVerificationMethod,
                DepartureTime = departureTime.ToString(@"hh\:mm")
            }
        }, ct);

        return new RecordChildDepartureResponse
        {
            Success = true,
            DepartureTimestamp = DateTimeOffset.UtcNow,
            AttendanceId = attId
        };
    }

    public async Task<IEnumerable<AuthorizedPickupDto>> GetAuthorizedPickupsForChildAsync(Guid childId, CancellationToken ct = default)
    {
        return await GetAuthorizedPickupsForChildAsync(_tenantContext.TenantId, childId, ct);
    }

    public async Task<IEnumerable<AuthorizedPickupDto>> GetAuthorizedPickupsForChildAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        var profile = await GetChildPickupSecurityProfileAsync(tenantId, childId, null, ct);
        return profile?.AuthorizedPickups ?? Enumerable.Empty<AuthorizedPickupDto>();
    }

    public async Task AddAuthorizedPickupAsync(AuthorizedPickup pickup, CancellationToken ct = default)
    {
        pickup.TenantId = _tenantContext.TenantId;
        if (pickup.Id == Guid.Empty) pickup.Id = Guid.NewGuid();

        await _pickupRepository.AddAuthorizedPickupAsync(pickup, ct);
    }

    public async Task<AuthorizedPickupDto> SaveAuthorizedPickupAsync(Guid tenantId, Guid childId, AuthorizedPickupDto dto, CancellationToken ct = default)
    {
        var record = await _pickupRepository.SaveAuthorizedPickupWithPersonAsync(
            tenantId, childId, dto.Id, dto.PersonId, dto.FullName, dto.Relationship, dto.Phone, dto.PickupCode, dto.Notes, ct);

        return new AuthorizedPickupDto
        {
            Id = record.Id,
            ChildId = record.ChildId,
            PersonId = record.PersonId,
            FullName = record.FullName,
            Relationship = record.Relationship ?? "Authorized Pickup",
            Phone = record.Phone,
            PickupCode = record.PickupCode,
            Notes = record.Notes,
            IsActive = record.IsActive,
            IsRegisteredPickup = true
        };
    }

    public async Task<bool> DeleteAuthorizedPickupAsync(Guid tenantId, Guid childId, Guid pickupId, CancellationToken ct = default)
    {
        return await _pickupRepository.DeleteAuthorizedPickupAsync(tenantId, childId, pickupId, ct);
    }
}

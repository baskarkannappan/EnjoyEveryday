using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Data;
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
    Task<RecordChildDepartureResponse> VerifyAndRecordChildDepartureAsync(RecordChildDepartureRequest request, CancellationToken ct = default);
    Task<IEnumerable<AuthorizedPickupDto>> GetAuthorizedPickupsForChildAsync(Guid childId, CancellationToken ct = default);
    Task AddAuthorizedPickupAsync(AuthorizedPickup pickup, CancellationToken ct = default);
}

public class SecurePickupService : ISecurePickupService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext? _userContext;
    private readonly IAuditService _auditService;

    public SecurePickupService(
        IDbConnectionFactory connectionFactory,
        IAttendanceRepository attendanceRepository,
        ITenantContext tenantContext,
        IUserContext? userContext = null,
        IAuditService? auditService = null)
    {
        _connectionFactory = connectionFactory;
        _attendanceRepository = attendanceRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService ?? new NullAuditService();
    }

    public async Task<ChildPickupSecurityProfile?> GetChildPickupSecurityProfileAsync(Guid childId, DateTime? date = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        DateTime targetDate = date ?? DateTime.Today;

        // 1. Fetch child profile
        var childSql = @"
            SELECT c.id, c.first_name AS FirstName, c.last_name AS LastName, c.photo_url AS PhotoUrl,
                   cr.name AS ClassroomName, b.name AS BranchName, cr.id AS ClassroomId, b.id AS BranchId
            FROM children c
            LEFT JOIN classrooms cr ON c.classroom_id = cr.id
            LEFT JOIN branches b ON cr.branch_id = b.id
            WHERE c.tenant_id = @TenantId AND c.id = @ChildId";

        var child = await conn.QueryFirstOrDefaultAsync<dynamic>(childSql, new { TenantId = _tenantContext.TenantId, ChildId = childId });
        if (child == null) return null;

        // 2. Fetch custody restrictions
        var restrictionsSql = @"
            SELECT r.id, r.restriction_type AS RestrictionType, r.notes, r.effective_from AS EffectiveFrom, r.effective_to AS EffectiveTo,
                   COALESCE(p.first_name || ' ' || p.last_name, 'Restricted Individual') AS PersonName
            FROM child_person_restrictions r
            LEFT JOIN persons p ON r.person_id = p.id
            WHERE r.tenant_id = @TenantId AND r.child_id = @ChildId
              AND (r.effective_to IS NULL OR r.effective_to >= NOW())";

        var rawRestrictions = (await conn.QueryAsync<dynamic>(restrictionsSql, new { TenantId = _tenantContext.TenantId, ChildId = childId })).ToList();
        var restrictions = rawRestrictions.Select(r => new CustodyRestrictionDto
        {
            Id = (Guid)r.id,
            PersonName = (string)r.PersonName,
            RestrictionType = (string)r.RestrictionType,
            Notes = (string?)r.notes,
            EffectiveFrom = (DateTimeOffset?)r.EffectiveFrom,
            EffectiveTo = (DateTimeOffset?)r.EffectiveTo
        }).ToList();

        // 3. Fetch explicit authorized pickups
        var pickupsSql = @"
            SELECT ap.id, ap.child_id AS ChildId, ap.person_id AS PersonId, ap.relationship, ap.pickup_code AS PickupCode,
                   ap.photo_url AS PhotoUrl, ap.is_active AS IsActive, ap.notes,
                   COALESCE(p.first_name || ' ' || p.last_name, ap.relationship) AS FullName,
                   p.phone AS Phone
            FROM authorized_pickups ap
            LEFT JOIN persons p ON ap.person_id = p.id
            WHERE ap.tenant_id = @TenantId AND ap.child_id = @ChildId AND ap.is_active = TRUE
            ORDER BY ap.is_active DESC";

        var rawPickups = (await conn.QueryAsync<dynamic>(pickupsSql, new { TenantId = _tenantContext.TenantId, ChildId = childId })).ToList();
        var pickups = rawPickups.Select(p => new AuthorizedPickupDto
        {
            Id = (Guid)p.id,
            ChildId = (Guid)p.ChildId,
            PersonId = (Guid?)p.PersonId,
            FullName = (string)p.FullName,
            Relationship = (string)(p.relationship ?? "Authorized Pickup"),
            Phone = (string?)p.Phone,
            PhotoUrl = (string?)p.PhotoUrl,
            PickupCode = (string?)p.PickupCode,
            IsActive = (bool)p.IsActive,
            IsPrimaryGuardian = ((string)(p.relationship ?? "")).Contains("Parent", StringComparison.OrdinalIgnoreCase) 
                             || ((string)(p.relationship ?? "")).Contains("Mother", StringComparison.OrdinalIgnoreCase)
                             || ((string)(p.relationship ?? "")).Contains("Father", StringComparison.OrdinalIgnoreCase),
            Notes = (string?)p.notes
        }).ToList();

        // 4. Fetch family relationships (parents & legal guardians)
        var familySql = @"
            SELECT fr.id, fr.child_id AS ChildId, fr.person_id AS PersonId, fr.relationship_type AS Relationship,
                   fr.is_primary_contact AS IsPrimary, fr.is_emergency_contact AS IsEmergency,
                   p.first_name || ' ' || p.last_name AS FullName, p.phone AS Phone, p.photo_url AS PhotoUrl
            FROM family_relationships fr
            JOIN persons p ON fr.person_id = p.id
            WHERE fr.tenant_id = @TenantId AND fr.child_id = @ChildId
              AND (fr.is_authorized_pickup = TRUE OR fr.is_primary_contact = TRUE)";

        var rawFamily = (await conn.QueryAsync<dynamic>(familySql, new { TenantId = _tenantContext.TenantId, ChildId = childId })).ToList();
        foreach (var f in rawFamily)
        {
            Guid personId = (Guid)f.PersonId;
            if (!pickups.Any(p => p.PersonId == personId))
            {
                pickups.Insert(0, new AuthorizedPickupDto
                {
                    Id = Guid.NewGuid(),
                    ChildId = childId,
                    PersonId = personId,
                    FullName = (string)f.FullName,
                    Relationship = (string)(f.Relationship ?? "Legal Guardian"),
                    Phone = (string?)f.Phone,
                    PhotoUrl = (string?)f.PhotoUrl,
                    IsActive = true,
                    IsPrimaryGuardian = (bool)f.IsPrimary,
                    Notes = "Verified legal guardian relationship."
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
                Notes = "Primary guardian visual verification required."
            });
        }

        // 5. Fetch today's attendance record
        var attSql = @"
            SELECT * FROM child_attendance
            WHERE tenant_id = @TenantId AND child_id = @ChildId AND attendance_date = @Date
            LIMIT 1";

        var todayAtt = await conn.QueryFirstOrDefaultAsync<ChildAttendance>(attSql, new { TenantId = _tenantContext.TenantId, ChildId = childId, Date = targetDate.Date });

        return new ChildPickupSecurityProfile
        {
            ChildId = childId,
            ChildName = $"{child.FirstName} {child.LastName}".Trim(),
            ClassroomName = (string)(child.ClassroomName ?? "Classroom"),
            BranchName = (string)(child.BranchName ?? "Main Branch"),
            PhotoUrl = (string?)child.PhotoUrl,
            CustodyRestrictions = restrictions,
            AuthorizedPickups = pickups,
            TodayAttendance = todayAtt
        };
    }

    public async Task<RecordChildDepartureResponse> VerifyAndRecordChildDepartureAsync(RecordChildDepartureRequest request, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        // 1. Check for custody restriction block
        var checkRestrictionSql = @"
            SELECT r.id, COALESCE(p.first_name || ' ' || p.last_name, '') AS PersonName, r.notes
            FROM child_person_restrictions r
            LEFT JOIN persons p ON r.person_id = p.id
            WHERE r.tenant_id = @TenantId AND r.child_id = @ChildId
              AND (r.effective_to IS NULL OR r.effective_to >= NOW())";

        var activeRestrictions = (await conn.QueryAsync<dynamic>(checkRestrictionSql, new { TenantId = _tenantContext.TenantId, ChildId = request.ChildId })).ToList();
        
        foreach (var r in activeRestrictions)
        {
            string rName = (string)r.PersonName;
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
        var attSql = @"
            SELECT * FROM child_attendance
            WHERE tenant_id = @TenantId AND child_id = @ChildId AND attendance_date = @Date
            LIMIT 1";

        var existingAtt = await conn.QueryFirstOrDefaultAsync<ChildAttendance>(attSql, new { 
            TenantId = _tenantContext.TenantId, 
            ChildId = request.ChildId, 
            Date = DateTime.Today 
        });

        var departureTime = request.DepartureTime ?? DateTime.Now.TimeOfDay;
        Guid attId = existingAtt?.Id ?? request.AttendanceId ?? Guid.NewGuid();

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

        attendanceRecord.Status = "Present"; // Remains on daily roster as attended today
        attendanceRecord.DepartureTime = departureTime;
        attendanceRecord.PickupPersonName = request.PickupPersonName;
        attendanceRecord.PickupPersonRelationship = request.PickupPersonRelationship;
        attendanceRecord.PickupVerificationMethod = request.PickupVerificationMethod;
        attendanceRecord.PickupNotes = request.StaffNotes;
        attendanceRecord.AuthorizedPickupId = request.AuthorizedPickupId;
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
        var profile = await GetChildPickupSecurityProfileAsync(childId, null, ct);
        return profile?.AuthorizedPickups ?? Enumerable.Empty<AuthorizedPickupDto>();
    }

    public async Task AddAuthorizedPickupAsync(AuthorizedPickup pickup, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        pickup.TenantId = _tenantContext.TenantId;
        if (pickup.Id == Guid.Empty) pickup.Id = Guid.NewGuid();

        var sql = @"
            INSERT INTO authorized_pickups (
                id, tenant_id, child_id, person_id, relationship, pickup_code, photo_url, valid_from, valid_to, is_active, notes
            )
            VALUES (
                @Id, @TenantId, @ChildId, @PersonId, @Relationship, @PickupCode, @PhotoUrl, @ValidFrom, @ValidTo, @IsActive, @Notes
            )";

        await conn.ExecuteAsync(sql, pickup);
    }
}

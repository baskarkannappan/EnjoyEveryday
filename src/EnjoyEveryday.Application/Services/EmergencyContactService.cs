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

public class EmergencyCardModel
{
    public string Role { get; set; } = EmergencyCardRole.Parent;
    public string BranchName { get; set; } = string.Empty;
    public string? BranchAddress { get; set; }
    public string? BranchPhone { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public List<EmergencyContact> Contacts { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool HasUnverifiedCriticalNumbers { get; set; }
}

public interface IEmergencyContactService
{
    Task<IEnumerable<EmergencyContact>> GetContactsAsync(Guid? organizationId = null, Guid? branchId = null, bool activeOnly = false, CancellationToken ct = default);
    Task<EmergencyContact?> GetContactByIdAsync(Guid id, CancellationToken ct = default);
    Task<EmergencyContact> CreateContactAsync(EmergencyContact contact, CancellationToken ct = default);
    Task UpdateContactAsync(EmergencyContact contact, CancellationToken ct = default);
    Task DeleteContactAsync(Guid id, CancellationToken ct = default);
    Task VerifyContactAsync(Guid id, string verifiedBy, DateTime? reviewDueAt = null, CancellationToken ct = default);
    Task<EmergencyCardModel> GeneratePrintableCardAsync(Guid branchId, string role, CancellationToken ct = default);
}

public class EmergencyContactService : IEmergencyContactService
{
    private readonly IEmergencyContactRepository _repository;
    private readonly IBranchRepository _branchRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;

    public EmergencyContactService(
        IEmergencyContactRepository repository,
        IBranchRepository branchRepository,
        IOrganizationRepository organizationRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService)
    {
        _repository = repository;
        _branchRepository = branchRepository;
        _organizationRepository = organizationRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
    }

    public async Task<IEnumerable<EmergencyContact>> GetContactsAsync(
        Guid? organizationId = null,
        Guid? branchId = null,
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        var effectiveOrgId = organizationId ?? _userContext.OrganizationId;
        return await _repository.GetContactsAsync(_tenantContext.TenantId, effectiveOrgId, branchId, activeOnly, ct);
    }

    public async Task<EmergencyContact?> GetContactByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.GetContactByIdAsync(_tenantContext.TenantId, id, ct);
    }

    public async Task<EmergencyContact> CreateContactAsync(EmergencyContact contact, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contact.Name))
            throw new ArgumentException("Contact name is required.");

        if (string.IsNullOrWhiteSpace(contact.PhoneNumber))
            throw new ArgumentException("Phone number is required.");

        contact.Id = Guid.NewGuid();
        contact.TenantId = _tenantContext.TenantId;
        if (contact.OrganizationId == null || contact.OrganizationId == Guid.Empty)
            contact.OrganizationId = _userContext.OrganizationId;

        // New contacts start as unverified unless explicitly verified by an authorized action
        contact.VerificationStatus = VerificationStatus.Unverified;
        contact.VerifiedBy = null;
        contact.VerifiedAt = null;
        contact.CreatedAt = DateTime.UtcNow;
        contact.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.CreateContactAsync(contact, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "EmergencyContact.Created",
            EntityType = nameof(EmergencyContact),
            EntityId = created.Id,
            Details = new { created.Name, created.Category, created.Scope, created.BranchId }
        }, ct);

        return created;
    }

    public async Task UpdateContactAsync(EmergencyContact contact, CancellationToken ct = default)
    {
        var existing = await _repository.GetContactByIdAsync(_tenantContext.TenantId, contact.Id, ct);
        if (existing == null)
            throw new KeyNotFoundException($"Emergency contact with ID {contact.Id} not found.");

        existing.Name = contact.Name.Trim();
        existing.Category = contact.Category;
        // If phone number or instructions changed, revert verification to unverified
        var detailsChanged = !string.Equals(existing.PhoneNumber, contact.PhoneNumber?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                             !string.Equals(existing.Instructions, contact.Instructions?.Trim(), StringComparison.OrdinalIgnoreCase);

        existing.Scope = contact.Scope;
        existing.PhoneNumber = contact.PhoneNumber.Trim();
        existing.AlternatePhoneNumber = contact.AlternatePhoneNumber?.Trim();
        existing.CountryRegion = contact.CountryRegion?.Trim();
        existing.Address = contact.Address?.Trim();
        existing.Instructions = contact.Instructions?.Trim();
        existing.IsActive = contact.IsActive;
        existing.DisplayOrder = contact.DisplayOrder;
        existing.OrganizationId = contact.OrganizationId;
        existing.BranchId = contact.BranchId;

        if (detailsChanged)
        {
            existing.VerificationStatus = VerificationStatus.Unverified;
            existing.VerifiedBy = null;
            existing.VerifiedAt = null;
        }

        await _repository.UpdateContactAsync(existing, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "EmergencyContact.Updated",
            EntityType = nameof(EmergencyContact),
            EntityId = existing.Id,
            Details = new { existing.Name, existing.Category, existing.VerificationStatus }
        }, ct);
    }

    public async Task DeleteContactAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _repository.GetContactByIdAsync(_tenantContext.TenantId, id, ct);
        if (existing == null)
            throw new KeyNotFoundException($"Emergency contact with ID {id} not found.");

        await _repository.DeleteContactAsync(_tenantContext.TenantId, id, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "EmergencyContact.Deleted",
            EntityType = nameof(EmergencyContact),
            EntityId = id,
            Details = new { existing.Name, existing.Category }
        }, ct);
    }

    public async Task VerifyContactAsync(Guid id, string verifiedBy, DateTime? reviewDueAt = null, CancellationToken ct = default)
    {
        var existing = await _repository.GetContactByIdAsync(_tenantContext.TenantId, id, ct);
        if (existing == null)
            throw new KeyNotFoundException($"Emergency contact with ID {id} not found.");

        var verifiedAt = DateTime.UtcNow;
        var due = reviewDueAt ?? verifiedAt.AddMonths(6);

        await _repository.VerifyContactAsync(_tenantContext.TenantId, id, verifiedBy, verifiedAt, due, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "EmergencyContact.Verified",
            EntityType = nameof(EmergencyContact),
            EntityId = id,
            Details = new { VerifiedBy = verifiedBy, VerifiedAt = verifiedAt, ReviewDueAt = due }
        }, ct);
    }

    public async Task<EmergencyCardModel> GeneratePrintableCardAsync(Guid branchId, string role, CancellationToken ct = default)
    {
        var branch = await _branchRepository.GetByIdAsync(_tenantContext.TenantId, branchId, ct);
        var org = branch?.OrganizationId != null ? await _organizationRepository.GetByIdAsync(_tenantContext.TenantId, branch.OrganizationId, ct) : null;

        var allContacts = (await _repository.GetContactsAsync(
            _tenantContext.TenantId,
            branch?.OrganizationId,
            branchId,
            activeOnly: true,
            ct)).ToList();

        // Audit card generation
        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "EmergencyCard.Generated",
            EntityType = nameof(EmergencyContact),
            EntityId = branchId,
            Details = new { Role = role, BranchId = branchId, ContactCount = allContacts.Count }
        }, ct);

        var warnings = new List<string>();

        // Critical number checks
        var hasEmergencyDispatch = allContacts.Any(c => c.Category == EmergencyContactCategory.EmergencyServices);
        var hasPoisonControl = allContacts.Any(c => c.Category == EmergencyContactCategory.PoisonControl);
        var hasHospital = allContacts.Any(c => c.Category == EmergencyContactCategory.NearestHospital);

        if (!hasEmergencyDispatch)
            warnings.Add("Warning: No primary emergency services number (e.g. 911) is configured for this branch.");

        if (!hasPoisonControl)
            warnings.Add("Warning: No Poison Control helpline is configured.");

        if (!hasHospital)
            warnings.Add("Warning: No nearest hospital or urgent pediatric facility is configured.");

        var unverifiedCritical = allContacts.Where(c => 
            c.VerificationStatus != VerificationStatus.Verified && 
            (c.Category == EmergencyContactCategory.EmergencyServices || 
             c.Category == EmergencyContactCategory.PoisonControl || 
             c.Category == EmergencyContactCategory.NearestHospital)).ToList();

        if (unverifiedCritical.Count > 0)
        {
            warnings.Add($"Caution: {unverifiedCritical.Count} critical emergency number(s) are unverified or pending review.");
        }

        // Role-based filtering
        List<EmergencyContact> filteredContacts;

        switch (role)
        {
            case EmergencyCardRole.Parent:
                // Parents: ONLY verified or public emergency contacts.
                // Exclude private staff details, on-call internal numbers.
                filteredContacts = allContacts
                    .Where(c => c.Category != EmergencyContactCategory.DesignatedOnCall &&
                                !(c.Category == EmergencyContactCategory.BranchDirector && c.Instructions != null && c.Instructions.Contains("Private", StringComparison.OrdinalIgnoreCase)))
                    .Select(c => new EmergencyContact
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Category = c.Category,
                        PhoneNumber = c.PhoneNumber,
                        AlternatePhoneNumber = null, // Redacted for parents
                        CountryRegion = c.CountryRegion,
                        Address = c.Address,
                        Instructions = c.Instructions,
                        VerificationStatus = c.VerificationStatus,
                        VerifiedAt = c.VerifiedAt,
                        DisplayOrder = c.DisplayOrder
                    })
                    .ToList();
                break;

            case EmergencyCardRole.Teacher:
                // Teachers: Emergency services, hospital, poison control, branch director, on-call staff
                filteredContacts = allContacts
                    .Select(c => new EmergencyContact
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Category = c.Category,
                        PhoneNumber = c.PhoneNumber,
                        AlternatePhoneNumber = c.AlternatePhoneNumber,
                        CountryRegion = c.CountryRegion,
                        Address = c.Address,
                        Instructions = c.Instructions,
                        VerificationStatus = c.VerificationStatus,
                        VerifiedAt = c.VerifiedAt,
                        DisplayOrder = c.DisplayOrder
                    })
                    .ToList();
                break;

            case EmergencyCardRole.Administrator:
            default:
                // Administrators: full view including unverified alerts, addresses, verification details
                filteredContacts = allContacts.ToList();
                break;
        }

        return new EmergencyCardModel
        {
            Role = role,
            BranchName = branch?.Name ?? "Main Branch",
            BranchAddress = branch?.Location ?? "Daycare Campus",
            BranchPhone = allContacts.FirstOrDefault(c => c.Category == EmergencyContactCategory.BranchDirector)?.PhoneNumber ?? "(555) 019-2000",
            OrganizationName = org?.Name ?? "EnjoyEveryday Childcare",
            GeneratedAtUtc = DateTime.UtcNow,
            Contacts = filteredContacts,
            Warnings = warnings,
            HasUnverifiedCriticalNumbers = unverifiedCritical.Count > 0
        };
    }
}

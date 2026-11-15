using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public interface IFacilityProfileService
{
    /// <summary>
    /// Gets the facility profile for a specific branch, falling back to the organization-level profile if not overridden.
    /// </summary>
    Task<FacilityProfileDto?> GetProfileAsync(Guid organizationId, Guid? branchId, CancellationToken ct = default);

    /// <summary>
    /// Gets the exact profile record for an organization or branch without fallback (used for editing).
    /// </summary>
    Task<FacilityProfileDto?> GetExactProfileAsync(Guid organizationId, Guid? branchId, CancellationToken ct = default);

    /// <summary>
    /// Saves or updates a facility profile. Accessible only by Org Admins and Branch Admins.
    /// </summary>
    Task<FacilityProfileDto> SaveProfileAsync(SaveFacilityProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets list of branches accessible to the current user (Org Admin sees all, Branch Admin sees their assigned).
    /// </summary>
    Task<IEnumerable<BranchOptionDto>> GetEditableBranchesAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active branches of an organization for directory lookup and campus switching across all roles.
    /// </summary>
    Task<IEnumerable<BranchOptionDto>> GetPublicBranchesAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Submits a new contact inquiry from parents, teachers, or visitors.
    /// </summary>
    Task<ContactInquiryDto> SubmitInquiryAsync(CreateContactInquiryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Lists inquiries for an organization or branch, with optional status filter.
    /// </summary>
    Task<IEnumerable<ContactInquiryDto>> GetInquiriesAsync(Guid organizationId, Guid? branchId = null, string? status = null, CancellationToken ct = default);

    /// <summary>
    /// Cross-tenant inquiry discovery for Platform Administrators.
    /// </summary>
    Task<IEnumerable<ContactInquiryDto>> GetAllPlatformInquiriesAsync(Guid? organizationId = null, string? status = null, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of an inquiry (e.g. New -> InReview -> Resolved).
    /// </summary>
    Task UpdateInquiryStatusAsync(UpdateInquiryStatusRequest request, CancellationToken ct = default);
}


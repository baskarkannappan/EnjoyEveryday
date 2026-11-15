using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Application.Services;

public class FacilityProfileDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public bool IsBranchLevel => BranchId.HasValue;

    // Branding & Identity
    public string Title { get; set; } = string.Empty;
    public string? Motto { get; set; }
    public string? AboutStory { get; set; }
    public string? LogoMediaUrl { get; set; }
    public string? CoverMediaUrl { get; set; }
    public List<string> GalleryMediaUrls { get; set; } = new();

    // Contact Details & Hours
    public string? Phone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Email { get; set; }
    public string? InquiryEmail { get; set; }
    public string? OperatingHours { get; set; }

    // Location & Directions
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? MapUrl { get; set; }
    public string? DirectionsNotes { get; set; }

    // Philosophy & Core Values
    public List<string> CoreValues { get; set; } = new();

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SaveFacilityProfileRequest
{
    public Guid? Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Motto { get; set; }
    public string? AboutStory { get; set; }
    public string? LogoMediaUrl { get; set; }
    public string? CoverMediaUrl { get; set; }
    public List<string> GalleryMediaUrls { get; set; } = new();

    public string? Phone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Email { get; set; }
    public string? InquiryEmail { get; set; }
    public string? OperatingHours { get; set; }

    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? MapUrl { get; set; }
    public string? DirectionsNotes { get; set; }

    public List<string> CoreValues { get; set; } = new();
}

public class ContactInquiryDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid? SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string? SenderPhone { get; set; }
    public string Category { get; set; } = "General";
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "New"; // New, InReview, Resolved
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class CreateContactInquiryRequest
{
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string? SenderPhone { get; set; }
    public string Category { get; set; } = "General";
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class UpdateInquiryStatusRequest
{
    public Guid InquiryId { get; set; }
    public string Status { get; set; } = "Resolved";
    public string? AdminNotes { get; set; }
}

public class BranchOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}


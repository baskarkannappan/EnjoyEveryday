using System;

namespace EnjoyEveryday.Domain.Entities;

public static class EmergencyContactScope
{
    public const string Jurisdiction = "Jurisdiction";
    public const string Organization = "Organization";
    public const string Branch = "Branch";
}

public static class EmergencyContactCategory
{
    public const string EmergencyServices = "EmergencyServices";
    public const string Police = "Police";
    public const string FireService = "FireService";
    public const string Ambulance = "Ambulance";
    public const string PoisonControl = "PoisonControl";
    public const string NearestHospital = "NearestHospital";
    public const string LocalEmergencyManagement = "LocalEmergencyManagement";
    public const string BranchDirector = "BranchDirector";
    public const string DesignatedOnCall = "DesignatedOnCall";
    public const string Other = "Other";

    public static string GetDisplayName(string category) => category switch
    {
        EmergencyServices => "Emergency Services (911/999)",
        Police => "Local Police Department",
        FireService => "Fire & Rescue Department",
        Ambulance => "Medical Emergency / Ambulance",
        PoisonControl => "Poison Control Center",
        NearestHospital => "Nearest Hospital / Pediatric ER",
        LocalEmergencyManagement => "Emergency Management Agency",
        BranchDirector => "Branch Director / Administrator",
        DesignatedOnCall => "Designated On-Call Staff",
        _ => "Other Approved Contact"
    };

    public static string GetIcon(string category) => category switch
    {
        EmergencyServices => "fa-triangle-exclamation",
        Police => "fa-shield-halved",
        FireService => "fa-fire-extinguisher",
        Ambulance => "fa-truck-medical",
        PoisonControl => "fa-skull-crossbones",
        NearestHospital => "fa-hospital",
        LocalEmergencyManagement => "fa-building-shield",
        BranchDirector => "fa-user-tie",
        DesignatedOnCall => "fa-phone-volume",
        _ => "fa-phone"
    };
}

public static class VerificationStatus
{
    public const string Verified = "Verified";
    public const string Unverified = "Unverified";
    public const string Overdue = "Overdue";
}

public static class EmergencyCardRole
{
    public const string Parent = "Parent";
    public const string Teacher = "Teacher";
    public const string Administrator = "Administrator";
}

public class EmergencyContact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ChildId { get; set; } // Kept for backwards compatibility with legacy stub
    public string Scope { get; set; } = EmergencyContactScope.Branch;
    public string Category { get; set; } = EmergencyContactCategory.EmergencyServices;
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AlternatePhoneNumber { get; set; }
    public string? CountryRegion { get; set; }
    public string? Address { get; set; }
    public string? Instructions { get; set; }
    public string VerificationStatus { get; set; } = EnjoyEveryday.Domain.Entities.VerificationStatus.Unverified;
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ReviewDueAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

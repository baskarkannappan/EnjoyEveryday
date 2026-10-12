using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public static class FloorMapStatus
{
    public const string Draft = "Draft";
    public const string PendingReview = "PendingReview";
    public const string Published = "Published";
    public const string Superseded = "Superseded";
    public const string Retired = "Retired";
}

public static class SafetyMarkerType
{
    public const string FirstAid = "FirstAid";
    public const string EmergencyExit = "EmergencyExit";
    public const string EvacuationRoute = "EvacuationRoute";
    public const string FireExtinguisher = "FireExtinguisher";
    public const string FireAlarm = "FireAlarm";
    public const string AssemblyPoint = "AssemblyPoint";
    public const string AED = "AED";
    public const string AccessibleRoute = "AccessibleRoute";
    public const string EmergencyEquipment = "EmergencyEquipment";
    public const string Other = "Other";

    public static string GetIcon(string markerType) => markerType switch
    {
        FirstAid => "fa-kit-medical",
        EmergencyExit => "fa-person-running",
        EvacuationRoute => "fa-route",
        FireExtinguisher => "fa-fire-extinguisher",
        FireAlarm => "fa-bell",
        AssemblyPoint => "fa-users-line",
        AED => "fa-heart-pulse",
        AccessibleRoute => "fa-wheelchair",
        EmergencyEquipment => "fa-toolbox",
        _ => "fa-location-dot"
    };

    public static string GetShortCode(string markerType) => markerType switch
    {
        FirstAid => "FA",
        EmergencyExit => "EXIT",
        EvacuationRoute => "EVAC",
        FireExtinguisher => "FE",
        FireAlarm => "ALARM",
        AssemblyPoint => "ASM",
        AED => "AED",
        AccessibleRoute => "ACC",
        EmergencyEquipment => "EQ",
        _ => "GEN"
    };

    public static string GetColor(string markerType) => markerType switch
    {
        FirstAid => "#198754",           // Green
        EmergencyExit => "#0d6efd",      // Blue
        EvacuationRoute => "#0dcaf0",    // Cyan
        FireExtinguisher => "#dc3545",   // Red
        FireAlarm => "#fd7e14",          // Orange
        AssemblyPoint => "#20c997",      // Teal
        AED => "#d63384",                // Pink / Magenta
        AccessibleRoute => "#6610f2",    // Indigo
        EmergencyEquipment => "#6f42c1", // Purple
        _ => "#6c757d"                   // Slate Gray
    };
}

public class BranchFloorMap
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BuildingArea { get; set; }
    public string FloorLevel { get; set; } = "Floor 1";
    public string? Description { get; set; }
    public string FileStorageKey { get; set; } = string.Empty;
    public string FileType { get; set; } = "image/png";
    public long FileSize { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string Status { get; set; } = FloorMapStatus.Draft;
    public DateTime? ReviewDueAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? SupersedesMapId { get; set; }
    public int RowVersion { get; set; } = 1;
    public string? EmergencyInstructions { get; set; }

    public List<FloorMapMarker> Markers { get; set; } = new();
}

public class FloorMapMarker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FloorMapId { get; set; }
    public string MarkerType { get; set; } = SafetyMarkerType.FirstAid;
    public string Label { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public double NormalizedX { get; set; }
    public double NormalizedY { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

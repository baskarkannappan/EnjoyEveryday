using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EnjoyEveryday.Domain.Entities;

public class TenantPreferences
{
    public Guid TenantId { get; set; }
    public string Currency { get; set; } = "INR";
    public string TimeZone { get; set; } = "Asia/Kolkata";

    public OperatingHoursConfig OperatingHours { get; set; } = new();
    public Dictionary<string, int> StatutoryRatios { get; set; } = new()
    {
        { "Infant", 4 },
        { "Toddler", 7 },
        { "Preschool", 10 },
        { "Kindergarten", 15 }
    };
    public BillingRulesConfig BillingRules { get; set; } = new();
    public SafetyPoliciesConfig SafetyPolicies { get; set; } = new();
    public Dictionary<string, string> CustomSettings { get; set; } = new();
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int GetMaxRatioForAgeGroup(string ageGroup)
    {
        if (StatutoryRatios.TryGetValue(ageGroup, out var ratio) && ratio > 0)
        {
            return ratio;
        }

        // Standard statutory fallbacks
        return ageGroup.ToLowerInvariant() switch
        {
            "infant" => 4,
            "toddler" => 7,
            "preschool" => 10,
            _ => 15
        };
    }
}

public class OperatingHoursConfig
{
    public string OpenTime { get; set; } = "08:00";
    public string CloseTime { get; set; } = "18:00";
    public List<string> WorkingDays { get; set; } = new()
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"
    };
}

public class BillingRulesConfig
{
    public decimal SiblingDiscountPercent { get; set; } = 10.0m;
    public decimal LatePickupFeePerMinute { get; set; } = 1.0m;
    public int GracePeriodMinutes { get; set; } = 15;
    public int InvoiceDueDays { get; set; } = 15;
}

public class SafetyPoliciesConfig
{
    public int NapCheckIntervalMinutes { get; set; } = 15;
    public bool RequireDualMedicationSignoff { get; set; } = false;
    public int MinEmergencyContacts { get; set; } = 2;
}

public class MasterLookupItem
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MetadataJson { get; set; }
}

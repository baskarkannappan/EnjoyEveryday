namespace EnjoyEveryday.Domain.Entities;

public enum CareEventType
{
    DiaperChange,
    Meal,
    SleepRest,
    Toileting,
    Hygiene,
    Comfort
}

public static class CareEventTypes
{
    public const string DiaperChange = "DiaperChange";
    public const string Meal = "Meal";
    public const string SleepRest = "SleepRest";
    public const string Toileting = "Toileting";
    public const string Hygiene = "Hygiene";
    public const string Comfort = "Comfort";
}

public class ChildCareEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid ChildId { get; set; }
    public string EventType { get; set; } = CareEventTypes.DiaperChange;
    public DateTime EventTime { get; set; } = DateTime.UtcNow;

    // Diaper Details
    public string? DiaperCondition { get; set; } // Wet, Soiled, WetAndSoiled, DryCheck
    public string? SkinObservation { get; set; }

    // Meal Details
    public string? MealType { get; set; } // Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner, Bottle, Water
    public string? FoodOffered { get; set; }
    public string? AmountConsumed { get; set; } // All, Most, Some, None, NotRecorded
    public decimal? FluidOz { get; set; }

    // Sleep Details
    public string? RestStatus { get; set; } // FellAsleep, Rested, DifficultySettling, WokeNaturally, Awakened

    // Toileting Details
    public string? ToiletingActivity { get; set; } // Attempt, Success, Accident, Assistance

    // Hygiene Details
    public string? HygieneType { get; set; } // Handwashing, FaceCleaning, ClothingChange, AccidentCleaning, PersonalCare

    // Comfort & Wellbeing
    public string? ComfortMood { get; set; } // Happy, Settled, NeededComfort, Tired, QuietRest, UpsetSupported

    // Metadata
    public string? Notes { get; set; }
    public bool ParentVisible { get; set; } = true;
    public Guid RecordedByUserId { get; set; }
    public string? RecordedByName { get; set; }
    public string? CorrectionReason { get; set; }
    public bool IsCorrected { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Projection helpers
    public string? ChildName { get; set; }
    public string? ClassroomName { get; set; }
}

public class ChildNapSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid ChildId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string Status { get; set; } = "Active"; // Active, Completed, Cancelled
    public string? Outcome { get; set; } // WokeNaturally, Awakened, RestedOnly, Restless
    public string? Notes { get; set; }
    public Guid StartedByUserId { get; set; }
    public string? StartedByName { get; set; }
    public Guid? EndedByUserId { get; set; }
    public string? EndedByName { get; set; }
    public bool ParentVisible { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? ChildName { get; set; }
}

public class ChildCarePlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ChildId { get; set; }
    public int Version { get; set; } = 1;
    public string? DiaperRoutineGuidance { get; set; }
    public string? FeedingInstructions { get; set; }
    public string? BottleScheduleGuidance { get; set; }
    public string? SleepRoutineGuidance { get; set; }
    public string? ToiletingSupportPlan { get; set; }
    public string? ComfortCalmingPreferences { get; set; }
    public string? RequiredSuppliesNote { get; set; }
    public string? DietaryAllergiesSummary { get; set; }
    public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ReviewDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, Superseded, PendingApproval
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? ChildName { get; set; }
}

public class ChildCareSupply
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid ChildId { get; set; }
    public string SupplyType { get; set; } = "Diapers"; // Diapers, Wipes, SpareClothes, Bottles, Ointment, ComfortItem, Other
    public string Status { get; set; } = "Adequate"; // Adequate, Low, Depleted
    public string? Notes { get; set; }
    public string? ThresholdNote { get; set; }
    public Guid LastReportedByUserId { get; set; }
    public string? LastReportedByName { get; set; }
    public bool ParentReminderSent { get; set; }
    public DateTime? LastReminderAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? ChildName { get; set; }
    public string? ClassroomName { get; set; }
}

namespace EnjoyEveryday.Domain.Constants;

/// <summary>
/// Type-safe constants and options for child journey observations and moments.
/// Preserves 100% backward compatibility with PostgreSQL string storage and Dapper mappings.
/// </summary>
public static class JourneyTypes
{
    public const string Observation = "Observation";
    public const string ActivityObservation = "ActivityObservation";
    public const string LittleMoment = "LittleMoment";
    public const string Discovery = "Discovery";
    public const string Milestone = "Milestone";
    public const string Attendance = "Attendance";
    public const string ArtAndSensory = "Art & Sensory";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Observation,
        ActivityObservation,
        LittleMoment,
        Discovery,
        Milestone,
        Attendance,
        ArtAndSensory
    };
}

/// <summary>
/// Visibility review states for parent sharing in the child portfolio/journey.
/// </summary>
public static class ParentVisibilityStatuses
{
    public const string Draft = "Draft";
    public const string PendingReview = "PendingReview";
    public const string Approved = "Approved";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Draft,
        PendingReview,
        Approved
    };
}

/// <summary>
/// Standard participation modes for children in activity sessions.
/// </summary>
public static class ParticipationTypes
{
    public const string Participated = "Participated";
    public const string Observed = "Observed";
    public const string Assisted = "Assisted";
}

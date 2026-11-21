namespace EnjoyEveryday.Domain.Constants;

/// <summary>
/// Standard status values for child and teacher attendance registers.
/// Preserves 100% backward compatibility with PostgreSQL string storage and Dapper mappings.
/// </summary>
public static class AttendanceStatuses
{
    public const string Present = "Present";
    public const string CheckedOut = "CheckedOut";
    public const string Late = "Late";
    public const string Absent = "Absent";
    public const string Excused = "Excused";
    public const string NotMarked = "Not marked";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Present,
        CheckedOut,
        Late,
        Absent,
        Excused,
        NotMarked
    };

    public static bool IsPresentOrLate(string? status) =>
        string.Equals(status, Present, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, Late, StringComparison.OrdinalIgnoreCase);

    public static bool IsCheckedIn(string? status) =>
        string.Equals(status, Present, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, Late, StringComparison.OrdinalIgnoreCase);
}

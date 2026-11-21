using System;

namespace EnjoyEveryday.Domain.Entities;

public class ChildAttendance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid ChildId { get; set; }
    public DateTime AttendanceDate { get; set; }

    /// <summary>
    /// Attendance status (Present, CheckedOut, Late, Absent, Excused, Not marked).
    /// See <see cref="Constants.AttendanceStatuses"/> for standard constants.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    public TimeSpan? ArrivalTime { get; set; }
    public TimeSpan? DepartureTime { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedByUserId { get; set; }
    
    // Secure pickup and departure verification
    public string? PickupPersonName { get; set; }
    public string? PickupPersonRelationship { get; set; }
    public string? PickupVerificationMethod { get; set; }
    public string? PickupNotes { get; set; }
    public Guid? AuthorizedPickupId { get; set; }
    public Guid? DepartureVerifiedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void CheckIn(TimeSpan arrivalTime, Guid? recordedByUserId = null, string? notes = null)
    {
        Status = Constants.AttendanceStatuses.Present;
        ArrivalTime = arrivalTime;
        RecordedByUserId = recordedByUserId ?? RecordedByUserId;
        if (!string.IsNullOrWhiteSpace(notes)) Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CheckOut(
        TimeSpan departureTime,
        string? pickupPersonName = null,
        string? pickupRelationship = null,
        string? verificationMethod = null,
        Guid? authorizedPickupId = null,
        Guid? verifiedByUserId = null,
        string? pickupNotes = null)
    {
        Status = Constants.AttendanceStatuses.CheckedOut;
        DepartureTime = departureTime;
        PickupPersonName = pickupPersonName ?? PickupPersonName;
        PickupPersonRelationship = pickupRelationship ?? PickupPersonRelationship;
        PickupVerificationMethod = verificationMethod ?? PickupVerificationMethod;
        AuthorizedPickupId = authorizedPickupId ?? AuthorizedPickupId;
        DepartureVerifiedByUserId = verifiedByUserId ?? DepartureVerifiedByUserId;
        if (!string.IsNullOrWhiteSpace(pickupNotes)) PickupNotes = pickupNotes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAbsent(string? notes = null, Guid? recordedByUserId = null)
    {
        Status = Constants.AttendanceStatuses.Absent;
        ArrivalTime = null;
        DepartureTime = null;
        RecordedByUserId = recordedByUserId ?? RecordedByUserId;
        if (!string.IsNullOrWhiteSpace(notes)) Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
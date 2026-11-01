using System;

namespace EnjoyEveryday.Shared.Events;

/// <summary>
/// Dispatched when an educator or parent sends a classroom communication message.
/// </summary>
public record ClassroomMessageSentEvent(
    Guid TenantId,
    Guid MessageId,
    Guid ChildId,
    Guid? ClassroomId,
    string SenderRole,
    string SenderName,
    string ContentPreview) : DomainEventBase(TenantId);

/// <summary>
/// Dispatched when child attendance (check-in, check-out, absence) is recorded.
/// </summary>
public record ChildAttendanceRecordedEvent(
    Guid TenantId,
    Guid AttendanceId,
    Guid ChildId,
    Guid ClassroomId,
    string Status,
    DateTimeOffset RecordedAt) : DomainEventBase(TenantId);

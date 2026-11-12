using System;

namespace EnjoyEveryday.Domain.Entities;

public class PlatformBusinessEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid? InitiatedByUserId { get; set; }
    public string? InitiatorName { get; set; }
    public string? TransactionReference { get; set; }
    public string TransactionCategory { get; set; } = "General";
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = "Completed";
    public string? EventPayload { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PlatformAdminNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BusinessEventId { get; set; }
    public Guid? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}


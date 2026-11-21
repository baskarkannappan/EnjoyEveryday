using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public static class GiftingRecipientTypes
{
    public const string BirthdayGift = "BirthdayGift";
    public const string ClassroomContribution = "ClassroomContribution";
}

public static class GiftingApprovalStatuses
{
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
}

public static class GiftingPaymentStatuses
{
    public const string AwaitingPayment = "AwaitingPayment";
    public const string CashPaymentPending = "CashPaymentPending";
    public const string PaymentVerificationPending = "PaymentVerificationPending";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Paid = "Paid";
    public const string PaymentFailed = "PaymentFailed";
    public const string PaymentRejected = "PaymentRejected";
    public const string Cancelled = "Cancelled";
}

public static class GiftingFulfilmentStatuses
{
    public const string NotStarted = "NotStarted";
    public const string Preparing = "Preparing";
    public const string ReadyForDelivery = "ReadyForDelivery";
    public const string DispatchedOrHandedOver = "DispatchedOrHandedOver";
    public const string ReceivedAtDestination = "ReceivedAtDestination";
    public const string Distributed = "Distributed";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public static class GiftingDeliveryDestinationTypes
{
    public const string BranchAddress = "BranchAddress";
    public const string OrganizationAddress = "OrganizationAddress";
}

public static class GiftingPaymentMethods
{
    public const string Cash = "Cash";
    public const string Razorpay = "Razorpay";
    public const string GooglePay_UPI = "GooglePay_UPI";
    public const string BankTransfer = "BankTransfer";
}

public class ClassroomGiftingOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid ParentUserId { get; set; }
    public string? ParentEmail { get; set; }
    public string? ParentName { get; set; }
    public Guid? ChildId { get; set; }
    public string RecipientType { get; set; } = GiftingRecipientTypes.BirthdayGift;
    public Guid? RecipientChildId { get; set; }
    public string? RecipientChildName { get; set; }
    public string? GreetingMessage { get; set; }
    public string DeliveryDestinationType { get; set; } = GiftingDeliveryDestinationTypes.BranchAddress;
    public string ConfirmedDeliveryAddress { get; set; } = string.Empty;
    public string? ConfirmedContactName { get; set; }
    public string? ConfirmedContactPhone { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public string ApprovalStatus { get; set; } = GiftingApprovalStatuses.PendingApproval;
    public Guid? ApprovalDecidedByUserId { get; set; }
    public string? ApprovalDecidedByName { get; set; }
    public string? ApprovalDecidedByRole { get; set; }
    public DateTime? ApprovalDecidedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = GiftingPaymentStatuses.AwaitingPayment;
    public string FulfilmentStatus { get; set; } = GiftingFulfilmentStatuses.NotStarted;
    public Guid? PreparedByUserId { get; set; }
    public string? PreparedByName { get; set; }
    public DateTime? PreparedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public string? ReceivedByName { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public Guid? DistributedByUserId { get; set; }
    public string? DistributedByName { get; set; }
    public DateTime? DistributedAt { get; set; }
    public string? DistributionNotes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ClassroomGiftingOrderItem> Items { get; set; } = new();
    public List<ClassroomGiftingAuditEvent> AuditEvents { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

public class ClassroomGiftingOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid MaterialProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ClassroomGiftingAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string ActorRole { get; set; } = "System";
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class GiftingPeerChildDto
{
    public Guid ChildId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? PhotoUrl { get; set; }
    public Guid ClassroomId { get; set; }
    public bool IsBirthdayToday { get; set; }
    public bool IsBirthdayThisMonth { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class GiftingFinancialReconciliationReport
{
    public long TotalOrders { get; set; }
    public decimal TotalOrderAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalPendingAmount { get; set; }
    public decimal CashCollected { get; set; }
    public decimal RazorpayCollected { get; set; }
    public decimal UpiCollected { get; set; }
    public decimal BankCollected { get; set; }
    public long PendingApprovals { get; set; }
    public long PendingVerifications { get; set; }
    public long InPreparation { get; set; }
    public long DeliveredOrDistributed { get; set; }
}


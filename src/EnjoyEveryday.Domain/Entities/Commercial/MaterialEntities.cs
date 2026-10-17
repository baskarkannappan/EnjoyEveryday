using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public enum MaterialClassification
{
    Consumable,
    Reusable,
    ActivityKit
}

public enum MaterialOrderType
{
    StandardCatalog,
    ApprovalRequired,
    SpecialRequest
}

public static class MaterialWorkflowStatus
{
    public const string RequestSubmitted = "RequestSubmitted";
    public const string UnderReview = "UnderReview";
    public const string QuotationOffered = "QuotationOffered";
    public const string AwaitingConfirmation = "AwaitingConfirmation";
    public const string Confirmed = "Confirmed";
    public const string PaymentPending = "PaymentPending";
    public const string PaymentConfirmed = "PaymentConfirmed";
    public const string StockAllocated = "StockAllocated";
    public const string PickingPacking = "PickingPacking";
    public const string Dispatched = "Dispatched";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string ReceiptConfirmed = "ReceiptConfirmed";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
    public const string DeliveryException = "DeliveryException";
}

public static class MaterialFulfilmentMode
{
    public const string Purchase = "Purchase";
    public const string Borrow = "Borrow";
}

public static class MaterialLoanStatus
{
    public const string Reserved = "Reserved";
    public const string Dispatched = "Dispatched";
    public const string Delivered = "Delivered";
    public const string InUse = "InUse";
    public const string ReturnDue = "ReturnDue";
    public const string ReturnInTransit = "ReturnInTransit";
    public const string Received = "Received";
    public const string Inspected = "Inspected";
    public const string AvailableAgain = "AvailableAgain";
    public const string Damaged = "Damaged";
    public const string Missing = "Missing";
    public const string Retired = "Retired";
}

public static class MaterialInventoryTransactionType
{
    public const string Receipt = "Receipt";
    public const string Reservation = "Reservation";
    public const string ReservationRelease = "ReservationRelease";
    public const string DispatchPurchase = "DispatchPurchase";
    public const string DispatchLoan = "DispatchLoan";
    public const string ReturnReceived = "ReturnReceived";
    public const string InspectionApproved = "InspectionApproved";
    public const string InspectionDamaged = "InspectionDamaged";
    public const string InspectionMissing = "InspectionMissing";
    public const string CleaningComplete = "CleaningComplete";
    public const string Adjustment = "Adjustment";
    public const string Retirement = "Retirement";
}

public class MaterialProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? ImageUrl { get; set; }
    public string MaterialType { get; set; } = MaterialClassification.Consumable.ToString();
    public string? SafetyInstructions { get; set; }
    public int? AgeMinMonths { get; set; }
    public int? AgeMaxMonths { get; set; }
    public string? UsageRestrictions { get; set; }
    public string? CleaningInstructions { get; set; }
    public string? ReturnPolicy { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public MaterialOffering? Offering { get; set; }
    public InventoryBalance? StockBalance { get; set; }
}

public class MaterialOffering
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaterialProductId { get; set; }
    public bool AllowPurchase { get; set; } = true;
    public decimal PurchasePrice { get; set; }
    public bool AllowBorrow { get; set; } = false;
    public decimal RentalFee { get; set; }
    public decimal RefundableDeposit { get; set; }
    public int DefaultLoanDurationDays { get; set; } = 14;
    public int MaxLoanDurationDays { get; set; } = 30;
    public string Currency { get; set; } = "INR";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class InventoryLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsCentralWarehouse { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class InventoryBalance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaterialProductId { get; set; }
    public Guid LocationId { get; set; }
    public int TotalStock { get; set; }
    public int AvailableStock { get; set; }
    public int ReservedStock { get; set; }
    public int DispatchedStock { get; set; }
    public int OnLoanStock { get; set; }
    public int UnderInspectionStock { get; set; }
    public int UnderCleaningStock { get; set; }
    public int DamagedStock { get; set; }
    public int MissingStock { get; set; }
    public int RetiredStock { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
    public string? LocationName { get; set; }
}

public class InventoryLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaterialProductId { get; set; }
    public Guid LocationId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int QuantityDelta { get; set; }
    public int AvailableDelta { get; set; }
    public int ReservedDelta { get; set; }
    public int OnLoanDelta { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByEmail { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
}

public class MaterialOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? PlannedExperienceId { get; set; }
    public Guid RequestingUserId { get; set; }
    public string? RequestingUserEmail { get; set; }
    public string OrderType { get; set; } = "StandardCatalog"; // StandardCatalog, ApprovalRequired, SpecialRequest
    public string WorkflowStatus { get; set; } = MaterialWorkflowStatus.RequestSubmitted;
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Completed, Partial, Waived, Refunded
    public string FulfilmentStatus { get; set; } = "Unfulfilled"; // Unfulfilled, Allocated, Dispatched, Delivered, ReceiptConfirmed
    public decimal TotalPurchaseAmount { get; set; }
    public decimal TotalRentalFee { get; set; }
    public decimal TotalDeposit { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public string? QuotationNotes { get; set; }
    public DateTimeOffset? QuotationValidUntil { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? DeliveryContactName { get; set; }
    public string? DeliveryContactPhone { get; set; }
    public DateTimeOffset? RequiredDeliveryDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? OrganizationName { get; set; }
    public string? BranchName { get; set; }
    public string? ClassroomName { get; set; }
    public string? PlannedExperienceTitle { get; set; }

    public List<MaterialOrderItem> Items { get; set; } = new();
    public List<MaterialOrderHistory> History { get; set; } = new();
    public MaterialShipment? Shipment { get; set; }
    public List<MaterialLoan> Loans { get; set; } = new();
}

public class MaterialOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid MaterialProductId { get; set; }
    public Guid? OfferingId { get; set; }
    public string FulfilmentMode { get; set; } = MaterialFulfilmentMode.Purchase; // Purchase, Borrow
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal RentalFee { get; set; }
    public decimal Deposit { get; set; }
    public decimal LineTotal { get; set; }
    public int LoanDurationDays { get; set; }
    public DateTimeOffset? ReturnDueDate { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
    public string? ImageUrl { get; set; }
    public string? Category { get; set; }
}

public class MaterialOrderHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string ActorRole { get; set; } = "DaycareOrganization"; // DaycareOrganization, PlatformAdmin, System
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsInternalNote { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class MaterialShipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string ShipmentNumber { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string TrackingReference { get; set; } = string.Empty;
    public DateTimeOffset DispatchDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EstimatedDeliveryDate { get; set; }
    public DateTimeOffset? ActualDeliveryDate { get; set; }
    public string DeliveryStatus { get; set; } = "Dispatched"; // Dispatched, InTransit, Delivered, Failed, Exception
    public string? ProofOfDeliveryNotes { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class MaterialLoan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid MaterialProductId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ClassroomId { get; set; }
    public int QuantityIssued { get; set; } = 1;
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DueDate { get; set; }
    public DateTimeOffset? ActualReturnDate { get; set; }
    public string ReturnStatus { get; set; } = MaterialLoanStatus.Reserved;
    public decimal DepositAmount { get; set; }
    public string DepositStatus { get; set; } = "Held"; // Held, Refunded, Forfeited, Waived
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
    public string? OrderNumber { get; set; }
    public string? ClassroomName { get; set; }
}

public class MaterialReturnInspection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LoanId { get; set; }
    public Guid OrderId { get; set; }
    public Guid MaterialProductId { get; set; }
    public int ReturnedQuantity { get; set; }
    public DateTimeOffset InspectedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? InspectedByUserId { get; set; }
    public string? InspectedByEmail { get; set; }
    public string Condition { get; set; } = "Good"; // Excellent, Good, RequiresCleaning, Damaged, MissingParts, Unusable
    public int QuantityApprovedForStock { get; set; }
    public int QuantityRequiringCleaning { get; set; }
    public int QuantityDamaged { get; set; }
    public int QuantityMissing { get; set; }
    public string? InspectionNotes { get; set; }
    public string DepositDisposition { get; set; } = "FullRefund"; // FullRefund, PartialDeduction, FullForfeiture, NoDeposit
    public decimal DepositDeductionAmount { get; set; }
    public string ResolutionStatus { get; set; } = "Completed"; // Completed, PendingAdminReview
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
}

public class MaterialRecurringSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public string ScheduleType { get; set; } = "RecurringPurchase"; // RecurringPurchase, RecurringLoan
    public string Frequency { get; set; } = "Monthly"; // Weekly, BiWeekly, Monthly
    public Guid MaterialProductId { get; set; }
    public Guid? OfferingId { get; set; }
    public int Quantity { get; set; } = 1;
    public int LoanDurationDays { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateTimeOffset NextRunDate { get; set; }
    public DateTimeOffset? LastRunDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, Paused, Cancelled
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
}

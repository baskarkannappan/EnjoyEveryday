using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

#region Request & Response Models

public class SaveMaterialProductRequest
{
    public Guid? Id { get; set; }
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

    // Offering pricing
    public bool AllowPurchase { get; set; } = true;
    public decimal PurchasePrice { get; set; }
    public bool AllowBorrow { get; set; } = false;
    public decimal RentalFee { get; set; }
    public decimal RefundableDeposit { get; set; }
    public int DefaultLoanDurationDays { get; set; } = 14;
    public int MaxLoanDurationDays { get; set; } = 30;
}

public class AdjustStockRequest
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public string TransactionType { get; set; } = MaterialInventoryTransactionType.Adjustment;
    public int QuantityDelta { get; set; }
    public string? Notes { get; set; }
}

public class CreateMaterialOrderItemRequest
{
    public Guid ProductId { get; set; }
    public string FulfilmentMode { get; set; } = MaterialFulfilmentMode.Purchase; // Purchase, Borrow
    public int Quantity { get; set; } = 1;
    public int LoanDurationDays { get; set; } = 14;
}

public class CreateMaterialOrderRequest
{
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? PlannedExperienceId { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? DeliveryContactName { get; set; }
    public string? DeliveryContactPhone { get; set; }
    public DateTimeOffset? RequiredDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<CreateMaterialOrderItemRequest> Items { get; set; } = new();
}

public class ReviewOrderRequest
{
    public Guid OrderId { get; set; }
    public bool Approve { get; set; }
    public string? RejectionReason { get; set; }
    public string? ReviewNotes { get; set; }
}

public class IssueQuotationRequest
{
    public Guid OrderId { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public decimal TotalRentalFee { get; set; }
    public decimal TotalDeposit { get; set; }
    public decimal ShippingFee { get; set; }
    public string? QuotationNotes { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
}

public class RecordManualOrderPaymentRequest
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = PaymentMethods.Cash; // Cash, BankTransfer
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
}

public class VerifyMaterialOrderPaymentRequest
{
    public Guid OrderId { get; set; }
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
}

public class DispatchShipmentRequest
{
    public Guid OrderId { get; set; }
    public string CarrierName { get; set; } = string.Empty;
    public string TrackingReference { get; set; } = string.Empty;
    public DateTimeOffset? EstimatedDeliveryDate { get; set; }
    public string? DispatchNotes { get; set; }
}

public class ConfirmDeliveryReceiptRequest
{
    public Guid OrderId { get; set; }
    public bool IsDeliveredSuccessfully { get; set; } = true;
    public string? Notes { get; set; }
}

public class CompleteReturnInspectionRequest
{
    public Guid LoanId { get; set; }
    public int ReturnedQuantity { get; set; }
    public string Condition { get; set; } = "Good"; // Excellent, Good, RequiresCleaning, Damaged, MissingParts, Unusable
    public int QuantityApprovedForStock { get; set; }
    public int QuantityRequiringCleaning { get; set; }
    public int QuantityDamaged { get; set; }
    public int QuantityMissing { get; set; }
    public string? InspectionNotes { get; set; }
    public string DepositDisposition { get; set; } = "FullRefund"; // FullRefund, PartialDeduction, FullForfeiture, NoDeposit
    public decimal DepositDeductionAmount { get; set; }
}

public class CreateRecurringScheduleRequest
{
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public string ScheduleType { get; set; } = "RecurringPurchase"; // RecurringPurchase, RecurringLoan
    public string Frequency { get; set; } = "Monthly"; // Weekly, BiWeekly, Monthly
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public int LoanDurationDays { get; set; } = 14;
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateTimeOffset FirstDeliveryDate { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
    public string? Notes { get; set; }
}

public class MaterialReadinessItem
{
    public string MaterialName { get; set; } = string.Empty;
    public bool IsAvailableInStock { get; set; }
    public bool IsOrdered { get; set; }
    public string? OrderNumber { get; set; }
    public string? FulfilmentStatus { get; set; }
    public Guid? MatchedProductId { get; set; }
    public bool CanPurchase { get; set; }
    public decimal PurchasePrice { get; set; }
    public bool CanBorrow { get; set; }
    public decimal RentalFee { get; set; }
    public string SuggestedAction { get; set; } = "Available"; // Available, Buy, Borrow, AwaitingDelivery
}

public class ExperienceMaterialReadinessSummary
{
    public Guid ExperienceId { get; set; }
    public string ExperienceTitle { get; set; } = string.Empty;
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public List<MaterialReadinessItem> Materials { get; set; } = new();
    public bool IsFullyReady { get; set; }
    public string OverallStatusBadge { get; set; } = "Ready"; // Ready, Dispatched, Requested, MissingMaterials
    public int TotalRequired { get; set; }
    public int TotalOnHandOrDelivered { get; set; }
}

#endregion

public interface IMaterialMarketplaceService
{
    // Catalog & Warehouse
    Task<List<MaterialProduct>> GetCatalogProductsAsync(string? category = null, string? materialType = null, bool activeOnly = true, CancellationToken ct = default);
    Task<MaterialProduct?> GetProductDetailsAsync(Guid productId, CancellationToken ct = default);
    Task<Guid> SaveProductAsync(SaveMaterialProductRequest request, CancellationToken ct = default);
    Task<List<InventoryBalance>> GetWarehouseInventoryBalancesAsync(Guid? locationId = null, CancellationToken ct = default);
    Task<List<InventoryLocation>> GetWarehouseLocationsAsync(CancellationToken ct = default);
    Task<List<InventoryLedgerEntry>> GetProductLedgerHistoryAsync(Guid productId, CancellationToken ct = default);
    Task AdjustInventoryStockAsync(AdjustStockRequest request, CancellationToken ct = default);

    // Orders & Shared Workflow
    Task<MaterialOrder> SubmitMaterialOrderAsync(CreateMaterialOrderRequest request, CancellationToken ct = default);
    Task<MaterialOrder?> GetOrderDetailsAsync(Guid orderId, CancellationToken ct = default);
    Task<List<MaterialOrder>> GetOrdersForOrganizationAsync(Guid organizationId, string? statusFilter = null, CancellationToken ct = default);
    Task<List<MaterialOrder>> GetPlatformOrderQueueAsync(string? statusFilter = null, CancellationToken ct = default);
    Task<List<MaterialOrderHistory>> GetOrderTimelineAsync(Guid orderId, CancellationToken ct = default);
    Task ReviewMaterialOrderAsync(ReviewOrderRequest request, CancellationToken ct = default);
    Task ApproveAndConfirmMaterialOrderAsync(Guid orderId, CancellationToken ct = default);
    Task IssueOrderQuotationAsync(IssueQuotationRequest request, CancellationToken ct = default);
    Task AcceptOrderQuotationAsync(Guid orderId, CancellationToken ct = default);
    Task CancelOrderAsync(Guid orderId, string reason, CancellationToken ct = default);

    // Payments
    Task<RazorpayOrderResponse> CreateMaterialRazorpayOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<bool> VerifyMaterialRazorpayPaymentAsync(VerifyMaterialOrderPaymentRequest request, CancellationToken ct = default);
    Task RecordManualMaterialPaymentAsync(RecordManualOrderPaymentRequest request, CancellationToken ct = default);

    // Fulfilment & Shipments
    Task AllocateStockAndPrepareOrderAsync(Guid orderId, CancellationToken ct = default);
    Task DispatchOrderShipmentAsync(DispatchShipmentRequest request, CancellationToken ct = default);
    Task ConfirmDeliveryReceiptAsync(ConfirmDeliveryReceiptRequest request, CancellationToken ct = default);

    // Loans & Returns
    Task<List<MaterialLoan>> GetOrganizationLoansAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<MaterialLoan>> GetPlatformLoanQueueAsync(string? statusFilter = null, CancellationToken ct = default);
    Task InitiateLoanReturnAsync(Guid loanId, string? notes = null, CancellationToken ct = default);
    Task ReceiveLoanReturnAtWarehouseAsync(Guid loanId, int receivedQuantity, string? notes = null, CancellationToken ct = default);
    Task CompleteReturnInspectionAsync(CompleteReturnInspectionRequest request, CancellationToken ct = default);
    Task SanitizeAndRestockCleaningItemsAsync(Guid productId, int quantity, CancellationToken ct = default);

    // Recurring Orders
    Task<Guid> CreateRecurringScheduleAsync(CreateRecurringScheduleRequest request, CancellationToken ct = default);
    Task<List<MaterialRecurringSchedule>> GetRecurringSchedulesAsync(Guid organizationId, CancellationToken ct = default);
    Task UpdateRecurringScheduleStatusAsync(Guid scheduleId, string newStatus, CancellationToken ct = default);
    Task<int> ProcessDueRecurringSchedulesJobAsync(DateTimeOffset asOfDate, CancellationToken ct = default);

    // Experience Planning Integration
    Task<ExperienceMaterialReadinessSummary> CheckExperienceMaterialReadinessAsync(Guid experienceId, Guid classroomId, CancellationToken ct = default);
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public class SubmitGiftingOrderRequest
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid? ChildId { get; set; }
    public string RecipientType { get; set; } = GiftingRecipientTypes.BirthdayGift;
    public Guid? RecipientChildId { get; set; }
    public string? RecipientChildName { get; set; }
    public string? GreetingMessage { get; set; }
    public string DeliveryDestinationType { get; set; } = GiftingDeliveryDestinationTypes.BranchAddress;
    public string ConfirmedDeliveryAddress { get; set; } = string.Empty;
    public string? ConfirmedContactName { get; set; }
    public string? ConfirmedContactPhone { get; set; }
    public List<SubmitGiftingOrderItemDto> Items { get; set; } = new();
}

public class SubmitGiftingOrderItemDto
{
    public Guid MaterialProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class GiftingDeliveryDestinationOptions
{
    public string BranchName { get; set; } = string.Empty;
    public string BranchFullAddress { get; set; } = string.Empty;
    public string BranchContactPhone { get; set; } = string.Empty;

    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationFullAddress { get; set; } = string.Empty;
    public string OrganizationContactPhone { get; set; } = string.Empty;
}

public class GiftingPaymentMethodsConfig
{
    public bool HasRazorpay { get; set; }
    public string? RazorpayKeyId { get; set; }
    public string? RazorpayEnvironment { get; set; }

    public bool HasActiveQrCode { get; set; }
    public string? QrDisplayName { get; set; }
    public string? QrImageDataUri { get; set; }
    public string? QrInstructions { get; set; }

    public bool HasBankAccount { get; set; }
    public string? AccountHolderName { get; set; }
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IfscCode { get; set; }
    public string? BankInstructions { get; set; }

    public bool AllowCash { get; set; } = true;
}

public class GiftingRazorpayOrderResult
{
    public string OrderId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long AmountInPaise { get; set; }
    public string Currency { get; set; } = "INR";
    public string GiftingOrderNumber { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string Environment { get; set; } = "Test";
    public string PrefillEmail { get; set; } = string.Empty;
}

public class ConfirmGiftingRazorpayRequest
{
    public Guid OrderId { get; set; }
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
}

public class SubmitGiftingManualPaymentRequest
{
    public Guid OrderId { get; set; }
    public string Method { get; set; } = GiftingPaymentMethods.GooglePay_UPI;
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
    public string? ProofStorageKey { get; set; }
    public string? Remarks { get; set; }
}

public class RecordCashReceivedRequest
{
    public Guid OrderId { get; set; }
    public decimal AmountReceived { get; set; }
    public string? Notes { get; set; }
}

public interface IClassroomGiftingService
{
    Task<List<GiftingPeerChildDto>> GetClassroomPeersForGiftingAsync(
        Guid classroomId,
        Guid? excludeChildId = null,
        CancellationToken ct = default);

    Task<List<MaterialProduct>> GetGiftingCatalogProductsAsync(
        string? category = null,
        CancellationToken ct = default);

    Task<GiftingDeliveryDestinationOptions> GetDeliveryDestinationsAsync(
        Guid branchId,
        Guid organizationId,
        CancellationToken ct = default);

    Task<ClassroomGiftingOrder> SubmitOrderAsync(
        SubmitGiftingOrderRequest request,
        CancellationToken ct = default);

    Task<(bool Success, string Message)> ApproveOrRejectOrderAsync(
        Guid orderId,
        string decision,
        string? reason = null,
        CancellationToken ct = default);

    Task<GiftingPaymentMethodsConfig> GetPaymentMethodsConfigAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<GiftingRazorpayOrderResult> CreateRazorpayOrderAsync(
        Guid orderId,
        CancellationToken ct = default);

    Task<(bool Success, string Message)> ConfirmRazorpayPaymentAsync(
        ConfirmGiftingRazorpayRequest request,
        CancellationToken ct = default);

    Task<(bool Success, string Message)> SubmitManualPaymentAsync(
        SubmitGiftingManualPaymentRequest request,
        CancellationToken ct = default);

    Task<(bool Success, string Message, string ReceiptNumber)> RecordCashPaymentReceivedAsync(
        RecordCashReceivedRequest request,
        CancellationToken ct = default);

    Task<(bool Success, string Message)> VerifyManualPaymentAsync(
        Guid paymentId,
        string? remarks = null,
        CancellationToken ct = default);

    Task<(bool Success, string Message)> RejectManualPaymentAsync(
        Guid paymentId,
        string reason,
        CancellationToken ct = default);

    Task<(bool Success, string Message)> UpdateFulfilmentStatusAsync(
        Guid orderId,
        string targetStatus,
        string? notes = null,
        CancellationToken ct = default);

    Task<List<ClassroomGiftingOrder>> GetOrdersForAdminAsync(
        Guid organizationId,
        Guid? branchId = null,
        Guid? classroomId = null,
        string? statusFilter = null,
        CancellationToken ct = default);

    Task<List<ClassroomGiftingOrder>> GetOrdersForParentAsync(
        CancellationToken ct = default);

    Task<ClassroomGiftingOrder?> GetOrderDetailsAsync(
        Guid orderId,
        CancellationToken ct = default);

    Task<GiftingFinancialReconciliationReport> GetFinancialReconciliationAsync(
        Guid organizationId,
        Guid? branchId = null,
        CancellationToken ct = default);
}


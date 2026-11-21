using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IClassroomGiftingRepository
{
    Task<List<GiftingPeerChildDto>> GetClassroomPeersForGiftingAsync(
        Guid tenantId,
        Guid classroomId,
        Guid? excludeChildId = null,
        CancellationToken ct = default);

    Task<(bool Success, string Message, Guid OrderId, string OrderNumber)> SubmitOrderAsync(
        ClassroomGiftingOrder order,
        List<ClassroomGiftingOrderItem> items,
        CancellationToken ct = default);

    Task<(bool Success, string Message, Guid OrderId, string OrderNumber, string ApprovalStatus, string? DecidedByName, DateTime? DecidedAt)> ApproveOrRejectOrderAsync(
        Guid orderId,
        Guid actorUserId,
        string actorName,
        string actorRole,
        string decision,
        string? reason = null,
        CancellationToken ct = default);

    Task<(bool Success, string Message, Guid PaymentId, string ReceiptNumber)> RecordCashPaymentAsync(
        Guid paymentId,
        Guid orderId,
        decimal amountReceived,
        Guid staffUserId,
        string staffEmail,
        string staffName,
        string receiptNumber,
        string? notes = null,
        CancellationToken ct = default);

    Task<(bool Success, string Message, Guid? OrderId, Guid PaymentId)> VerifyPaymentAsync(
        Guid paymentId,
        Guid verifierUserId,
        string verifierName,
        string verifierRole,
        string? remarks = null,
        CancellationToken ct = default);

    Task<(bool Success, string Message, Guid? OrderId, Guid PaymentId)> RejectPaymentAsync(
        Guid paymentId,
        Guid actorUserId,
        string actorName,
        string actorRole,
        string reason,
        CancellationToken ct = default);

    Task<(bool Success, string Message, Guid OrderId, string FulfilmentStatus)> UpdateFulfilmentStatusAsync(
        Guid orderId,
        string targetStatus,
        Guid actorUserId,
        string actorName,
        string actorRole,
        string? notes = null,
        CancellationToken ct = default);

    Task<List<ClassroomGiftingOrder>> GetOrdersForAdminAsync(
        Guid tenantId,
        Guid organizationId,
        Guid? branchId = null,
        Guid? classroomId = null,
        string? statusFilter = null,
        CancellationToken ct = default);

    Task<List<ClassroomGiftingOrder>> GetOrdersForParentAsync(
        Guid tenantId,
        Guid parentUserId,
        CancellationToken ct = default);

    Task<ClassroomGiftingOrder?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);

    Task<List<ClassroomGiftingOrderItem>> GetOrderItemsAsync(Guid orderId, CancellationToken ct = default);

    Task<List<ClassroomGiftingAuditEvent>> GetAuditEventsAsync(Guid orderId, CancellationToken ct = default);

    Task<List<Payment>> GetPaymentsForOrderAsync(Guid orderId, CancellationToken ct = default);

    Task<GiftingFinancialReconciliationReport> GetFinancialReconciliationAsync(
        Guid tenantId,
        Guid organizationId,
        Guid? branchId = null,
        CancellationToken ct = default);

    Task<(bool Success, string Message, Guid PaymentId)> RecordOnlineRazorpayPaymentAsync(
        Payment payment,
        Guid orderId,
        CancellationToken ct = default);

    Task<Guid> SubmitManualPaymentDeclarationAsync(
        Payment payment,
        Guid orderId,
        CancellationToken ct = default);
}


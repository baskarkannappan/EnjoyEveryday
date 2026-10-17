using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IMaterialMarketplaceRepository
{
    // Product Catalog
    Task<List<MaterialProduct>> GetProductsAsync(string? category = null, string? materialType = null, bool activeOnly = true, CancellationToken ct = default);
    Task<MaterialProduct?> GetProductByIdAsync(Guid productId, CancellationToken ct = default);
    Task<MaterialProduct?> GetProductBySkuAsync(string sku, CancellationToken ct = default);
    Task<Guid> CreateProductAsync(MaterialProduct product, MaterialOffering offering, CancellationToken ct = default);
    Task UpdateProductAsync(MaterialProduct product, MaterialOffering offering, CancellationToken ct = default);

    // Locations & Warehouse Inventory
    Task<List<InventoryLocation>> GetLocationsAsync(CancellationToken ct = default);
    Task<InventoryLocation?> GetCentralWarehouseLocationAsync(CancellationToken ct = default);
    Task<List<InventoryBalance>> GetInventoryBalancesAsync(Guid? locationId = null, CancellationToken ct = default);
    Task<InventoryBalance?> GetInventoryBalanceAsync(Guid productId, Guid locationId, CancellationToken ct = default);
    Task<List<InventoryLedgerEntry>> GetProductLedgerAsync(Guid productId, int limit = 50, CancellationToken ct = default);
    Task RecordInventoryLedgerAsync(InventoryLedgerEntry entry, CancellationToken ct = default);
    Task UpdateInventoryBalanceAtomicAsync(
        Guid productId,
        Guid locationId,
        int totalDelta,
        int availableDelta,
        int reservedDelta,
        int dispatchedDelta,
        int onLoanDelta,
        int underInspectionDelta,
        int underCleaningDelta,
        int damagedDelta,
        int missingDelta,
        int retiredDelta,
        CancellationToken ct = default);

    // Orders & Shared Workflow
    Task<Guid> CreateOrderAsync(MaterialOrder order, List<MaterialOrderItem> items, MaterialOrderHistory initialHistory, CancellationToken ct = default);
    Task<MaterialOrder?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<MaterialOrder?> GetOrderByNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<List<MaterialOrder>> GetOrdersForOrganizationAsync(Guid organizationId, string? statusFilter = null, CancellationToken ct = default);
    Task<List<MaterialOrder>> GetPlatformOrderQueueAsync(string? statusFilter = null, CancellationToken ct = default);
    Task UpdateOrderStatusAsync(Guid orderId, string newStatus, string? paymentStatus = null, string? fulfilmentStatus = null, string? rejectionReason = null, string? cancellationReason = null, CancellationToken ct = default);
    Task UpdateOrderQuotationAsync(Guid orderId, decimal totalPurchaseAmount, decimal totalRentalFee, decimal totalDeposit, decimal shippingFee, decimal totalAmount, string? quotationNotes, DateTimeOffset? validUntil, CancellationToken ct = default);
    Task AddOrderHistoryEventAsync(MaterialOrderHistory history, CancellationToken ct = default);
    Task<List<MaterialOrderHistory>> GetOrderHistoryAsync(Guid orderId, bool includeInternalNotes = false, CancellationToken ct = default);

    // Shipments
    Task<Guid> CreateShipmentAsync(MaterialShipment shipment, CancellationToken ct = default);
    Task<MaterialShipment?> GetShipmentByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task UpdateShipmentStatusAsync(Guid shipmentId, string deliveryStatus, DateTimeOffset? actualDeliveryDate, string? proofNotes, CancellationToken ct = default);

    // Reusable Loans & Return Inspections
    Task CreateLoansAsync(List<MaterialLoan> loans, CancellationToken ct = default);
    Task<List<MaterialLoan>> GetLoansByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<List<MaterialLoan>> GetActiveLoansForOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<MaterialLoan>> GetPlatformLoanQueueAsync(string? statusFilter = null, CancellationToken ct = default);
    Task<MaterialLoan?> GetLoanByIdAsync(Guid loanId, CancellationToken ct = default);
    Task UpdateLoanStatusAsync(Guid loanId, string returnStatus, DateTimeOffset? actualReturnDate = null, string? notes = null, string? depositStatus = null, CancellationToken ct = default);
    Task<Guid> RecordReturnInspectionAsync(MaterialReturnInspection inspection, CancellationToken ct = default);
    Task<MaterialReturnInspection?> GetInspectionByLoanIdAsync(Guid loanId, CancellationToken ct = default);

    // Recurring Schedules
    Task<Guid> CreateRecurringScheduleAsync(MaterialRecurringSchedule schedule, CancellationToken ct = default);
    Task<List<MaterialRecurringSchedule>> GetRecurringSchedulesAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<MaterialRecurringSchedule>> GetDueRecurringSchedulesAsync(DateTimeOffset asOfDate, CancellationToken ct = default);
    Task UpdateRecurringScheduleStatusAsync(Guid scheduleId, string status, DateTimeOffset? nextRunDate, DateTimeOffset? lastRunDate, CancellationToken ct = default);
}

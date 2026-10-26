using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class MaterialMarketplaceRepository : IMaterialMarketplaceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<MaterialMarketplaceRepository> _logger;

    public MaterialMarketplaceRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<MaterialMarketplaceRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    #region Catalog & Offerings

    public async Task<List<MaterialProduct>> GetProductsAsync(string? category = null, string? materialType = null, bool activeOnly = true, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<MaterialProductRow>(
            "fn_get_material_products",
            new { Category = category, MaterialType = materialType, ActiveOnly = activeOnly },
            cancellationToken: ct);

        return rows.Select(MapProductRow).ToList();
    }

    public async Task<MaterialProduct?> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var r = await conn.QuerySingleFunctionAsync<MaterialProductRow>(
            "fn_get_material_product_by_id",
            new { ProductId = productId },
            cancellationToken: ct);

        return r != null ? MapProductRow(r) : null;
    }

    public async Task<MaterialProduct?> GetProductBySkuAsync(string sku, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var r = await conn.QuerySingleFunctionAsync<MaterialProductRow>(
            "fn_get_material_product_by_sku",
            new { Sku = sku },
            cancellationToken: ct);

        return r != null ? MapProductRow(r) : null;
    }

    public async Task<Guid> CreateProductAsync(MaterialProduct product, MaterialOffering offering, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (product.Id == Guid.Empty) product.Id = Guid.NewGuid();
        if (offering.Id == Guid.Empty) offering.Id = Guid.NewGuid();
        offering.MaterialProductId = product.Id;

        await conn.ExecuteProcedureAsync("sp_create_material_product", product, cancellationToken: ct);
        await conn.ExecuteProcedureAsync("sp_create_material_offering", offering, cancellationToken: ct);

        return product.Id;
    }

    public async Task UpdateProductAsync(MaterialProduct product, MaterialOffering offering, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        product.UpdatedAt = DateTimeOffset.UtcNow;
        offering.UpdatedAt = DateTimeOffset.UtcNow;

        await conn.ExecuteProcedureAsync("sp_update_material_product", product, cancellationToken: ct);
        await conn.ExecuteProcedureAsync("sp_update_material_offering", new
        {
            Id = product.Id,
            offering.AllowPurchase,
            offering.PurchasePrice,
            offering.AllowBorrow,
            offering.RentalFee,
            offering.RefundableDeposit,
            offering.DefaultLoanDurationDays,
            offering.MaxLoanDurationDays,
            offering.IsActive,
            offering.UpdatedAt
        }, cancellationToken: ct);
    }

    #endregion

    #region Inventory & Warehouse

    public async Task<List<InventoryLocation>> GetLocationsAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<InventoryLocation>("fn_get_inventory_locations", cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<InventoryLocation?> GetCentralWarehouseLocationAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<InventoryLocation>("fn_get_central_warehouse_location", cancellationToken: ct);
    }

    public async Task<List<InventoryBalance>> GetInventoryBalancesAsync(Guid? locationId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<InventoryBalance>(
            "fn_get_inventory_balances",
            new { LocationId = locationId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<InventoryBalance?> GetInventoryBalanceAsync(Guid productId, Guid locationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<InventoryBalance>(
            "fn_get_inventory_balance",
            new { ProductId = productId, LocationId = locationId },
            cancellationToken: ct);
    }

    public async Task<List<InventoryLedgerEntry>> GetProductLedgerAsync(Guid productId, int limit = 50, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<InventoryLedgerEntry>(
            "fn_get_product_ledger",
            new { ProductId = productId, Limit = limit },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task RecordInventoryLedgerAsync(InventoryLedgerEntry entry, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (entry.Id == Guid.Empty) entry.Id = Guid.NewGuid();
        entry.CreatedAt = entry.CreatedAt.ToUniversalTime();

        await conn.ExecuteProcedureAsync("sp_record_inventory_ledger", entry, cancellationToken: ct);
    }

    public async Task UpdateInventoryBalanceAtomicAsync(
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
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync("sp_update_inventory_balance_atomic", new
        {
            ProductId = productId,
            LocationId = locationId,
            TotalDelta = totalDelta,
            AvailableDelta = availableDelta,
            ReservedDelta = reservedDelta,
            DispatchedDelta = dispatchedDelta,
            OnLoanDelta = onLoanDelta,
            UnderInspectionDelta = underInspectionDelta,
            UnderCleaningDelta = underCleaningDelta,
            DamagedDelta = damagedDelta,
            MissingDelta = missingDelta,
            RetiredDelta = retiredDelta
        }, cancellationToken: ct);
    }

    #endregion

    #region Orders & History

    public async Task<Guid> CreateOrderAsync(MaterialOrder order, List<MaterialOrderItem> items, MaterialOrderHistory initialHistory, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (order.Id == Guid.Empty) order.Id = Guid.NewGuid();

        order.CreatedAt = order.CreatedAt.ToUniversalTime();
        order.UpdatedAt = order.UpdatedAt.ToUniversalTime();
        if (order.RequiredDeliveryDate.HasValue) order.RequiredDeliveryDate = order.RequiredDeliveryDate.Value.ToUniversalTime();
        if (order.QuotationValidUntil.HasValue) order.QuotationValidUntil = order.QuotationValidUntil.Value.ToUniversalTime();

        await conn.ExecuteProcedureAsync("sp_create_material_order", order, cancellationToken: ct);

        foreach (var item in items)
        {
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
            item.OrderId = order.Id;
            item.CreatedAt = item.CreatedAt.ToUniversalTime();
            if (item.ReturnDueDate.HasValue) item.ReturnDueDate = item.ReturnDueDate.Value.ToUniversalTime();

            await conn.ExecuteProcedureAsync("sp_create_material_order_item", item, cancellationToken: ct);
        }

        if (initialHistory.Id == Guid.Empty) initialHistory.Id = Guid.NewGuid();
        initialHistory.OrderId = order.Id;

        await AddOrderHistoryEventAsync(initialHistory, ct);

        return order.Id;
    }

    public async Task<MaterialOrder?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var order = await conn.QuerySingleFunctionAsync<MaterialOrder>(
            "fn_get_material_order_by_id",
            new { OrderId = orderId },
            cancellationToken: ct);

        if (order == null) return null;

        order.Items = await GetItemsForOrderInternalAsync(conn, order.Id);
        order.History = await GetHistoryForOrderInternalAsync(conn, order.Id, true);
        order.Shipment = await GetShipmentByOrderIdAsync(order.Id, ct);
        order.Loans = await GetLoansByOrderIdAsync(order.Id, ct);

        return order;
    }

    public async Task<MaterialOrder?> GetOrderByNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var order = await conn.QuerySingleFunctionAsync<MaterialOrder>(
            "fn_get_material_order_by_number",
            new { OrderNumber = orderNumber },
            cancellationToken: ct);

        if (order == null) return null;

        order.Items = await GetItemsForOrderInternalAsync(conn, order.Id);
        order.History = await GetHistoryForOrderInternalAsync(conn, order.Id, true);
        order.Shipment = await GetShipmentByOrderIdAsync(order.Id, ct);
        order.Loans = await GetLoansByOrderIdAsync(order.Id, ct);

        return order;
    }

    public async Task<List<MaterialOrder>> GetOrdersForOrganizationAsync(Guid organizationId, string? statusFilter = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var orders = (await conn.QueryFunctionAsync<MaterialOrder>(
            "fn_get_material_orders_for_organization",
            new { OrganizationId = organizationId, Status = statusFilter },
            cancellationToken: ct)).ToList();

        foreach (var ord in orders)
        {
            ord.Items = await GetItemsForOrderInternalAsync(conn, ord.Id);
        }

        return orders;
    }

    public async Task<List<MaterialOrder>> GetPlatformOrderQueueAsync(string? statusFilter = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var orders = (await conn.QueryFunctionAsync<MaterialOrder>(
            "fn_get_platform_order_queue",
            new { Status = statusFilter },
            cancellationToken: ct)).ToList();

        foreach (var ord in orders)
        {
            ord.Items = await GetItemsForOrderInternalAsync(conn, ord.Id);
        }

        return orders;
    }

    public async Task UpdateOrderStatusAsync(Guid orderId, string newStatus, string? paymentStatus = null, string? fulfilmentStatus = null, string? rejectionReason = null, string? cancellationReason = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync("sp_update_material_order_status", new
        {
            OrderId = orderId,
            NewStatus = newStatus,
            PaymentStatus = paymentStatus,
            FulfilmentStatus = fulfilmentStatus,
            RejectionReason = rejectionReason,
            CancellationReason = cancellationReason
        }, cancellationToken: ct);
    }

    public async Task UpdateOrderQuotationAsync(Guid orderId, decimal totalPurchaseAmount, decimal totalRentalFee, decimal totalDeposit, decimal shippingFee, decimal totalAmount, string? quotationNotes, DateTimeOffset? validUntil, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync("sp_update_material_order_quotation", new
        {
            OrderId = orderId,
            TotalPurchaseAmount = totalPurchaseAmount,
            TotalRentalFee = totalRentalFee,
            TotalDeposit = totalDeposit,
            ShippingFee = shippingFee,
            TotalAmount = totalAmount,
            QuotationNotes = quotationNotes,
            ValidUntil = validUntil
        }, cancellationToken: ct);
    }

    public async Task AddOrderHistoryEventAsync(MaterialOrderHistory history, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (history.Id == Guid.Empty) history.Id = Guid.NewGuid();
        history.CreatedAt = history.CreatedAt.ToUniversalTime();

        await conn.ExecuteProcedureAsync("sp_add_material_order_history", history, cancellationToken: ct);
    }

    public async Task<List<MaterialOrderHistory>> GetOrderHistoryAsync(Guid orderId, bool includeInternalNotes = false, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await GetHistoryForOrderInternalAsync(conn, orderId, includeInternalNotes);
    }

    private static async Task<List<MaterialOrderItem>> GetItemsForOrderInternalAsync(IDbConnection conn, Guid orderId)
    {
        var rows = await conn.QueryFunctionAsync<MaterialOrderItem>(
            "fn_get_material_order_items",
            new { OrderId = orderId });

        return rows.ToList();
    }

    private static async Task<List<MaterialOrderHistory>> GetHistoryForOrderInternalAsync(IDbConnection conn, Guid orderId, bool includeInternalNotes)
    {
        var rows = await conn.QueryFunctionAsync<MaterialOrderHistory>(
            "fn_get_material_order_history",
            new { OrderId = orderId, IncludeInternalNotes = includeInternalNotes });

        return rows.ToList();
    }

    #endregion

    #region Shipments

    public async Task<Guid> CreateShipmentAsync(MaterialShipment shipment, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (shipment.Id == Guid.Empty) shipment.Id = Guid.NewGuid();
        shipment.CreatedAt = shipment.CreatedAt.ToUniversalTime();
        shipment.UpdatedAt = shipment.UpdatedAt.ToUniversalTime();
        shipment.DispatchDate = shipment.DispatchDate.ToUniversalTime();
        if (shipment.EstimatedDeliveryDate.HasValue) shipment.EstimatedDeliveryDate = shipment.EstimatedDeliveryDate.Value.ToUniversalTime();
        if (shipment.ActualDeliveryDate.HasValue) shipment.ActualDeliveryDate = shipment.ActualDeliveryDate.Value.ToUniversalTime();

        await conn.ExecuteProcedureAsync("sp_create_material_shipment", shipment, cancellationToken: ct);
        return shipment.Id;
    }

    public async Task<MaterialShipment?> GetShipmentByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<MaterialShipment>(
            "fn_get_material_shipment_by_order_id",
            new { OrderId = orderId },
            cancellationToken: ct);
    }

    public async Task UpdateShipmentStatusAsync(Guid shipmentId, string deliveryStatus, DateTimeOffset? actualDeliveryDate, string? proofNotes, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync("sp_update_material_shipment_status", new
        {
            ShipmentId = shipmentId,
            DeliveryStatus = deliveryStatus,
            ActualDeliveryDate = actualDeliveryDate?.ToUniversalTime(),
            ProofNotes = proofNotes
        }, cancellationToken: ct);
    }

    #endregion

    #region Loans & Returns

    public async Task CreateLoansAsync(List<MaterialLoan> loans, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        foreach (var loan in loans)
        {
            if (loan.Id == Guid.Empty) loan.Id = Guid.NewGuid();
            loan.CreatedAt = loan.CreatedAt.ToUniversalTime();
            loan.UpdatedAt = loan.UpdatedAt.ToUniversalTime();
            loan.IssuedAt = loan.IssuedAt.ToUniversalTime();
            loan.DueDate = loan.DueDate.ToUniversalTime();
            if (loan.ActualReturnDate.HasValue) loan.ActualReturnDate = loan.ActualReturnDate.Value.ToUniversalTime();

            await conn.ExecuteProcedureAsync("sp_create_material_loan", loan, cancellationToken: ct);
        }
    }

    public async Task<List<MaterialLoan>> GetLoansByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<MaterialLoan>(
            "fn_get_material_loans_by_order_id",
            new { OrderId = orderId },
            cancellationToken: ct);

        return rows.ToList();
    }

    public async Task<List<MaterialLoan>> GetActiveLoansForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<MaterialLoan>(
            "fn_get_active_material_loans_for_organization",
            new { OrganizationId = organizationId },
            cancellationToken: ct);

        return rows.ToList();
    }

    public async Task<List<MaterialLoan>> GetPlatformLoanQueueAsync(string? statusFilter = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<MaterialLoan>(
            "fn_get_platform_loan_queue",
            new { Status = statusFilter },
            cancellationToken: ct);

        return rows.ToList();
    }

    public async Task<MaterialLoan?> GetLoanByIdAsync(Guid loanId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<MaterialLoan>(
            "fn_get_material_loan_by_id",
            new { LoanId = loanId },
            cancellationToken: ct);
    }

    public async Task UpdateLoanStatusAsync(Guid loanId, string returnStatus, DateTimeOffset? actualReturnDate = null, string? notes = null, string? depositStatus = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync("sp_update_material_loan_status", new
        {
            LoanId = loanId,
            ReturnStatus = returnStatus,
            ActualReturnDate = actualReturnDate,
            Notes = notes,
            DepositStatus = depositStatus
        }, cancellationToken: ct);
    }

    public async Task<Guid> RecordReturnInspectionAsync(MaterialReturnInspection inspection, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (inspection.Id == Guid.Empty) inspection.Id = Guid.NewGuid();
        inspection.CreatedAt = inspection.CreatedAt.ToUniversalTime();
        inspection.InspectedAt = inspection.InspectedAt.ToUniversalTime();

        await conn.ExecuteProcedureAsync("sp_record_material_return_inspection", inspection, cancellationToken: ct);
        return inspection.Id;
    }

    public async Task<MaterialReturnInspection?> GetInspectionByLoanIdAsync(Guid loanId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<MaterialReturnInspection>(
            "fn_get_material_return_inspection_by_loan_id",
            new { LoanId = loanId },
            cancellationToken: ct);
    }

    #endregion

    #region Recurring Schedules

    public async Task<Guid> CreateRecurringScheduleAsync(MaterialRecurringSchedule schedule, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (schedule.Id == Guid.Empty) schedule.Id = Guid.NewGuid();
        schedule.CreatedAt = schedule.CreatedAt.ToUniversalTime();
        schedule.UpdatedAt = schedule.UpdatedAt.ToUniversalTime();
        schedule.NextRunDate = schedule.NextRunDate.ToUniversalTime();
        if (schedule.LastRunDate.HasValue) schedule.LastRunDate = schedule.LastRunDate.Value.ToUniversalTime();

        await conn.ExecuteProcedureAsync("sp_create_material_recurring_schedule", schedule, cancellationToken: ct);
        return schedule.Id;
    }

    public async Task<List<MaterialRecurringSchedule>> GetRecurringSchedulesAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<MaterialRecurringSchedule>(
            "fn_get_material_recurring_schedules",
            new { OrganizationId = organizationId },
            cancellationToken: ct);

        return rows.ToList();
    }

    public async Task<List<MaterialRecurringSchedule>> GetDueRecurringSchedulesAsync(DateTimeOffset asOfDate, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<MaterialRecurringSchedule>(
            "fn_get_due_material_recurring_schedules",
            new { AsOfDate = asOfDate.ToUniversalTime() },
            cancellationToken: ct);

        return rows.ToList();
    }

    public async Task UpdateRecurringScheduleStatusAsync(Guid scheduleId, string status, DateTimeOffset? nextRunDate, DateTimeOffset? lastRunDate, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync("sp_update_material_recurring_schedule_status", new
        {
            ScheduleId = scheduleId,
            Status = status,
            NextRunDate = nextRunDate?.ToUniversalTime(),
            LastRunDate = lastRunDate?.ToUniversalTime()
        }, cancellationToken: ct);
    }

    #endregion

    private static MaterialProduct MapProductRow(MaterialProductRow r)
    {
        var prod = new MaterialProduct
        {
            Id = r.id,
            Sku = r.sku,
            Name = r.name,
            Description = r.description ?? string.Empty,
            Category = r.category,
            ImageUrl = r.image_url,
            MaterialType = r.material_type,
            SafetyInstructions = r.safety_instructions,
            AgeMinMonths = r.age_min_months,
            AgeMaxMonths = r.age_max_months,
            UsageRestrictions = r.usage_restrictions,
            CleaningInstructions = r.cleaning_instructions,
            ReturnPolicy = r.return_policy,
            IsActive = r.is_active,
            CreatedAt = r.created_at,
            UpdatedAt = r.updated_at
        };

        if (r.off_id != null)
        {
            prod.Offering = new MaterialOffering
            {
                Id = r.off_id.Value,
                MaterialProductId = r.off_prod_id ?? prod.Id,
                AllowPurchase = r.allow_purchase ?? false,
                PurchasePrice = r.purchase_price ?? 0,
                AllowBorrow = r.allow_borrow ?? false,
                RentalFee = r.rental_fee ?? 0,
                RefundableDeposit = r.refundable_deposit ?? 0,
                DefaultLoanDurationDays = r.default_loan_duration_days ?? 7,
                MaxLoanDurationDays = r.max_loan_duration_days ?? 30,
                Currency = r.currency ?? "INR",
                IsActive = r.off_is_active ?? true
            };
        }

        if (r.bal_id != null)
        {
            prod.StockBalance = new InventoryBalance
            {
                Id = r.bal_id.Value,
                MaterialProductId = prod.Id,
                TotalStock = r.total_stock ?? 0,
                AvailableStock = r.available_stock ?? 0,
                ReservedStock = r.reserved_stock ?? 0,
                DispatchedStock = r.dispatched_stock ?? 0,
                OnLoanStock = r.on_loan_stock ?? 0,
                UnderInspectionStock = r.under_inspection_stock ?? 0,
                UnderCleaningStock = r.under_cleaning_stock ?? 0,
                DamagedStock = r.damaged_stock ?? 0,
                MissingStock = r.missing_stock ?? 0,
                RetiredStock = r.retired_stock ?? 0
            };
        }

        return prod;
    }

    private class MaterialProductRow
    {
        public Guid id { get; set; }
        public string sku { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string? description { get; set; }
        public string category { get; set; } = string.Empty;
        public string? image_url { get; set; }
        public string material_type { get; set; } = string.Empty;
        public string? safety_instructions { get; set; }
        public int? age_min_months { get; set; }
        public int? age_max_months { get; set; }
        public string? usage_restrictions { get; set; }
        public string? cleaning_instructions { get; set; }
        public string? return_policy { get; set; }
        public bool is_active { get; set; }
        public DateTimeOffset created_at { get; set; }
        public DateTimeOffset updated_at { get; set; }

        public Guid? off_id { get; set; }
        public Guid? off_prod_id { get; set; }
        public bool? allow_purchase { get; set; }
        public decimal? purchase_price { get; set; }
        public bool? allow_borrow { get; set; }
        public decimal? rental_fee { get; set; }
        public decimal? refundable_deposit { get; set; }
        public int? default_loan_duration_days { get; set; }
        public int? max_loan_duration_days { get; set; }
        public string? currency { get; set; }
        public bool? off_is_active { get; set; }

        public Guid? bal_id { get; set; }
        public int? total_stock { get; set; }
        public int? available_stock { get; set; }
        public int? reserved_stock { get; set; }
        public int? dispatched_stock { get; set; }
        public int? on_loan_stock { get; set; }
        public int? under_inspection_stock { get; set; }
        public int? under_cleaning_stock { get; set; }
        public int? damaged_stock { get; set; }
        public int? missing_stock { get; set; }
        public int? retired_stock { get; set; }
    }
}

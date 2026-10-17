using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
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

        var sql = @"
            SELECT p.id, p.sku, p.name, p.description, p.category, p.image_url,
                   p.material_type, p.safety_instructions, p.age_min_months, p.age_max_months,
                   p.usage_restrictions, p.cleaning_instructions, p.return_policy,
                   p.is_active, p.created_at, p.updated_at,
                   o.id as off_id, o.material_product_id as off_prod_id, o.allow_purchase, o.purchase_price,
                   o.allow_borrow, o.rental_fee, o.refundable_deposit, o.default_loan_duration_days,
                   o.max_loan_duration_days, o.currency, o.is_active as off_is_active,
                   b.id as bal_id, b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
                   b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock, b.damaged_stock,
                   b.missing_stock, b.retired_stock
            FROM material_products p
            LEFT JOIN material_offerings o ON p.id = o.material_product_id
            LEFT JOIN inventory_balances b ON p.id = b.material_product_id
            WHERE 1=1 ";

        if (activeOnly)
            sql += " AND p.is_active = TRUE ";
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            sql += " AND p.category = @Category ";
        if (!string.IsNullOrWhiteSpace(materialType) && materialType != "All")
            sql += " AND p.material_type = @MaterialType ";

        sql += " ORDER BY p.name ASC ";

        var rows = await conn.QueryAsync(sql, new { Category = category, MaterialType = materialType });
        var result = new List<MaterialProduct>();

        foreach (var r in rows)
        {
            var prod = new MaterialProduct
            {
                Id = r.id,
                Sku = r.sku,
                Name = r.name,
                Description = r.description,
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
                    Id = r.off_id,
                    MaterialProductId = r.off_prod_id,
                    AllowPurchase = r.allow_purchase,
                    PurchasePrice = r.purchase_price,
                    AllowBorrow = r.allow_borrow,
                    RentalFee = r.rental_fee,
                    RefundableDeposit = r.refundable_deposit,
                    DefaultLoanDurationDays = r.default_loan_duration_days,
                    MaxLoanDurationDays = r.max_loan_duration_days,
                    Currency = r.currency,
                    IsActive = r.off_is_active
                };
            }

            if (r.bal_id != null)
            {
                prod.StockBalance = new InventoryBalance
                {
                    Id = r.bal_id,
                    MaterialProductId = prod.Id,
                    TotalStock = r.total_stock,
                    AvailableStock = r.available_stock,
                    ReservedStock = r.reserved_stock,
                    DispatchedStock = r.dispatched_stock,
                    OnLoanStock = r.on_loan_stock,
                    UnderInspectionStock = r.under_inspection_stock,
                    UnderCleaningStock = r.under_cleaning_stock,
                    DamagedStock = r.damaged_stock,
                    MissingStock = r.missing_stock,
                    RetiredStock = r.retired_stock
                };
            }

            result.Add(prod);
        }

        return result;
    }

    public async Task<MaterialProduct?> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT p.id, p.sku, p.name, p.description, p.category, p.image_url,
                   p.material_type, p.safety_instructions, p.age_min_months, p.age_max_months,
                   p.usage_restrictions, p.cleaning_instructions, p.return_policy,
                   p.is_active, p.created_at, p.updated_at,
                   o.id as off_id, o.material_product_id as off_prod_id, o.allow_purchase, o.purchase_price,
                   o.allow_borrow, o.rental_fee, o.refundable_deposit, o.default_loan_duration_days,
                   o.max_loan_duration_days, o.currency, o.is_active as off_is_active,
                   b.id as bal_id, b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
                   b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock, b.damaged_stock,
                   b.missing_stock, b.retired_stock
            FROM material_products p
            LEFT JOIN material_offerings o ON p.id = o.material_product_id
            LEFT JOIN inventory_balances b ON p.id = b.material_product_id
            WHERE p.id = @ProductId";

        var r = await conn.QuerySingleOrDefaultAsync(sql, new { ProductId = productId });
        if (r == null) return null;

        var prod = new MaterialProduct
        {
            Id = r.id,
            Sku = r.sku,
            Name = r.name,
            Description = r.description,
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
                Id = r.off_id,
                MaterialProductId = r.off_prod_id,
                AllowPurchase = r.allow_purchase,
                PurchasePrice = r.purchase_price,
                AllowBorrow = r.allow_borrow,
                RentalFee = r.rental_fee,
                RefundableDeposit = r.refundable_deposit,
                DefaultLoanDurationDays = r.default_loan_duration_days,
                MaxLoanDurationDays = r.max_loan_duration_days,
                Currency = r.currency,
                IsActive = r.off_is_active
            };
        }

        if (r.bal_id != null)
        {
            prod.StockBalance = new InventoryBalance
            {
                Id = r.bal_id,
                MaterialProductId = prod.Id,
                TotalStock = r.total_stock,
                AvailableStock = r.available_stock,
                ReservedStock = r.reserved_stock,
                DispatchedStock = r.dispatched_stock,
                OnLoanStock = r.on_loan_stock,
                UnderInspectionStock = r.under_inspection_stock,
                UnderCleaningStock = r.under_cleaning_stock,
                DamagedStock = r.damaged_stock,
                MissingStock = r.missing_stock,
                RetiredStock = r.retired_stock
            };
        }

        return prod;
    }

    public async Task<MaterialProduct?> GetProductBySkuAsync(string sku, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.QuerySingleOrDefaultAsync<Guid?>("SELECT id FROM material_products WHERE sku = @Sku", new { Sku = sku });
        if (!id.HasValue) return null;
        return await GetProductByIdAsync(id.Value, ct);
    }

    public async Task<Guid> CreateProductAsync(MaterialProduct product, MaterialOffering offering, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (product.Id == Guid.Empty) product.Id = Guid.NewGuid();
        if (offering.Id == Guid.Empty) offering.Id = Guid.NewGuid();
        offering.MaterialProductId = product.Id;

        await conn.ExecuteAsync(@"
            INSERT INTO material_products (
                id, sku, name, description, category, image_url, material_type,
                safety_instructions, age_min_months, age_max_months, usage_restrictions,
                cleaning_instructions, return_policy, is_active, created_at, updated_at
            ) VALUES (
                @Id, @Sku, @Name, @Description, @Category, @ImageUrl, @MaterialType,
                @SafetyInstructions, @AgeMinMonths, @AgeMaxMonths, @UsageRestrictions,
                @CleaningInstructions, @ReturnPolicy, @IsActive, @CreatedAt, @UpdatedAt
            )", product);

        await conn.ExecuteAsync(@"
            INSERT INTO material_offerings (
                id, material_product_id, allow_purchase, purchase_price, allow_borrow,
                rental_fee, refundable_deposit, default_loan_duration_days, max_loan_duration_days,
                currency, is_active, created_at, updated_at
            ) VALUES (
                @Id, @MaterialProductId, @AllowPurchase, @PurchasePrice, @AllowBorrow,
                @RentalFee, @RefundableDeposit, @DefaultLoanDurationDays, @MaxLoanDurationDays,
                @Currency, @IsActive, @CreatedAt, @UpdatedAt
            )", offering);

        return product.Id;
    }

    public async Task UpdateProductAsync(MaterialProduct product, MaterialOffering offering, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        product.UpdatedAt = DateTimeOffset.UtcNow;
        offering.UpdatedAt = DateTimeOffset.UtcNow;

        await conn.ExecuteAsync(@"
            UPDATE material_products SET
                name = @Name,
                description = @Description,
                category = @Category,
                image_url = @ImageUrl,
                material_type = @MaterialType,
                safety_instructions = @SafetyInstructions,
                age_min_months = @AgeMinMonths,
                age_max_months = @AgeMaxMonths,
                usage_restrictions = @UsageRestrictions,
                cleaning_instructions = @CleaningInstructions,
                return_policy = @ReturnPolicy,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id", product);

        await conn.ExecuteAsync(@"
            UPDATE material_offerings SET
                allow_purchase = @AllowPurchase,
                purchase_price = @PurchasePrice,
                allow_borrow = @AllowBorrow,
                rental_fee = @RentalFee,
                refundable_deposit = @RefundableDeposit,
                default_loan_duration_days = @DefaultLoanDurationDays,
                max_loan_duration_days = @MaxLoanDurationDays,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE material_product_id = @Id", new
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
        });
    }

    #endregion

    #region Inventory & Warehouse

    public async Task<List<InventoryLocation>> GetLocationsAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<InventoryLocation>(
            "SELECT * FROM inventory_locations WHERE is_active = TRUE ORDER BY is_central_warehouse DESC, name ASC");
        return rows.ToList();
    }

    public async Task<InventoryLocation?> GetCentralWarehouseLocationAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<InventoryLocation>(
            "SELECT * FROM inventory_locations WHERE is_central_warehouse = TRUE AND is_active = TRUE LIMIT 1");
    }

    public async Task<List<InventoryBalance>> GetInventoryBalancesAsync(Guid? locationId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT b.*, p.name as product_name, p.sku as product_sku, l.name as location_name
            FROM inventory_balances b
            JOIN material_products p ON b.material_product_id = p.id
            JOIN inventory_locations l ON b.location_id = l.id ";

        if (locationId.HasValue)
        {
            sql += " WHERE b.location_id = @LocationId ";
        }
        sql += " ORDER BY p.name ASC";

        var rows = await conn.QueryAsync<InventoryBalance>(sql, new { LocationId = locationId });
        return rows.ToList();
    }

    public async Task<InventoryBalance?> GetInventoryBalanceAsync(Guid productId, Guid locationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT b.*, p.name as product_name, p.sku as product_sku, l.name as location_name
            FROM inventory_balances b
            JOIN material_products p ON b.material_product_id = p.id
            JOIN inventory_locations l ON b.location_id = l.id
            WHERE b.material_product_id = @ProductId AND b.location_id = @LocationId";

        return await conn.QuerySingleOrDefaultAsync<InventoryBalance>(sql, new { ProductId = productId, LocationId = locationId });
    }

    public async Task<List<InventoryLedgerEntry>> GetProductLedgerAsync(Guid productId, int limit = 50, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT l.*, p.name as product_name, p.sku as product_sku
            FROM inventory_ledger l
            JOIN material_products p ON l.material_product_id = p.id
            WHERE l.material_product_id = @ProductId
            ORDER BY l.created_at DESC
            LIMIT @Limit";

        var rows = await conn.QueryAsync<InventoryLedgerEntry>(sql, new { ProductId = productId, Limit = limit });
        return rows.ToList();
    }

    public async Task RecordInventoryLedgerAsync(InventoryLedgerEntry entry, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (entry.Id == Guid.Empty) entry.Id = Guid.NewGuid();
        entry.CreatedAt = entry.CreatedAt.ToUniversalTime();

        await conn.ExecuteAsync(@"
            INSERT INTO inventory_ledger (
                id, material_product_id, location_id, transaction_type, quantity_delta,
                available_delta, reserved_delta, on_loan_delta, reference_type, reference_id,
                notes, performed_by_user_id, performed_by_email, created_at
            ) VALUES (
                @Id, @MaterialProductId, @LocationId, @TransactionType, @QuantityDelta,
                @AvailableDelta, @ReservedDelta, @OnLoanDelta, @ReferenceType, @ReferenceId,
                @Notes, @PerformedByUserId, @PerformedByEmail, @CreatedAt
            )", entry);
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

        // First attempt direct update for existing product balance at location
        var updateSql = @"
            UPDATE inventory_balances SET
                total_stock = total_stock + @TotalDelta,
                available_stock = GREATEST(0, available_stock + @AvailableDelta),
                reserved_stock = GREATEST(0, reserved_stock + @ReservedDelta),
                dispatched_stock = GREATEST(0, dispatched_stock + @DispatchedDelta),
                on_loan_stock = GREATEST(0, on_loan_stock + @OnLoanDelta),
                under_inspection_stock = GREATEST(0, under_inspection_stock + @UnderInspectionDelta),
                under_cleaning_stock = GREATEST(0, under_cleaning_stock + @UnderCleaningDelta),
                damaged_stock = GREATEST(0, damaged_stock + @DamagedDelta),
                missing_stock = GREATEST(0, missing_stock + @MissingDelta),
                retired_stock = GREATEST(0, retired_stock + @RetiredDelta),
                updated_at = NOW()
            WHERE material_product_id = @ProductId AND location_id = @LocationId";

        var rows = await conn.ExecuteAsync(updateSql, new
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
        });

        if (rows == 0)
        {
            // Initial row if product had no prior balance record at location
            var insertSql = @"
                INSERT INTO inventory_balances (
                    id, material_product_id, location_id, total_stock, available_stock,
                    reserved_stock, dispatched_stock, on_loan_stock, under_inspection_stock,
                    under_cleaning_stock, damaged_stock, missing_stock, retired_stock, updated_at
                ) VALUES (
                    gen_random_uuid(), @ProductId, @LocationId,
                    GREATEST(0, @TotalDelta), GREATEST(0, @AvailableDelta),
                    GREATEST(0, @ReservedDelta), GREATEST(0, @DispatchedDelta),
                    GREATEST(0, @OnLoanDelta), GREATEST(0, @UnderInspectionDelta),
                    GREATEST(0, @UnderCleaningDelta), GREATEST(0, @DamagedDelta),
                    GREATEST(0, @MissingDelta), GREATEST(0, @RetiredDelta), NOW()
                )
                ON CONFLICT (material_product_id, location_id) DO UPDATE SET
                    total_stock = inventory_balances.total_stock + @TotalDelta,
                    available_stock = GREATEST(0, inventory_balances.available_stock + @AvailableDelta),
                    reserved_stock = GREATEST(0, inventory_balances.reserved_stock + @ReservedDelta),
                    dispatched_stock = GREATEST(0, inventory_balances.dispatched_stock + @DispatchedDelta),
                    on_loan_stock = GREATEST(0, inventory_balances.on_loan_stock + @OnLoanDelta),
                    under_inspection_stock = GREATEST(0, inventory_balances.under_inspection_stock + @UnderInspectionDelta),
                    under_cleaning_stock = GREATEST(0, inventory_balances.under_cleaning_stock + @UnderCleaningDelta),
                    damaged_stock = GREATEST(0, inventory_balances.damaged_stock + @DamagedDelta),
                    missing_stock = GREATEST(0, inventory_balances.missing_stock + @MissingDelta),
                    retired_stock = inventory_balances.retired_stock + @RetiredDelta,
                    updated_at = NOW();";

            await conn.ExecuteAsync(insertSql, new
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
            });
        }
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

        await conn.ExecuteAsync(@"
            INSERT INTO material_orders (
                id, order_number, tenant_id, organization_id, branch_id, classroom_id,
                planned_experience_id, requesting_user_id, requesting_user_email,
                order_type, workflow_status, payment_status, fulfilment_status,
                total_purchase_amount, total_rental_fee, total_deposit, shipping_fee,
                tax_amount, total_amount, currency, quotation_notes, quotation_valid_until,
                delivery_address, delivery_contact_name, delivery_contact_phone,
                required_delivery_date, created_at, updated_at
            ) VALUES (
                @Id, @OrderNumber, @TenantId, @OrganizationId, @BranchId, @ClassroomId,
                @PlannedExperienceId, @RequestingUserId, @RequestingUserEmail,
                @OrderType, @WorkflowStatus, @PaymentStatus, @FulfilmentStatus,
                @TotalPurchaseAmount, @TotalRentalFee, @TotalDeposit, @ShippingFee,
                @TaxAmount, @TotalAmount, @Currency, @QuotationNotes, @QuotationValidUntil,
                @DeliveryAddress, @DeliveryContactName, @DeliveryContactPhone,
                @RequiredDeliveryDate, @CreatedAt, @UpdatedAt
            )", order);

        foreach (var item in items)
        {
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
            item.OrderId = order.Id;
            item.CreatedAt = item.CreatedAt.ToUniversalTime();
            if (item.ReturnDueDate.HasValue) item.ReturnDueDate = item.ReturnDueDate.Value.ToUniversalTime();

            await conn.ExecuteAsync(@"
                INSERT INTO material_order_items (
                    id, order_id, material_product_id, offering_id, fulfilment_mode,
                    quantity, unit_price, rental_fee, deposit, line_total, loan_duration_days,
                    return_due_date, status, created_at
                ) VALUES (
                    @Id, @OrderId, @MaterialProductId, @OfferingId, @FulfilmentMode,
                    @Quantity, @UnitPrice, @RentalFee, @Deposit, @LineTotal, @LoanDurationDays,
                    @ReturnDueDate, @Status, @CreatedAt
                )", item);
        }

        if (initialHistory.Id == Guid.Empty) initialHistory.Id = Guid.NewGuid();
        initialHistory.OrderId = order.Id;

        await AddOrderHistoryEventAsync(initialHistory, ct);

        return order.Id;
    }

    public async Task<MaterialOrder?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT o.*, org.name as organization_name, b.name as branch_name, c.name as classroom_name,
                   e.title as planned_experience_title
            FROM material_orders o
            JOIN organizations org ON o.organization_id = org.id
            LEFT JOIN branches b ON o.branch_id = b.id
            LEFT JOIN classrooms c ON o.classroom_id = c.id
            LEFT JOIN experiences e ON o.planned_experience_id = e.id
            WHERE o.id = @OrderId";

        var order = await conn.QuerySingleOrDefaultAsync<MaterialOrder>(sql, new { OrderId = orderId });
        if (order == null) return null;

        order.Items = await GetItemsForOrderInternalAsync(conn, order.Id);
        order.History = await GetHistoryForOrderInternalAsync(conn, order.Id, true);
        order.Shipment = await conn.QuerySingleOrDefaultAsync<MaterialShipment>(
            "SELECT * FROM material_shipments WHERE order_id = @OrderId LIMIT 1", new { OrderId = order.Id });
        order.Loans = (await conn.QueryAsync<MaterialLoan>(
            "SELECT * FROM material_loans WHERE order_id = @OrderId", new { OrderId = order.Id })).ToList();

        return order;
    }

    public async Task<MaterialOrder?> GetOrderByNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.QuerySingleOrDefaultAsync<Guid?>(
            "SELECT id FROM material_orders WHERE order_number = @OrderNumber", new { OrderNumber = orderNumber });
        if (!id.HasValue) return null;
        return await GetOrderByIdAsync(id.Value, ct);
    }

    public async Task<List<MaterialOrder>> GetOrdersForOrganizationAsync(Guid organizationId, string? statusFilter = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT o.*, org.name as organization_name, b.name as branch_name, c.name as classroom_name,
                   e.title as planned_experience_title
            FROM material_orders o
            JOIN organizations org ON o.organization_id = org.id
            LEFT JOIN branches b ON o.branch_id = b.id
            LEFT JOIN classrooms c ON o.classroom_id = c.id
            LEFT JOIN experiences e ON o.planned_experience_id = e.id
            WHERE o.organization_id = @OrganizationId ";

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            sql += " AND o.workflow_status = @Status ";
        }

        sql += " ORDER BY o.created_at DESC";

        var orders = (await conn.QueryAsync<MaterialOrder>(sql, new { OrganizationId = organizationId, Status = statusFilter })).ToList();
        foreach (var ord in orders)
        {
            ord.Items = await GetItemsForOrderInternalAsync(conn, ord.Id);
        }

        return orders;
    }

    public async Task<List<MaterialOrder>> GetPlatformOrderQueueAsync(string? statusFilter = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT o.*, org.name as organization_name, b.name as branch_name, c.name as classroom_name,
                   e.title as planned_experience_title
            FROM material_orders o
            JOIN organizations org ON o.organization_id = org.id
            LEFT JOIN branches b ON o.branch_id = b.id
            LEFT JOIN classrooms c ON o.classroom_id = c.id
            LEFT JOIN experiences e ON o.planned_experience_id = e.id ";

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            sql += " WHERE o.workflow_status = @Status ";
        }

        sql += " ORDER BY o.created_at DESC";

        var orders = (await conn.QueryAsync<MaterialOrder>(sql, new { Status = statusFilter })).ToList();
        foreach (var ord in orders)
        {
            ord.Items = await GetItemsForOrderInternalAsync(conn, ord.Id);
        }

        return orders;
    }

    public async Task UpdateOrderStatusAsync(Guid orderId, string newStatus, string? paymentStatus = null, string? fulfilmentStatus = null, string? rejectionReason = null, string? cancellationReason = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            UPDATE material_orders SET
                workflow_status = @NewStatus,
                payment_status = COALESCE(@PaymentStatus, payment_status),
                fulfilment_status = COALESCE(@FulfilmentStatus, fulfilment_status),
                rejection_reason = COALESCE(@RejectionReason, rejection_reason),
                cancellation_reason = COALESCE(@CancellationReason, cancellation_reason),
                updated_at = NOW()
            WHERE id = @OrderId";

        await conn.ExecuteAsync(sql, new
        {
            OrderId = orderId,
            NewStatus = newStatus,
            PaymentStatus = paymentStatus,
            FulfilmentStatus = fulfilmentStatus,
            RejectionReason = rejectionReason,
            CancellationReason = cancellationReason
        });
    }

    public async Task UpdateOrderQuotationAsync(Guid orderId, decimal totalPurchaseAmount, decimal totalRentalFee, decimal totalDeposit, decimal shippingFee, decimal totalAmount, string? quotationNotes, DateTimeOffset? validUntil, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            UPDATE material_orders SET
                total_purchase_amount = @TotalPurchaseAmount,
                total_rental_fee = @TotalRentalFee,
                total_deposit = @TotalDeposit,
                shipping_fee = @ShippingFee,
                total_amount = @TotalAmount,
                quotation_notes = @QuotationNotes,
                quotation_valid_until = @ValidUntil,
                workflow_status = 'QuotationOffered',
                updated_at = NOW()
            WHERE id = @OrderId";

        await conn.ExecuteAsync(sql, new
        {
            OrderId = orderId,
            TotalPurchaseAmount = totalPurchaseAmount,
            TotalRentalFee = totalRentalFee,
            TotalDeposit = totalDeposit,
            ShippingFee = shippingFee,
            TotalAmount = totalAmount,
            QuotationNotes = quotationNotes,
            ValidUntil = validUntil
        });
    }

    public async Task AddOrderHistoryEventAsync(MaterialOrderHistory history, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (history.Id == Guid.Empty) history.Id = Guid.NewGuid();
        history.CreatedAt = history.CreatedAt.ToUniversalTime();

        await conn.ExecuteAsync(@"
            INSERT INTO material_order_history (
                id, order_id, event_type, previous_status, new_status, actor_user_id,
                actor_email, actor_role, description, notes, is_internal_note, created_at
            ) VALUES (
                @Id, @OrderId, @EventType, @PreviousStatus, @NewStatus, @ActorUserId,
                @ActorEmail, @ActorRole, @Description, @Notes, @IsInternalNote, @CreatedAt
            )", history);
    }

    public async Task<List<MaterialOrderHistory>> GetOrderHistoryAsync(Guid orderId, bool includeInternalNotes = false, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await GetHistoryForOrderInternalAsync(conn, orderId, includeInternalNotes);
    }

    private static async Task<List<MaterialOrderItem>> GetItemsForOrderInternalAsync(IDbConnection conn, Guid orderId)
    {
        var sql = @"
            SELECT i.*, p.name as product_name, p.sku as product_sku, p.image_url, p.category
            FROM material_order_items i
            JOIN material_products p ON i.material_product_id = p.id
            WHERE i.order_id = @OrderId";

        var rows = await conn.QueryAsync<MaterialOrderItem>(sql, new { OrderId = orderId });
        return rows.ToList();
    }

    private static async Task<List<MaterialOrderHistory>> GetHistoryForOrderInternalAsync(IDbConnection conn, Guid orderId, bool includeInternalNotes)
    {
        var sql = @"
            SELECT * FROM material_order_history
            WHERE order_id = @OrderId ";

        if (!includeInternalNotes)
        {
            sql += " AND is_internal_note = FALSE ";
        }

        sql += " ORDER BY created_at ASC";

        var rows = await conn.QueryAsync<MaterialOrderHistory>(sql, new { OrderId = orderId });
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

        await conn.ExecuteAsync(@"
            INSERT INTO material_shipments (
                id, order_id, shipment_number, carrier_name, tracking_reference,
                dispatch_date, estimated_delivery_date, actual_delivery_date,
                delivery_status, proof_of_delivery_notes, recorded_by_user_id,
                created_at, updated_at
            ) VALUES (
                @Id, @OrderId, @ShipmentNumber, @CarrierName, @TrackingReference,
                @DispatchDate, @EstimatedDeliveryDate, @ActualDeliveryDate,
                @DeliveryStatus, @ProofOfDeliveryNotes, @RecordedByUserId,
                @CreatedAt, @UpdatedAt
            )", shipment);

        return shipment.Id;
    }

    public async Task<MaterialShipment?> GetShipmentByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<MaterialShipment>(
            "SELECT * FROM material_shipments WHERE order_id = @OrderId ORDER BY created_at DESC LIMIT 1",
            new { OrderId = orderId });
    }

    public async Task UpdateShipmentStatusAsync(Guid shipmentId, string deliveryStatus, DateTimeOffset? actualDeliveryDate, string? proofNotes, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(@"
            UPDATE material_shipments SET
                delivery_status = @DeliveryStatus,
                actual_delivery_date = COALESCE(@ActualDeliveryDate, actual_delivery_date),
                proof_of_delivery_notes = COALESCE(@ProofNotes, proof_of_delivery_notes),
                updated_at = NOW()
            WHERE id = @ShipmentId", new
        {
            ShipmentId = shipmentId,
            DeliveryStatus = deliveryStatus,
            ActualDeliveryDate = actualDeliveryDate?.ToUniversalTime(),
            ProofNotes = proofNotes
        });
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

            await conn.ExecuteAsync(@"
                INSERT INTO material_loans (
                    id, order_id, order_item_id, material_product_id, organization_id,
                    classroom_id, quantity_issued, issued_at, due_date, actual_return_date,
                    return_status, deposit_amount, deposit_status, notes, created_at, updated_at
                ) VALUES (
                    @Id, @OrderId, @OrderItemId, @MaterialProductId, @OrganizationId,
                    @ClassroomId, @QuantityIssued, @IssuedAt, @DueDate, @ActualReturnDate,
                    @ReturnStatus, @DepositAmount, @DepositStatus, @Notes, @CreatedAt, @UpdatedAt
                )", loan);
        }
    }

    public async Task<List<MaterialLoan>> GetLoansByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT l.*, p.name as product_name, p.sku as product_sku, o.order_number, c.name as classroom_name
            FROM material_loans l
            JOIN material_products p ON l.material_product_id = p.id
            JOIN material_orders o ON l.order_id = o.id
            LEFT JOIN classrooms c ON l.classroom_id = c.id
            WHERE l.order_id = @OrderId";

        var rows = await conn.QueryAsync<MaterialLoan>(sql, new { OrderId = orderId });
        return rows.ToList();
    }

    public async Task<List<MaterialLoan>> GetActiveLoansForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT l.*, p.name as product_name, p.sku as product_sku, o.order_number, c.name as classroom_name
            FROM material_loans l
            JOIN material_products p ON l.material_product_id = p.id
            JOIN material_orders o ON l.order_id = o.id
            LEFT JOIN classrooms c ON l.classroom_id = c.id
            WHERE l.organization_id = @OrganizationId
            ORDER BY l.due_date ASC";

        var rows = await conn.QueryAsync<MaterialLoan>(sql, new { OrganizationId = organizationId });
        return rows.ToList();
    }

    public async Task<List<MaterialLoan>> GetPlatformLoanQueueAsync(string? statusFilter = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT l.*, p.name as product_name, p.sku as product_sku, o.order_number, c.name as classroom_name
            FROM material_loans l
            JOIN material_products p ON l.material_product_id = p.id
            JOIN material_orders o ON l.order_id = o.id
            LEFT JOIN classrooms c ON l.classroom_id = c.id ";

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            sql += " WHERE l.return_status = @Status ";
        }

        sql += " ORDER BY l.due_date ASC";

        var rows = await conn.QueryAsync<MaterialLoan>(sql, new { Status = statusFilter });
        return rows.ToList();
    }

    public async Task<MaterialLoan?> GetLoanByIdAsync(Guid loanId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT l.*, p.name as product_name, p.sku as product_sku, o.order_number, c.name as classroom_name
            FROM material_loans l
            JOIN material_products p ON l.material_product_id = p.id
            JOIN material_orders o ON l.order_id = o.id
            LEFT JOIN classrooms c ON l.classroom_id = c.id
            WHERE l.id = @LoanId";

        return await conn.QuerySingleOrDefaultAsync<MaterialLoan>(sql, new { LoanId = loanId });
    }

    public async Task UpdateLoanStatusAsync(Guid loanId, string returnStatus, DateTimeOffset? actualReturnDate = null, string? notes = null, string? depositStatus = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            UPDATE material_loans SET
                return_status = @ReturnStatus,
                actual_return_date = COALESCE(@ActualReturnDate, actual_return_date),
                notes = COALESCE(@Notes, notes),
                deposit_status = COALESCE(@DepositStatus, deposit_status),
                updated_at = NOW()
            WHERE id = @LoanId";

        await conn.ExecuteAsync(sql, new
        {
            LoanId = loanId,
            ReturnStatus = returnStatus,
            ActualReturnDate = actualReturnDate,
            Notes = notes,
            DepositStatus = depositStatus
        });
    }

    public async Task<Guid> RecordReturnInspectionAsync(MaterialReturnInspection inspection, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (inspection.Id == Guid.Empty) inspection.Id = Guid.NewGuid();
        inspection.CreatedAt = inspection.CreatedAt.ToUniversalTime();
        inspection.InspectedAt = inspection.InspectedAt.ToUniversalTime();

        await conn.ExecuteAsync(@"
            INSERT INTO material_return_inspections (
                id, loan_id, order_id, material_product_id, returned_quantity,
                inspected_at, inspected_by_user_id, inspected_by_email, condition,
                quantity_approved_for_stock, quantity_requiring_cleaning,
                quantity_damaged, quantity_missing, inspection_notes,
                deposit_disposition, deposit_deduction_amount, resolution_status, created_at
            ) VALUES (
                @Id, @LoanId, @OrderId, @MaterialProductId, @ReturnedQuantity,
                @InspectedAt, @InspectedByUserId, @InspectedByEmail, @Condition,
                @QuantityApprovedForStock, @QuantityRequiringCleaning,
                @QuantityDamaged, @QuantityMissing, @InspectionNotes,
                @DepositDisposition, @DepositDeductionAmount, @ResolutionStatus, @CreatedAt
            )", inspection);

        return inspection.Id;
    }

    public async Task<MaterialReturnInspection?> GetInspectionByLoanIdAsync(Guid loanId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT i.*, p.name as product_name, p.sku as product_sku
            FROM material_return_inspections i
            JOIN material_products p ON i.material_product_id = p.id
            WHERE i.loan_id = @LoanId
            ORDER BY i.created_at DESC LIMIT 1";

        return await conn.QuerySingleOrDefaultAsync<MaterialReturnInspection>(sql, new { LoanId = loanId });
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

        await conn.ExecuteAsync(@"
            INSERT INTO material_recurring_schedules (
                id, tenant_id, organization_id, branch_id, classroom_id, schedule_type,
                frequency, material_product_id, offering_id, quantity, loan_duration_days,
                delivery_address, next_run_date, last_run_date, status, notes,
                created_by_user_id, created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @ClassroomId, @ScheduleType,
                @Frequency, @MaterialProductId, @OfferingId, @Quantity, @LoanDurationDays,
                @DeliveryAddress, @NextRunDate, @LastRunDate, @Status, @Notes,
                @CreatedByUserId, @CreatedAt, @UpdatedAt
            )", schedule);

        return schedule.Id;
    }

    public async Task<List<MaterialRecurringSchedule>> GetRecurringSchedulesAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT s.*, p.name as product_name, p.sku as product_sku
            FROM material_recurring_schedules s
            JOIN material_products p ON s.material_product_id = p.id
            WHERE s.organization_id = @OrganizationId
            ORDER BY s.next_run_date ASC";

        var rows = await conn.QueryAsync<MaterialRecurringSchedule>(sql, new { OrganizationId = organizationId });
        return rows.ToList();
    }

    public async Task<List<MaterialRecurringSchedule>> GetDueRecurringSchedulesAsync(DateTimeOffset asOfDate, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT s.*, p.name as product_name, p.sku as product_sku
            FROM material_recurring_schedules s
            JOIN material_products p ON s.material_product_id = p.id
            WHERE s.status = 'Active' AND s.next_run_date <= @AsOfDate
            ORDER BY s.next_run_date ASC";

        var rows = await conn.QueryAsync<MaterialRecurringSchedule>(sql, new { AsOfDate = asOfDate.ToUniversalTime() });
        return rows.ToList();
    }

    public async Task UpdateRecurringScheduleStatusAsync(Guid scheduleId, string status, DateTimeOffset? nextRunDate, DateTimeOffset? lastRunDate, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(@"
            UPDATE material_recurring_schedules SET
                status = @Status,
                next_run_date = COALESCE(@NextRunDate, next_run_date),
                last_run_date = COALESCE(@LastRunDate, last_run_date),
                updated_at = NOW()
            WHERE id = @ScheduleId", new
        {
            ScheduleId = scheduleId,
            Status = status,
            NextRunDate = nextRunDate?.ToUniversalTime(),
            LastRunDate = lastRunDate?.ToUniversalTime()
        });
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Repositories;

public partial class MaterialMarketplaceRepository : IMaterialMarketplaceRepository
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

        await conn.ExecuteProcedureAsync("sp_create_material_product", new
        {
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.Category,
            product.ImageUrl,
            product.MaterialType,
            product.SafetyInstructions,
            product.AgeMinMonths,
            product.AgeMaxMonths,
            product.UsageRestrictions,
            product.CleaningInstructions,
            product.ReturnPolicy,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt
        }, cancellationToken: ct);

        await conn.ExecuteProcedureAsync("sp_create_material_offering", new
        {
            offering.Id,
            offering.MaterialProductId,
            offering.AllowPurchase,
            offering.PurchasePrice,
            offering.AllowBorrow,
            offering.RentalFee,
            offering.RefundableDeposit,
            offering.DefaultLoanDurationDays,
            offering.MaxLoanDurationDays,
            offering.Currency,
            offering.IsActive,
            offering.CreatedAt,
            offering.UpdatedAt
        }, cancellationToken: ct);

        return product.Id;
    }

    public async Task UpdateProductAsync(MaterialProduct product, MaterialOffering offering, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        product.UpdatedAt = DateTimeOffset.UtcNow;
        offering.UpdatedAt = DateTimeOffset.UtcNow;

        await conn.ExecuteProcedureAsync("sp_update_material_product", new
        {
            product.Id,
            product.Name,
            product.Description,
            product.Category,
            product.ImageUrl,
            product.MaterialType,
            product.SafetyInstructions,
            product.AgeMinMonths,
            product.AgeMaxMonths,
            product.UsageRestrictions,
            product.CleaningInstructions,
            product.ReturnPolicy,
            product.IsActive,
            product.UpdatedAt
        }, cancellationToken: ct);

        await conn.ExecuteProcedureAsync("sp_update_material_offering", new
        {
            MaterialProductId = product.Id,
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

        await conn.ExecuteProcedureAsync("sp_record_inventory_ledger", new
        {
            entry.Id,
            entry.MaterialProductId,
            entry.LocationId,
            entry.TransactionType,
            entry.QuantityDelta,
            entry.AvailableDelta,
            entry.ReservedDelta,
            entry.OnLoanDelta,
            entry.ReferenceType,
            entry.ReferenceId,
            entry.Notes,
            entry.PerformedByUserId,
            entry.PerformedByEmail,
            entry.CreatedAt
        }, cancellationToken: ct);
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

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public partial class MaterialMarketplaceService : IMaterialMarketplaceService
{
    private readonly IMaterialMarketplaceRepository _materialRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IExperienceRepository _experienceRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IRazorpayGateway _razorpayGateway;
    private readonly IResourceAuthorizer _authorizer;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<MaterialMarketplaceService> _logger;
    private readonly IPlatformBusinessEventService? _eventService;

    public MaterialMarketplaceService(
        IMaterialMarketplaceRepository materialRepository,
        IOrganizationRepository organizationRepository,
        IExperienceRepository experienceRepository,
        IClassroomRepository classroomRepository,
        IBillingRepository billingRepository,
        IRazorpayGateway razorpayGateway,
        IResourceAuthorizer authorizer,
        IUserContext userContext,
        ITenantContext tenantContext,
        IAuditService auditService,
        ILogger<MaterialMarketplaceService> logger,
        IPlatformBusinessEventService? eventService = null)
    {
        _materialRepository = materialRepository;
        _organizationRepository = organizationRepository;
        _experienceRepository = experienceRepository;
        _classroomRepository = classroomRepository;
        _billingRepository = billingRepository;
        _razorpayGateway = razorpayGateway;
        _authorizer = authorizer;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _logger = logger;
        _eventService = eventService;
    }

    #region Catalog & Warehouse

    public async Task<List<MaterialProduct>> GetCatalogProductsAsync(string? category = null, string? materialType = null, bool activeOnly = true, CancellationToken ct = default)
    {
        return await _materialRepository.GetProductsAsync(category, materialType, activeOnly, ct);
    }

    public async Task<MaterialProduct?> GetProductDetailsAsync(Guid productId, CancellationToken ct = default)
    {
        return await _materialRepository.GetProductByIdAsync(productId, ct);
    }

    public async Task<Guid> SaveProductAsync(SaveMaterialProductRequest request, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Product name is required.");
        if (string.IsNullOrWhiteSpace(request.Sku))
            throw new ArgumentException("Product SKU is required.");

        var existingBySku = await _materialRepository.GetProductBySkuAsync(request.Sku, ct);
        if (existingBySku != null && (!request.Id.HasValue || existingBySku.Id != request.Id.Value))
        {
            throw new InvalidOperationException($"A product with SKU '{request.Sku}' already exists.");
        }

        if (request.Id.HasValue && request.Id.Value != Guid.Empty)
        {
            var existing = await _materialRepository.GetProductByIdAsync(request.Id.Value, ct);
            if (existing == null) throw new KeyNotFoundException($"Product with ID {request.Id.Value} not found.");

            existing.Sku = request.Sku.Trim().ToUpperInvariant();
            existing.Name = request.Name.Trim();
            existing.Description = request.Description.Trim();
            existing.Category = request.Category;
            if (request.ImageUrls != null && request.ImageUrls.Count > 0)
            {
                var distinctImages = request.ImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Take(3).ToList();
                existing.ImageUrls = distinctImages;
                existing.ImageUrl = distinctImages.Count > 1 ? JsonSerializer.Serialize(distinctImages) : distinctImages.FirstOrDefault();
            }
            else
            {
                existing.ImageUrl = request.ImageUrl;
                existing.ImageUrls = !string.IsNullOrWhiteSpace(request.ImageUrl) ? new List<string> { request.ImageUrl } : new List<string>();
            }
            existing.MaterialType = request.MaterialType;
            existing.SafetyInstructions = request.SafetyInstructions;
            existing.AgeMinMonths = request.AgeMinMonths;
            existing.AgeMaxMonths = request.AgeMaxMonths;
            existing.UsageRestrictions = request.UsageRestrictions;
            existing.CleaningInstructions = request.CleaningInstructions;
            existing.ReturnPolicy = request.ReturnPolicy;
            existing.IsActive = request.IsActive;

            var offering = existing.Offering ?? new MaterialOffering { MaterialProductId = existing.Id };
            offering.AllowPurchase = request.AllowPurchase;
            offering.PurchasePrice = request.PurchasePrice;
            offering.AllowBorrow = request.AllowBorrow;
            offering.RentalFee = request.RentalFee;
            offering.RefundableDeposit = request.RefundableDeposit;
            offering.DefaultLoanDurationDays = request.DefaultLoanDurationDays;
            offering.MaxLoanDurationDays = request.MaxLoanDurationDays;
            offering.IsActive = request.IsActive;

            await _materialRepository.UpdateProductAsync(existing, offering, ct);
            return existing.Id;
        }
        else
        {
            var newProd = new MaterialProduct
            {
                Id = Guid.NewGuid(),
                Sku = request.Sku.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                Category = request.Category,
                ImageUrl = request.ImageUrls != null && request.ImageUrls.Count > 1 
                    ? JsonSerializer.Serialize(request.ImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Take(3).ToList())
                    : (request.ImageUrls?.FirstOrDefault() ?? request.ImageUrl),
                ImageUrls = request.ImageUrls != null && request.ImageUrls.Count > 0
                    ? request.ImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Take(3).ToList()
                    : (!string.IsNullOrWhiteSpace(request.ImageUrl) ? new List<string> { request.ImageUrl } : new List<string>()),
                MaterialType = request.MaterialType,
                SafetyInstructions = request.SafetyInstructions,
                AgeMinMonths = request.AgeMinMonths,
                AgeMaxMonths = request.AgeMaxMonths,
                UsageRestrictions = request.UsageRestrictions,
                CleaningInstructions = request.CleaningInstructions,
                ReturnPolicy = request.ReturnPolicy,
                IsActive = request.IsActive
            };

            var newOffering = new MaterialOffering
            {
                Id = Guid.NewGuid(),
                MaterialProductId = newProd.Id,
                AllowPurchase = request.AllowPurchase,
                PurchasePrice = request.PurchasePrice,
                AllowBorrow = request.AllowBorrow,
                RentalFee = request.RentalFee,
                RefundableDeposit = request.RefundableDeposit,
                DefaultLoanDurationDays = request.DefaultLoanDurationDays,
                MaxLoanDurationDays = request.MaxLoanDurationDays,
                Currency = "INR",
                IsActive = request.IsActive
            };

            var id = await _materialRepository.CreateProductAsync(newProd, newOffering, ct);

            // Initialize central warehouse balance
            var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
            if (central != null)
            {
                await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                    id, central.Id, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ct);
            }

            return id;
        }
    }

    public async Task<List<InventoryBalance>> GetWarehouseInventoryBalancesAsync(Guid? locationId = null, CancellationToken ct = default)
    {
        return await _materialRepository.GetInventoryBalancesAsync(locationId, ct);
    }

    public async Task<List<InventoryLocation>> GetWarehouseLocationsAsync(CancellationToken ct = default)
    {
        return await _materialRepository.GetLocationsAsync(ct);
    }

    public async Task<List<InventoryLedgerEntry>> GetProductLedgerHistoryAsync(Guid productId, CancellationToken ct = default)
    {
        return await _materialRepository.GetProductLedgerAsync(productId, 50, ct);
    }

    public async Task AdjustInventoryStockAsync(AdjustStockRequest request, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        if (request.QuantityDelta == 0)
            throw new ArgumentException("Quantity delta cannot be zero.");

        var balance = await _materialRepository.GetInventoryBalanceAsync(request.ProductId, request.LocationId, ct);
        if (balance == null)
            throw new KeyNotFoundException("Inventory balance record not found for this product and location.");

        if (balance.AvailableStock + request.QuantityDelta < 0)
            throw new InvalidOperationException($"Cannot reduce stock below zero. Current available: {balance.AvailableStock}");

        // Atomic balance update
        await _materialRepository.UpdateInventoryBalanceAtomicAsync(
            request.ProductId,
            request.LocationId,
            totalDelta: request.QuantityDelta,
            availableDelta: request.QuantityDelta,
            reservedDelta: 0,
            dispatchedDelta: 0,
            onLoanDelta: 0,
            underInspectionDelta: 0,
            underCleaningDelta: 0,
            damagedDelta: 0,
            missingDelta: 0,
            retiredDelta: 0,
            ct: ct);

        // Record Ledger
        var ledger = new InventoryLedgerEntry
        {
            Id = Guid.NewGuid(),
            MaterialProductId = request.ProductId,
            LocationId = request.LocationId,
            TransactionType = request.TransactionType,
            QuantityDelta = request.QuantityDelta,
            AvailableDelta = request.QuantityDelta,
            Notes = request.Notes ?? "Manual inventory adjustment",
            PerformedByUserId = _userContext.UserId,
            PerformedByEmail = _userContext.Email
        };
        await _materialRepository.RecordInventoryLedgerAsync(ledger, ct);
    }

    #endregion

    #region Security Helpers

    private bool IsPlatformAdmin()
    {
        return (_userContext.Roles != null && _userContext.Roles.Any(r => 
                    r.Equals(RoleTemplates.PlatformAdmin, StringComparison.OrdinalIgnoreCase) ||
                    r.Equals("Superadmin", StringComparison.OrdinalIgnoreCase))) ||
               _userContext.HasPermission(Permissions.PlatformWarehouseManage) ||
               _userContext.HasPermission(Permissions.PlatformBillingManage);
    }

    private void EnsurePlatformPermission()
    {
        if (!IsPlatformAdmin())
        {
            throw new UnauthorizedAccessException("This action is restricted to Platform Operations administrators.");
        }
    }

    private async Task EnsureOrganizationAccessAsync(Guid organizationId, CancellationToken ct)
    {
        if (IsPlatformAdmin()) return;

        var userOrgId = _userContext.OrganizationId;
        if (userOrgId.HasValue && userOrgId.Value == organizationId) return;

        var org = await _organizationRepository.GetByIdAsync(organizationId, ct);
        if (org == null)
            throw new KeyNotFoundException("Organization not found.");

        if (org.TenantId != _tenantContext.TenantId)
        {
            throw new UnauthorizedAccessException("Cross-tenant access is strictly denied.");
        }

        if (!_userContext.HasPermission(Permissions.MaterialsView))
        {
            throw new UnauthorizedAccessException("You do not have authorization for this organization's materials and orders.");
        }
    }

    #endregion
}

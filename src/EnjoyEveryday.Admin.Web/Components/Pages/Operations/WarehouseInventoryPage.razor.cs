using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Components;

namespace EnjoyEveryday.Admin.Web.Components.Pages.Operations;

public partial class WarehouseInventoryPage : ComponentBase
{
    [Inject]
    private IMaterialMarketplaceService MaterialService { get; set; } = default!;

    private List<InventoryLocation> _locations = new();
    private List<InventoryBalance> _balances = new();
    private List<MaterialProduct> _products = new();
    private Guid _selectedLocationId = Guid.Empty;
    private string _searchFilter = "";
    private bool _isLoading = true;

    // Adjustment modal
    private InventoryBalance? _adjustModalItem;
    private string _adjustTxType = "Receipt";
    private int _adjustDelta = 10;
    private string _adjustNotes = "";

    // Add Product modal
    private bool _isAddProductOpen = false;
    private string _newSku = "";
    private string _newName = "";
    private string _newCategory = "Sensory & Science";
    private string _newType = "Consumable";
    private string _newDesc = "";
    private string _newSafety = "";
    private decimal _newBuyPrice = 500m;
    private decimal _newRentalFee = 0m;
    private decimal _newDeposit = 0m;
    private int _newInitialStock = 50;

    // Edit Product modal state
    private MaterialProduct? _editProductModalItem;
    private Guid _editProductId = Guid.Empty;
    private string _editSku = "";
    private string _editName = "";
    private string _editCategory = "Sensory & Science";
    private string _editType = "Consumable";
    private string _editDesc = "";
    private string _editSafety = "";
    private int? _editAgeMin;
    private int? _editAgeMax;
    private decimal _editBuyPrice;
    private decimal _editRentalFee;
    private decimal _editDeposit;
    private int _editDefaultLoanDays = 14;
    private int _editMaxLoanDays = 30;
    private bool _editIsActive = true;

    // Ledger drawer
    private InventoryBalance? _ledgerModalItem;
    private List<InventoryLedgerEntry> _ledgerHistory = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadLocationsAndInitialDataAsync();
    }

    private async Task LoadLocationsAndInitialDataAsync()
    {
        _isLoading = true;
        try
        {
            _locations = await MaterialService.GetWarehouseLocationsAsync();
            var central = _locations.FirstOrDefault(l => l.IsCentralWarehouse) ?? _locations.FirstOrDefault();
            if (central != null)
            {
                _selectedLocationId = central.Id;
            }

            _products = await MaterialService.GetCatalogProductsAsync(activeOnly: false);
            await LoadInventoryDataAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing inventory: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadInventoryDataAsync()
    {
        if (_selectedLocationId != Guid.Empty)
        {
            _balances = await MaterialService.GetWarehouseInventoryBalancesAsync(_selectedLocationId);
        }
    }

    // Filtering, Sorting, and Pagination state
    private string _categoryFilter = "All";
    private string _typeFilter = "All";
    private string _stockStatusFilter = "All";

    private string _sortColumn = "ProductName";
    private bool _sortAscending = true;

    private int _currentPage = 1;
    private int _pageSize = 10;

    private List<string> AvailableCategories => _products
        .Select(p => p.Category)
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .Distinct()
        .OrderBy(c => c)
        .ToList();

    private bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(_searchFilter) ||
        _categoryFilter != "All" ||
        _typeFilter != "All" ||
        _stockStatusFilter != "All";

    private void OnFilterChanged()
    {
        _currentPage = 1;
    }

    private void ClearSearch()
    {
        _searchFilter = "";
        _currentPage = 1;
    }

    private void ResetAllFilters()
    {
        _searchFilter = "";
        _categoryFilter = "All";
        _typeFilter = "All";
        _stockStatusFilter = "All";
        _currentPage = 1;
    }

    private void SortTable(string column)
    {
        if (_sortColumn == column)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = column;
            _sortAscending = true;
        }
    }

    private string GetSortIcon(string column)
    {
        if (_sortColumn != column) return "↕";
        return _sortAscending ? "▲" : "▼";
    }

    private async Task OnLocationChangedAsync()
    {
        _currentPage = 1;
        await LoadInventoryDataAsync();
    }

    private void OnPageSizeChanged()
    {
        _currentPage = 1;
    }

    private void GoToPage(int page)
    {
        _currentPage = Math.Clamp(page, 1, TotalPages);
    }

    private IEnumerable<InventoryBalance> FilteredBalances
    {
        get
        {
            var query = _balances.AsEnumerable();

            // Search filter (SKU, Name, Category)
            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                var term = _searchFilter.Trim();
                query = query.Where(b =>
                {
                    var p = _products.FirstOrDefault(prod => prod.Id == b.MaterialProductId);
                    return (b.ProductName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (b.ProductSku?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (p?.Category?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
                });
            }

            // Category filter
            if (_categoryFilter != "All")
            {
                query = query.Where(b =>
                {
                    var p = _products.FirstOrDefault(prod => prod.Id == b.MaterialProductId);
                    return string.Equals(p?.Category, _categoryFilter, StringComparison.OrdinalIgnoreCase);
                });
            }

            // Type filter
            if (_typeFilter != "All")
            {
                query = query.Where(b =>
                {
                    var p = _products.FirstOrDefault(prod => prod.Id == b.MaterialProductId);
                    return string.Equals(p?.MaterialType, _typeFilter, StringComparison.OrdinalIgnoreCase);
                });
            }

            // Stock status filter
            if (_stockStatusFilter != "All")
            {
                query = _stockStatusFilter switch
                {
                    "InStock" => query.Where(b => b.AvailableStock > 0),
                    "LowStock" => query.Where(b => b.AvailableStock > 0 && b.AvailableStock <= 10),
                    "OutOfStock" => query.Where(b => b.AvailableStock <= 0),
                    "OnLoan" => query.Where(b => b.OnLoanStock > 0),
                    "UnderCleaning" => query.Where(b => b.UnderCleaningStock > 0),
                    "Damaged" => query.Where(b => (b.DamagedStock + b.MissingStock) > 0),
                    "Active" => query.Where(b => {
                        var p = _products.FirstOrDefault(prod => prod.Id == b.MaterialProductId);
                        return p?.IsActive ?? true;
                    }),
                    "Discontinued" => query.Where(b => {
                        var p = _products.FirstOrDefault(prod => prod.Id == b.MaterialProductId);
                        return !(p?.IsActive ?? true);
                    }),
                    _ => query
                };
            }

            // Sorting
            query = (_sortColumn, _sortAscending) switch
            {
                ("ProductName", true) => query.OrderBy(b => b.ProductName),
                ("ProductName", false) => query.OrderByDescending(b => b.ProductName),

                ("Category", true) => query.OrderBy(b => _products.FirstOrDefault(p => p.Id == b.MaterialProductId)?.Category ?? ""),
                ("Category", false) => query.OrderByDescending(b => _products.FirstOrDefault(p => p.Id == b.MaterialProductId)?.Category ?? ""),

                ("MaterialType", true) => query.OrderBy(b => _products.FirstOrDefault(p => p.Id == b.MaterialProductId)?.MaterialType ?? ""),
                ("MaterialType", false) => query.OrderByDescending(b => _products.FirstOrDefault(p => p.Id == b.MaterialProductId)?.MaterialType ?? ""),

                ("Status", true) => query.OrderBy(b => _products.FirstOrDefault(p => p.Id == b.MaterialProductId)?.IsActive ?? true),
                ("Status", false) => query.OrderByDescending(b => _products.FirstOrDefault(p => p.Id == b.MaterialProductId)?.IsActive ?? true),

                ("TotalStock", true) => query.OrderBy(b => b.TotalStock),
                ("TotalStock", false) => query.OrderByDescending(b => b.TotalStock),

                ("AvailableStock", true) => query.OrderBy(b => b.AvailableStock),
                ("AvailableStock", false) => query.OrderByDescending(b => b.AvailableStock),

                ("ReservedStock", true) => query.OrderBy(b => b.ReservedStock),
                ("ReservedStock", false) => query.OrderByDescending(b => b.ReservedStock),

                ("DispatchedStock", true) => query.OrderBy(b => b.DispatchedStock),
                ("DispatchedStock", false) => query.OrderByDescending(b => b.DispatchedStock),

                ("OnLoanStock", true) => query.OrderBy(b => b.OnLoanStock),
                ("OnLoanStock", false) => query.OrderByDescending(b => b.OnLoanStock),

                ("UnderCleaningStock", true) => query.OrderBy(b => b.UnderCleaningStock),
                ("UnderCleaningStock", false) => query.OrderByDescending(b => b.UnderCleaningStock),

                ("DamagedStock", true) => query.OrderBy(b => b.DamagedStock + b.MissingStock),
                ("DamagedStock", false) => query.OrderByDescending(b => b.DamagedStock + b.MissingStock),

                _ => query.OrderBy(b => b.ProductName)
            };

            return query;
        }
    }

    private int TotalFilteredCount => FilteredBalances.Count();

    private int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalFilteredCount / _pageSize));

    private IEnumerable<InventoryBalance> PagedBalances =>
        FilteredBalances.Skip((_currentPage - 1) * _pageSize).Take(_pageSize);

    private int ItemRangeStart => TotalFilteredCount == 0 ? 0 : (_currentPage - 1) * _pageSize + 1;
    private int ItemRangeEnd => Math.Min(_currentPage * _pageSize, TotalFilteredCount);

    private IEnumerable<int> VisiblePages
    {
        get
        {
            var total = TotalPages;
            var current = _currentPage;
            var start = Math.Max(1, current - 2);
            var end = Math.Min(total, start + 4);
            if (end - start < 4)
            {
                start = Math.Max(1, end - 4);
            }
            for (int i = start; i <= end; i++)
            {
                yield return i;
            }
        }
    }

    private void OpenAdjustModal(InventoryBalance bal)
    {
        _adjustModalItem = bal;
        _adjustTxType = "Receipt";
        _adjustDelta = 10;
        _adjustNotes = "Standard replenishment received at central warehouse dock";
    }

    private async Task SubmitAdjustment()
    {
        if (_adjustModalItem == null) return;

        await MaterialService.AdjustInventoryStockAsync(new AdjustStockRequest
        {
            ProductId = _adjustModalItem.MaterialProductId,
            LocationId = _selectedLocationId,
            TransactionType = _adjustTxType,
            QuantityDelta = _adjustDelta,
            Notes = _adjustNotes
        });

        _adjustModalItem = null;
        await LoadInventoryDataAsync();
    }

    private async Task SanitizeCleaningStock(InventoryBalance item)
    {
        if (item.UnderCleaningStock <= 0) return;
        await MaterialService.SanitizeAndRestockCleaningItemsAsync(item.MaterialProductId, item.UnderCleaningStock);
        await LoadInventoryDataAsync();
    }

    private void OpenAddProductModal()
    {
        _newSku = $"MAT-{Random.Shared.Next(1000, 9999)}";
        _newName = "";
        _newCategory = "Sensory & Science";
        _newType = "Consumable";
        _newDesc = "";
        _newSafety = "Non-toxic, safe for preschool environments";
        _newBuyPrice = 450m;
        _newRentalFee = 0m;
        _newDeposit = 0m;
        _newInitialStock = 50;
        _isAddProductOpen = true;
    }

    private async Task SubmitNewProduct()
    {
        if (string.IsNullOrWhiteSpace(_newSku) || string.IsNullOrWhiteSpace(_newName)) return;

        var req = new SaveMaterialProductRequest
        {
            Sku = _newSku.Trim(),
            Name = _newName.Trim(),
            Category = _newCategory,
            MaterialType = _newType,
            Description = _newDesc.Trim(),
            SafetyInstructions = _newSafety.Trim(),
            AllowPurchase = _newBuyPrice > 0,
            PurchasePrice = _newBuyPrice,
            AllowBorrow = _newRentalFee > 0,
            RentalFee = _newRentalFee,
            RefundableDeposit = _newDeposit,
            DefaultLoanDurationDays = 14,
            MaxLoanDurationDays = 30,
            IsActive = true
        };

        var id = await MaterialService.SaveProductAsync(req);

        if (_newInitialStock > 0 && _selectedLocationId != Guid.Empty)
        {
            await MaterialService.AdjustInventoryStockAsync(new AdjustStockRequest
            {
                ProductId = id,
                LocationId = _selectedLocationId,
                TransactionType = "Receipt",
                QuantityDelta = _newInitialStock,
                Notes = "Initial catalog opening inventory"
            });
        }

        _isAddProductOpen = false;

        _products = await MaterialService.GetCatalogProductsAsync(activeOnly: false);
        await LoadInventoryDataAsync();
    }

    private async Task OpenEditProductModal(InventoryBalance bal, MaterialProduct? prod)
    {
        var product = prod ?? await MaterialService.GetProductDetailsAsync(bal.MaterialProductId);
        if (product == null) return;

        // Fetch full details with offering
        var full = await MaterialService.GetProductDetailsAsync(product.Id) ?? product;

        _editProductModalItem = full;
        _editProductId = full.Id;
        _editSku = full.Sku;
        _editName = full.Name;
        _editCategory = full.Category;
        _editType = full.MaterialType;
        _editDesc = full.Description ?? "";
        _editSafety = full.SafetyInstructions ?? "";
        _editAgeMin = full.AgeMinMonths;
        _editAgeMax = full.AgeMaxMonths;
        _editIsActive = full.IsActive;

        var offering = full.Offering;
        _editBuyPrice = offering?.PurchasePrice ?? 0m;
        _editRentalFee = offering?.RentalFee ?? 0m;
        _editDeposit = offering?.RefundableDeposit ?? 0m;
        _editDefaultLoanDays = offering?.DefaultLoanDurationDays ?? 14;
        _editMaxLoanDays = offering?.MaxLoanDurationDays ?? 30;
    }

    private async Task SubmitEditProduct()
    {
        if (string.IsNullOrWhiteSpace(_editSku) || string.IsNullOrWhiteSpace(_editName)) return;

        var req = new SaveMaterialProductRequest
        {
            Id = _editProductId,
            Sku = _editSku.Trim(),
            Name = _editName.Trim(),
            Category = _editCategory,
            MaterialType = _editType,
            Description = _editDesc.Trim(),
            SafetyInstructions = _editSafety.Trim(),
            AgeMinMonths = _editAgeMin,
            AgeMaxMonths = _editAgeMax,
            AllowPurchase = _editBuyPrice > 0,
            PurchasePrice = _editBuyPrice,
            AllowBorrow = _editRentalFee > 0,
            RentalFee = _editRentalFee,
            RefundableDeposit = _editDeposit,
            DefaultLoanDurationDays = _editDefaultLoanDays,
            MaxLoanDurationDays = _editMaxLoanDays,
            IsActive = _editIsActive
        };

        await MaterialService.SaveProductAsync(req);
        _editProductModalItem = null;

        _products = await MaterialService.GetCatalogProductsAsync(activeOnly: false);
        await LoadInventoryDataAsync();
    }

    private async Task ToggleProductStatusAsync(MaterialProduct prod, bool newStatus)
    {
        var detailed = await MaterialService.GetProductDetailsAsync(prod.Id) ?? prod;
        var off = detailed.Offering;

        var req = new SaveMaterialProductRequest
        {
            Id = detailed.Id,
            Sku = detailed.Sku,
            Name = detailed.Name,
            Category = detailed.Category,
            MaterialType = detailed.MaterialType,
            Description = detailed.Description,
            SafetyInstructions = detailed.SafetyInstructions,
            AgeMinMonths = detailed.AgeMinMonths,
            AgeMaxMonths = detailed.AgeMaxMonths,
            AllowPurchase = off?.AllowPurchase ?? (off?.PurchasePrice > 0),
            PurchasePrice = off?.PurchasePrice ?? 0,
            AllowBorrow = off?.AllowBorrow ?? (off?.RentalFee > 0),
            RentalFee = off?.RentalFee ?? 0,
            RefundableDeposit = off?.RefundableDeposit ?? 0,
            DefaultLoanDurationDays = off?.DefaultLoanDurationDays ?? 14,
            MaxLoanDurationDays = off?.MaxLoanDurationDays ?? 30,
            IsActive = newStatus
        };

        await MaterialService.SaveProductAsync(req);
        _products = await MaterialService.GetCatalogProductsAsync(activeOnly: false);
        await LoadInventoryDataAsync();
    }

    private async Task OpenLedgerDrawer(InventoryBalance bal)
    {
        _ledgerModalItem = bal;
        _ledgerHistory = await MaterialService.GetProductLedgerHistoryAsync(bal.MaterialProductId);
    }
}

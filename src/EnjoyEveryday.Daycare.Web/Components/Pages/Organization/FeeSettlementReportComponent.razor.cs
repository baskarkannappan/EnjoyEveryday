using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.Shared.Auth;
using EnjoyEveryday.UI.Shared.Services;

namespace EnjoyEveryday.Daycare.Web.Components.Pages.Organization;

public partial class FeeSettlementReportComponent
{
    [Parameter] public Guid? InitialBranchId { get; set; }

    private bool _isLoading = true;
    private bool _isOrgAdmin;
    private string _currency = "INR";
    private string? _bannerMessage;
    private bool _bannerSuccess = true;

    private Guid? _selectedBranchId;
    private Guid? _selectedClassroomId;
    private string? _selectedBillingPeriod;
    private string _selectedStatus = SettlementStatuses.All;
    private string _searchQuery = string.Empty;

    private List<Branch> _availableBranches = new();
    private List<Classroom> _allClassrooms = new();
    private List<Classroom> _filteredClassrooms = new();
    private List<string> _availablePeriods = new();
    private HashSet<Guid> _expandedChildIds = new();
    private Dictionary<Guid, string> _childDetailTabs = new();

    private string GetChildDetailTab(Guid childId)
    {
        return _childDetailTabs.TryGetValue(childId, out var tab) ? tab : "Invoices";
    }

    private void SetChildDetailTab(Guid childId, string tab)
    {
        _childDetailTabs[childId] = tab;
    }

    private static string FormatAuditor(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return "-";
        return val.Contains('@') ? val.Split('@')[0] : val;
    }

    private static string FormatMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return "Payment";
        return method.ToLowerInvariant() switch
        {
            "qrcode" or "qr" => "📱 QR Code",
            "banktransfer" or "bank" => "🏦 Bank Transfer",
            "cash" => "💵 Cash",
            "razorpay" or "gateway" or "online" => "💳 Online Gateway",
            _ => method
        };
    }

    private static string CleanNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return string.Empty;
        var trimmed = note.Trim();
        if (trimmed.Length > 55)
            return trimmed[..52] + "...";
        return trimmed;
    }

    private FeeSettlementReportSummary _summary = new();

    protected override async Task OnInitializedAsync()
    {
        _selectedBranchId = InitialBranchId;
        BuildAvailablePeriods();
        await InitializeScopeAndFiltersAsync();
        await LoadReportAsync();
    }

    private void BuildAvailablePeriods()
    {
        var periods = new List<string>();
        var now = DateTime.Today;
        for (int i = 0; i < 12; i++)
        {
            periods.Add(now.AddMonths(-i).ToString("yyyy-MM"));
        }
        _availablePeriods = periods;
        // Default to All Periods (Cumulative) so admins see the complete financial standing, partial settlements, and open balances across all months
        _selectedBillingPeriod = "";
    }

    private async Task InitializeScopeAndFiltersAsync()
    {
        try
        {
            var scope = await FeeService.GetCurrentFinancialScopeAsync();
            _isOrgAdmin = scope.IsOrgAdmin;

            var tenantId = TenantContext.TenantId;
            var orgId = UserContext.OrganizationId ?? Guid.Empty;
            if (orgId != Guid.Empty)
            {
                var branches = (await BranchRepo.GetByOrganizationIdAsync(tenantId, orgId)).ToList();
                _availableBranches = _isOrgAdmin
                    ? branches
                    : branches.Where(b => scope.AllowedBranchIds.Contains(b.Id)).ToList();

                if (!_isOrgAdmin && !_selectedBranchId.HasValue && _availableBranches.Any())
                {
                    _selectedBranchId = _availableBranches[0].Id;
                }

                _allClassrooms = (await ClassroomService.GetClassroomsByOrganizationAsync(orgId)).ToList();
                FilterClassrooms();
            }
        }
        catch (Exception ex)
        {
            _bannerSuccess = false;
            _bannerMessage = ex.Message;
        }
    }

    private void FilterClassrooms()
    {
        if (_selectedBranchId.HasValue)
        {
            _filteredClassrooms = _allClassrooms.Where(c => c.BranchId == _selectedBranchId.Value).ToList();
            if (_selectedClassroomId.HasValue && !_filteredClassrooms.Any(c => c.Id == _selectedClassroomId.Value))
            {
                _selectedClassroomId = null;
            }
        }
        else
        {
            _filteredClassrooms = _allClassrooms.ToList();
        }
    }

    private async Task OnBranchChangedAsync(ChangeEventArgs e)
    {
        _selectedBranchId = Guid.TryParse(e.Value?.ToString(), out var bid) ? bid : null;
        FilterClassrooms();
        await LoadReportAsync();
    }

    private async Task OnClassroomChangedAsync(ChangeEventArgs e)
    {
        _selectedClassroomId = Guid.TryParse(e.Value?.ToString(), out var cid) ? cid : null;
        await LoadReportAsync();
    }

    private async Task OnPeriodChangedAsync(ChangeEventArgs e)
    {
        _selectedBillingPeriod = e.Value?.ToString();
        await LoadReportAsync();
    }

    private async Task OnStatusChangedAsync(ChangeEventArgs e)
    {
        _selectedStatus = e.Value?.ToString() ?? SettlementStatuses.All;
        await LoadReportAsync();
    }

    private async Task SetStatusQuick(string status)
    {
        _selectedStatus = status;
        await LoadReportAsync();
    }

    private async Task OnSearchInputAsync(ChangeEventArgs e)
    {
        _searchQuery = e.Value?.ToString() ?? string.Empty;
        await LoadReportAsync();
    }

    private async Task ClearSearchAsync()
    {
        _searchQuery = string.Empty;
        await LoadReportAsync();
    }

    private void ToggleExpand(Guid childId)
    {
        if (_expandedChildIds.Contains(childId))
            _expandedChildIds.Remove(childId);
        else
            _expandedChildIds.Add(childId);
    }

    private async Task LoadReportAsync()
    {
        _isLoading = true;
        _bannerMessage = null;
        try
        {
            _summary = await FeeService.GetFeeSettlementReportAsync(
                _selectedBranchId,
                _selectedClassroomId,
                null,
                _selectedBillingPeriod,
                _selectedStatus,
                _searchQuery);
        }
        catch (Exception ex)
        {
            _bannerSuccess = false;
            _bannerMessage = ex.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task VerifyPaymentInlineAsync(Guid paymentId)
    {
        try
        {
            var res = await FeeService.VerifyPaymentAsync(paymentId, "Verified via settlement report");
            _bannerSuccess = res.Success;
            _bannerMessage = res.Message;
            await LoadReportAsync();
        }
        catch (Exception ex)
        {
            _bannerSuccess = false;
            _bannerMessage = ex.Message;
        }
    }

    private async Task RejectPaymentInlineAsync(Guid paymentId)
    {
        try
        {
            var res = await FeeService.RejectPaymentAsync(paymentId, "Rejected via settlement report");
            _bannerSuccess = res.Success;
            _bannerMessage = res.Message;
            await LoadReportAsync();
        }
        catch (Exception ex)
        {
            _bannerSuccess = false;
            _bannerMessage = ex.Message;
        }
    }

    private string FormatMoney(decimal amount, string? currency = null)
    {
        var curr = string.IsNullOrWhiteSpace(currency) ? _currency : currency;
        return CurrencyService.Format(amount, curr, forceDecimals: true);
    }

    private string FormatDate(DateTimeOffset date, bool includeTime = false)
    {
        if (date == default || date.Year <= 1) return "-";
        var local = date.ToLocalTime();
        return includeTime ? local.ToString("d MMM yyyy HH:mm") : local.ToString("d MMM yyyy");
    }

    private async Task ExportCsvAsync()
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Child Name,Classroom,Branch,Settlement Status,Invoiced Amount,Paid Amount,Balance Due,Pending Amount,Currency");
            foreach (var item in _summary.Items)
            {
                var cleanName = EscapeCsv(item.ChildName);
                var cleanRoom = EscapeCsv(item.ClassroomName);
                var cleanBranch = EscapeCsv(item.BranchName);
                sb.AppendLine($"{cleanName},{cleanRoom},{cleanBranch},{item.SettlementStatus},{item.TotalInvoiced},{item.TotalPaid},{item.BalanceDue},{item.PendingVerificationAmount},{item.Currency}");
            }

            var csvData = sb.ToString();
            var fileName = $"Fee_Settlement_Report_{DateTime.Today:yyyyMMdd}.csv";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csvData);
            var base64 = Convert.ToBase64String(bytes);
            await JS.InvokeVoidAsync("eval", $"var a = document.createElement('a'); a.href = 'data:text/csv;base64,{base64}'; a.download = '{fileName}'; document.body.appendChild(a); a.click(); document.body.removeChild(a);");
        }
        catch (Exception ex)
        {
            _bannerSuccess = false;
            _bannerMessage = $"Export failed: {ex.Message}";
        }
    }

    private static string EscapeCsv(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return "\"\"";
        return $"\"{val.Replace("\"", "\"\"")}\"";
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "C";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private RenderFragment RenderStatusBadge(string status) => builder =>
    {
        var css = status switch
        {
            SettlementStatuses.Paid => "ee-badge-green",
            SettlementStatuses.PartiallyPaid => "ee-badge-orange",
            SettlementStatuses.PendingVerification => "ee-badge-amber",
            SettlementStatuses.Overdue => "ee-badge-red",
            SettlementStatuses.Unpaid => "ee-badge-red",
            _ => "ee-badge-gray"
        };
        var label = status switch
        {
            SettlementStatuses.Paid => "Paid",
            SettlementStatuses.PartiallyPaid => "Partially Paid",
            SettlementStatuses.PendingVerification => "Pending Verif",
            SettlementStatuses.Overdue => "Overdue",
            SettlementStatuses.Unpaid => "Unpaid",
            SettlementStatuses.NotInvoiced => "Not Invoiced",
            _ => status
        };
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", css);
        builder.AddContent(2, label);
        builder.CloseElement();
    };

    private RenderFragment RenderInvoiceStatusBadge(string status) => builder =>
    {
        var css = status switch
        {
            "Paid" => "ee-badge-green",
            "PartiallyPaid" => "ee-badge-orange",
            "Issued" or "Overdue" => "ee-badge-red",
            _ => "ee-sub-pill"
        };
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", css);
        builder.AddContent(2, status);
        builder.CloseElement();
    };
}

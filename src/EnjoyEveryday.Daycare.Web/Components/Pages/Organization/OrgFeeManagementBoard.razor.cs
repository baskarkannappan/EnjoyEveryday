using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.UI.Shared.Components;
using EnjoyEveryday.UI.Shared.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace EnjoyEveryday.Daycare.Web.Components.Pages.Organization;

public partial class OrgFeeManagementBoard
{
    private string _activeTab = "Overview";
    private bool _isLoading = true;
    private bool _isOrgAdmin;
    private Guid? _selectedBranchId;
    private string? _bannerMessage;
    private bool _bannerSuccess = true;

    private List<Branch> _branches = new();
    private List<Classroom> _classrooms = new();
    private List<Child> _children = new();
    private OrgFeeDashboardSummary _dashboard = new();
    private List<OrgFeePlan> _feePlans = new();
    private List<OrgTariffRule> _tariffRules = new();
    private List<Invoice> _tuitionInvoices = new();
    private List<Payment> _paymentsQueue = new();

    private UpsertFeePlanRequest _planForm = new();
    private UpsertTariffRuleRequest _ruleForm = new();
    private AssignChildFeeRequest _assignForm = new();
    private List<FamilyBillingContract> _selectedChildContracts = new();
    private List<ChildFeeAdjustment> _selectedChildAdjustments = new();
    private decimal _adjustAmount;
    private string _adjustReason = string.Empty;

    private GenerateChildTuitionInvoiceRequest _invoiceForm = new() { BillingPeriod = DateTime.Today.ToString("yyyy-MM") };
    private DateTime _invoiceDueDate = DateTime.Today.AddDays(14);
    private bool _enableInstallments;
    private int _installmentCount = 2;
    private List<CreateInstallmentItemRequest> _installmentRows = new();

    private string _verifyRemarks = string.Empty;
    private Invoice? _cashInvoice;
    private RecordDaycareCashPaymentRequest _cashRequest = new();

    private OrgBankAccount _bankForm = new();
    private List<OrgPaymentQrCode> _qrCodes = new();
    private int _qrSlot = 1;
    private string _qrDisplayName = string.Empty;
    private string _qrPurpose = string.Empty;
    private string _qrDataUri = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        _isLoading = true;
        try
        {
            var scope = await FeeService.GetCurrentFinancialScopeAsync();
            _isOrgAdmin = scope.IsOrgAdmin;

            var tenantId = TenantContext.TenantId;
            var orgId = UserContext.OrganizationId ?? Guid.Empty;
            if (orgId != Guid.Empty)
            {
                var allBranches = (await BranchRepo.GetByOrganizationIdAsync(tenantId, orgId)).ToList();
                _branches = _isOrgAdmin ? allBranches : allBranches.Where(b => scope.AllowedBranchIds.Contains(b.Id)).ToList();
                if (!_isOrgAdmin && !_selectedBranchId.HasValue && _branches.Any())
                    _selectedBranchId = _branches[0].Id;

                _classrooms = (await ClassroomService.GetClassroomsByOrganizationAsync(orgId)).ToList();
                _children = (await ChildRepo.GetAllAsync(tenantId, orgId)).Where(c => c.IsActive).ToList();
            }

            _dashboard = await FeeService.GetDashboardSummaryAsync(_selectedBranchId);
            _feePlans = await FeeService.GetFeePlansAsync();
            _tariffRules = await FeeService.GetTariffRulesAsync();
            _tuitionInvoices = await FeeService.GetOrgTuitionInvoicesAsync(_selectedBranchId);
            _paymentsQueue = await FeeService.GetPaymentsQueueAsync(_selectedBranchId);

            if (_isOrgAdmin)
            {
                _bankForm = await FeeService.GetBankAccountAsync() ?? new OrgBankAccount();
                _qrCodes = await FeeService.GetQrCodesAsync();
            }
        }
        catch (Exception ex)
        {
            SetBanner(false, ex.Message);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task OnBranchFilterChanged(ChangeEventArgs e)
    {
        _selectedBranchId = Guid.TryParse(e.Value?.ToString(), out var bid) ? bid : null;
        await LoadAllAsync();
    }

    private async Task SaveFeePlanAsync()
    {
        try
        {
            await FeeService.UpsertFeePlanAsync(_planForm);
            _planForm = new();
            _feePlans = await FeeService.GetFeePlansAsync();
            SetBanner(true, "Fee plan saved.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task ToggleFeePlanAsync(OrgFeePlan p)
    {
        await FeeService.ToggleFeePlanActiveAsync(p.Id, !p.IsActive);
        _feePlans = await FeeService.GetFeePlansAsync();
    }

    private async Task SaveTariffRuleAsync()
    {
        try
        {
            await FeeService.UpsertTariffRuleAsync(_ruleForm);
            _ruleForm = new();
            _tariffRules = await FeeService.GetTariffRulesAsync();
            SetBanner(true, "Tariff rule saved.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task ToggleTariffRuleAsync(OrgTariffRule r)
    {
        await FeeService.ToggleTariffRuleActiveAsync(r.Id, !r.IsActive);
        _tariffRules = await FeeService.GetTariffRulesAsync();
    }

    private async Task OnAssignChildSelectedAsync(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out var cid))
        {
            _assignForm.ChildId = cid;
            _selectedChildContracts = await FeeService.GetChildContractsAsync(cid);
            _selectedChildAdjustments = await FeeService.GetChildFeeAdjustmentsAsync(cid);
        }
    }

    private async Task PreviewChildTariffAsync()
    {
        try
        {
            var ch = _children.FirstOrDefault(x => x.Id == _assignForm.ChildId);
            var branchId = _classrooms.FirstOrDefault(r => r.Id == ch?.ClassroomId)?.BranchId;
            var res = await FeeService.ResolveChildTariffAsync(branchId, ch?.ClassroomId, ch?.Id, null, _assignForm.FeePeriod);
            if (res == null)
            {
                SetBanner(false, "No matching tariff rule found for this child/branch/frequency.");
                return;
            }
            _assignForm.AgreedAmount = res.Amount;
            _assignForm.FeePlanId = res.FeePlanId;
            _assignForm.TariffRuleId = res.RuleId;
            SetBanner(true, $"Resolved tariff: {res.PlanName} ({res.ScopeType}) = {CurrencyService.Format(res.Amount, res.Currency)}");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task AssignChildFeeAsync()
    {
        try
        {
            await FeeService.AssignChildFeeAsync(_assignForm);
            _selectedChildContracts = await FeeService.GetChildContractsAsync(_assignForm.ChildId);
            SetBanner(true, "Child fee arrangement assigned.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task AdjustFlatRateAsync(Guid contractId)
    {
        try
        {
            await FeeService.AdjustChildFlatRateAsync(new AdjustChildFlatRateRequest
            {
                ContractId = contractId,
                NewAmount = _adjustAmount,
                Reason = _adjustReason
            });
            _adjustReason = string.Empty;
            _selectedChildContracts = await FeeService.GetChildContractsAsync(_assignForm.ChildId);
            _selectedChildAdjustments = await FeeService.GetChildFeeAdjustmentsAsync(_assignForm.ChildId);
            SetBanner(true, "Flat-rate adjusted and logged in audit history.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task OnInvoiceChildSelectedAsync(ChangeEventArgs e)
    {
        if (!Guid.TryParse(e.Value?.ToString(), out var cid) || cid == Guid.Empty)
        {
            _invoiceForm.ChildId = Guid.Empty;
            return;
        }

        _invoiceForm.ChildId = cid;
        var ch = _children.FirstOrDefault(x => x.Id == cid);
        var branchId = _classrooms.FirstOrDefault(r => r.Id == ch?.ClassroomId)?.BranchId;
        _invoiceForm.BranchId = branchId;
        if (string.IsNullOrWhiteSpace(_invoiceForm.BillingPeriod))
            _invoiceForm.BillingPeriod = DateTime.Today.ToString("yyyy-MM");

        var contracts = await FeeService.GetChildContractsAsync(cid);
        var active = contracts.FirstOrDefault(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase));
        if (active != null)
        {
            _invoiceForm.ContractId = active.Id;
            _invoiceForm.FeeMethod = active.FeeMethod;
            _invoiceForm.Amount = active.EffectiveRate;
            _invoiceForm.FeePlanName = $"{active.FeeMethod} ({active.FeePeriod})";
            _invoiceForm.Description = $"{active.FeePeriod} Tuition — {ch?.FirstName} {ch?.LastName}".Trim();
        }
        else
        {
            var resolved = await FeeService.ResolveChildTariffAsync(branchId, ch?.ClassroomId, cid, null, null);
            if (resolved != null)
            {
                _invoiceForm.ContractId = null;
                _invoiceForm.FeeMethod = FeeMethods.TariffBased;
                _invoiceForm.Amount = resolved.Amount;
                _invoiceForm.FeePlanName = resolved.PlanName;
                _invoiceForm.Description = $"{resolved.PlanName} ({resolved.BillingFrequency}) — {ch?.FirstName} {ch?.LastName}".Trim();
            }
        }

        if (_enableInstallments) RebuildInstallmentRows();
    }

    private void RebuildInstallmentRows()
    {
        _installmentRows.Clear();
        if (!_enableInstallments) return;

        var count = Math.Clamp(_installmentCount, 2, 12);
        var total = Math.Max(0m, _invoiceForm.Amount);
        var per = count > 0 ? Math.Round(total / count, 2) : 0m;
        decimal running = 0m;

        for (int i = 1; i <= count; i++)
        {
            var amt = i == count ? Math.Max(0m, total - running) : per;
            running += amt;
            _installmentRows.Add(new CreateInstallmentItemRequest
            {
                InstallmentNumber = i,
                AmountDue = amt,
                DueDate = DateTime.Today.AddMonths(i - 1).AddDays(7)
            });
        }
    }

    private async Task GenerateInvoiceAsync(bool saveAsDraft)
    {
        try
        {
            _invoiceForm.SaveAsDraft = saveAsDraft;
            _invoiceForm.DueDate = new DateTimeOffset(DateTime.SpecifyKind(_invoiceDueDate.Date, DateTimeKind.Utc), TimeSpan.Zero);
            _invoiceForm.Installments = _enableInstallments
                ? _installmentRows.Where(r => r.AmountDue > 0).ToList()
                : new List<CreateInstallmentItemRequest>();
            await FeeService.GenerateChildTuitionInvoiceAsync(_invoiceForm);
            _invoiceForm = new() { BillingPeriod = DateTime.Today.ToString("yyyy-MM") };
            _installmentRows.Clear();
            _enableInstallments = false;
            await LoadAllAsync();
            SetBanner(true, saveAsDraft ? "Invoice saved as Draft." : "Tuition invoice published with bank/QR snapshot.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task PublishInvoiceAsync(Invoice inv)
    {
        try
        {
            var ok = await FeeService.PublishTuitionInvoiceAsync(inv.Id);
            await LoadAllAsync();
            SetBanner(ok, ok ? $"Invoice #{inv.InvoiceNumber} published to Parent Portal." : "Could not publish invoice.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private void OpenCashModal(Invoice inv)
    {
        _cashInvoice = inv;
        _cashRequest = new RecordDaycareCashPaymentRequest { InvoiceId = inv.Id, Amount = inv.BalanceDue };
    }

    private async Task SubmitCashPaymentAsync()
    {
        try
        {
            await FeeService.RecordDirectCashPaymentAsync(_cashRequest);
            _cashInvoice = null;
            await LoadAllAsync();
            SetBanner(true, "Cash payment recorded and verified.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task QuickWaiveAsync(Invoice inv)
    {
        try
        {
            var waiveAmt = Math.Min(inv.BalanceDue, 500m);
            await FeeService.ApplyInvoiceAdjustmentAsync(new ApplyInvoiceAdjustmentRequest
            {
                InvoiceId = inv.Id,
                AdjustmentType = "Credit",
                AdjustmentAmount = waiveAmt,
                Reason = "Authorized administrative fee adjustment"
            });
            await LoadAllAsync();
            SetBanner(true, $"Applied adjustment of {CurrencyService.Format(waiveAmt, inv.Currency)}.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task CancelInvoiceAsync(Invoice inv)
    {
        try
        {
            await FeeService.CancelTuitionInvoiceAsync(inv.Id, "Cancelled by administrator");
            await LoadAllAsync();
            SetBanner(true, $"Invoice #{inv.InvoiceNumber} cancelled.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task VerifyPaymentAsync(Guid paymentId)
    {
        try
        {
            var res = await FeeService.VerifyPaymentAsync(paymentId, _verifyRemarks);
            _verifyRemarks = string.Empty;
            await LoadAllAsync();
            SetBanner(res.Success, res.Message);
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task RejectPaymentAsync(Guid paymentId)
    {
        try
        {
            var res = await FeeService.RejectPaymentAsync(paymentId, string.IsNullOrWhiteSpace(_verifyRemarks) ? "Unmatched transaction reference" : _verifyRemarks);
            _verifyRemarks = string.Empty;
            await LoadAllAsync();
            SetBanner(res.Success, res.Message);
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task SaveBankAccountAsync()
    {
        try
        {
            _bankForm = await FeeService.UpsertBankAccountAsync(_bankForm);
            SetBanner(true, "Organization bank details saved.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task OnQrFileSelectedAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null) return;
        using var stream = file.OpenReadStream(2 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        _qrDataUri = $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
    }

    private async Task SaveQrCodeAsync()
    {
        try
        {
            await FeeService.UpsertQrCodeAsync(new OrgPaymentQrCode
            {
                SlotIndex = _qrSlot,
                DisplayName = _qrDisplayName,
                PaymentPurpose = _qrPurpose,
                QrImageDataUri = _qrDataUri,
                IsActive = true
            });
            _qrCodes = await FeeService.GetQrCodesAsync();
            SetBanner(true, $"QR code saved in slot {_qrSlot}.");
        }
        catch (Exception ex) { SetBanner(false, ex.Message); }
    }

    private async Task DeleteQrAsync(int slot)
    {
        await FeeService.DeleteQrCodeAsync(slot);
        _qrCodes = await FeeService.GetQrCodesAsync();
    }

    private void SetBanner(bool ok, string msg)
    {
        _bannerSuccess = ok;
        _bannerMessage = msg;
    }
}

using System.Collections.Concurrent;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public partial class PlatformAdminService
{
    #region Plan CRUD

    public Task<List<SubscriptionPlan>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var plans = _plans.Values.AsEnumerable();
        if (!includeInactive)
        {
            plans = plans.Where(p => p.IsActive);
        }

        foreach (var p in plans)
        {
            p.Tariffs = _tariffs.Values.Where(t => t.PlanId == p.Id && t.IsActive).OrderBy(t => t.Price).ToList();
        }

        return Task.FromResult(plans.OrderBy(p => p.BasePrice).ToList());
    }

    public Task<SubscriptionPlan?> GetPlanByIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (_plans.TryGetValue(planId, out var plan))
        {
            plan.Tariffs = _tariffs.Values.Where(t => t.PlanId == plan.Id && t.IsActive).OrderBy(t => t.Price).ToList();
            return Task.FromResult<SubscriptionPlan?>(plan);
        }
        return Task.FromResult<SubscriptionPlan?>(null);
    }

    public Task<SubscriptionPlan> CreatePlanAsync(
        string name,
        string code,
        string description,
        decimal basePrice,
        int maxBranches,
        int maxClassrooms,
        int maxChildren,
        bool includesAi,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = id,
            Name = name,
            Code = code.ToUpperInvariant().Trim(),
            Description = description,
            BasePrice = basePrice,
            MaxBranches = maxBranches,
            MaxClassrooms = maxClassrooms,
            MaxChildren = maxChildren,
            IncludesAiFeatures = includesAi,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Create a default monthly tariff for this plan
        var tariffId = Guid.NewGuid();
        var defaultTariff = new Tariff
        {
            Id = tariffId,
            PlanId = id,
            Name = "Standard Monthly",
            Frequency = BillingFrequency.Monthly,
            Price = basePrice,
            DiscountPercentage = 0,
            EffectiveDate = DateTimeOffset.UtcNow,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _tariffs[tariffId] = defaultTariff;
        plan.Tariffs.Add(defaultTariff);
        _plans[id] = plan;

        AddAuditLog("Plan Created", $"New subscription plan '{name}' ({code}) created at {basePrice:C0}/mo.", "Info", id);

        return Task.FromResult(plan);
    }

    public async Task<bool> UpdatePlanAsync(
        Guid planId,
        string name,
        string description,
        decimal basePrice,
        int maxBranches,
        int maxClassrooms,
        int maxChildren,
        bool includesAi,
        CancellationToken cancellationToken = default)
    {
        if (_plans.TryGetValue(planId, out var plan))
        {
            var oldName = plan.Name;
            var oldPrice = plan.BasePrice;

            plan.Name = name;
            plan.Description = description;
            plan.BasePrice = basePrice;
            plan.MaxBranches = maxBranches;
            plan.MaxClassrooms = maxClassrooms;
            plan.MaxChildren = maxChildren;
            plan.IncludesAiFeatures = includesAi;
            plan.UpdatedAt = DateTimeOffset.UtcNow;

            AddAuditLog("Plan Updated", $"Subscription plan '{name}' updated (Price: {basePrice:C0}, AI: {includesAi}).", "Info", planId);

            // Automatically notify active subscribers of this plan
            var subscribers = _organizations.Values
                .Where(o => o.PlanId == planId && (o.AccountStatus == AccountStatus.Active || o.AccountStatus == AccountStatus.Trial))
                .ToList();

            if (_emailNotificationService != null && subscribers.Count > 0)
            {
                var daycareUrl = GetDaycarePortalUrl();
                var billingUrl = $"{daycareUrl}/billing";
                var summaryHtml = $@"
                    <div style='margin-bottom: 8px;'><strong>Updated Plan:</strong> {name}</div>
                    <div style='margin-bottom: 8px;'><strong>Base Price:</strong> ₹{basePrice:F2}/month</div>
                    <div style='margin-bottom: 8px;'><strong>Capacity Limits:</strong> Up to {maxChildren} children, {maxClassrooms} classrooms, {maxBranches} branches</div>
                    <div style='margin-bottom: 8px;'><strong>Experience Studio AI:</strong> {(includesAi ? "Included ✨" : "Standard")}</div>
                    <div style='margin-top: 10px; font-size: 13px; color: #6c7870;'>{description}</div>";

                int sentCount = 0;
                foreach (var sub in subscribers)
                {
                    if (string.IsNullOrWhiteSpace(sub.AdminEmail)) continue;
                    try
                    {
                        sub.PlanName = name;
                        await _emailNotificationService.SendPlanOrTariffUpdateNoticeAsync(
                            sub.AdminEmail,
                            sub.Name,
                            name,
                            $"Subscription Plan Terms Updated: {name}",
                            summaryHtml,
                            "Effective next billing cycle",
                            billingUrl,
                            sub.TenantId,
                            cancellationToken);
                        sentCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send plan update email to subscriber {Email}", sub.AdminEmail);
                    }
                }

                if (sentCount > 0)
                {
                    AddAuditLog("Plan Subscribers Notified", $"Dispatched plan update notice to {sentCount} active subscribers of '{name}'.", "Info", planId);
                }
            }

            return true;
        }
        return false;
    }

    public Task<bool> DeactivatePlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (_plans.TryGetValue(planId, out var plan))
        {
            plan.IsActive = false;
            plan.UpdatedAt = DateTimeOffset.UtcNow;

            AddAuditLog("Plan Deactivated", $"Subscription plan '{plan.Name}' archived/deactivated.", "Warning", planId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    #endregion

    #region Tariff CRUD

    public Task<List<Tariff>> GetTariffsByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var tariffs = _tariffs.Values.Where(t => t.PlanId == planId && t.IsActive).OrderBy(t => t.Price).ToList();
        return Task.FromResult(tariffs);
    }

    public Task<Tariff> CreateTariffAsync(
        Guid planId,
        string name,
        BillingFrequency frequency,
        decimal price,
        decimal discountPercentage,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var tariff = new Tariff
        {
            Id = id,
            PlanId = planId,
            Name = name,
            Frequency = frequency,
            Price = price,
            DiscountPercentage = discountPercentage,
            EffectiveDate = DateTimeOffset.UtcNow,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _tariffs[id] = tariff;

        if (_plans.TryGetValue(planId, out var plan))
        {
            plan.Tariffs.Add(tariff);
            AddAuditLog("Tariff Created", $"New tariff '{name}' ({frequency}: {price:C0}) added to plan '{plan.Name}'.", "Info", id);
        }

        return Task.FromResult(tariff);
    }

    public async Task<bool> UpdateTariffAsync(
        Guid tariffId,
        string name,
        BillingFrequency frequency,
        decimal price,
        decimal discountPercentage,
        CancellationToken cancellationToken = default)
    {
        if (_tariffs.TryGetValue(tariffId, out var tariff))
        {
            tariff.Name = name;
            tariff.Frequency = frequency;
            tariff.Price = price;
            tariff.DiscountPercentage = discountPercentage;
            tariff.UpdatedAt = DateTimeOffset.UtcNow;

            AddAuditLog("Tariff Updated", $"Tariff '{name}' updated to {price:C0} ({frequency}).", "Info", tariffId);

            var subscribers = _organizations.Values
                .Where(o => o.TariffId == tariffId && (o.AccountStatus == AccountStatus.Active || o.AccountStatus == AccountStatus.Trial))
                .ToList();

            if (_emailNotificationService != null && subscribers.Count > 0)
            {
                var daycareUrl = GetDaycarePortalUrl();
                var billingUrl = $"{daycareUrl}/billing";
                var summaryHtml = $@"
                    <div style='margin-bottom: 8px;'><strong>Tariff Schedule:</strong> {name}</div>
                    <div style='margin-bottom: 8px;'><strong>Billing Frequency:</strong> {frequency}</div>
                    <div style='margin-bottom: 8px;'><strong>Rate:</strong> ₹{price:F2}</div>
                    {(discountPercentage > 0 ? $"<div style='margin-bottom: 8px;'><strong>Applied Discount:</strong> {discountPercentage}%</div>" : "")}";

                foreach (var sub in subscribers)
                {
                    if (string.IsNullOrWhiteSpace(sub.AdminEmail)) continue;
                    try
                    {
                        sub.TariffName = name;
                        sub.MonthlyRate = price;
                        await _emailNotificationService.SendPlanOrTariffUpdateNoticeAsync(
                            sub.AdminEmail,
                            sub.Name,
                            sub.PlanName,
                            $"Billing Schedule Updated: {name}",
                            summaryHtml,
                            "Effective next billing cycle",
                            billingUrl,
                            sub.TenantId,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send tariff update email to {Email}", sub.AdminEmail);
                    }
                }
            }

            return true;
        }
        return false;
    }

    public Task<bool> DeactivateTariffAsync(Guid tariffId, CancellationToken cancellationToken = default)
    {
        if (_tariffs.TryGetValue(tariffId, out var tariff))
        {
            tariff.IsActive = false;
            tariff.UpdatedAt = DateTimeOffset.UtcNow;

            AddAuditLog("Tariff Deactivated", $"Tariff '{tariff.Name}' deactivated.", "Warning", tariffId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public async Task<bool> AssignPlanAndTariffToOrganizationAsync(
        Guid organizationId,
        Guid planId,
        Guid? tariffId,
        CancellationToken cancellationToken = default)
    {
        if (_organizations.TryGetValue(organizationId, out var org) && _plans.TryGetValue(planId, out var plan))
        {
            Tariff? tariff = null;
            if (tariffId.HasValue)
            {
                _tariffs.TryGetValue(tariffId.Value, out tariff);
            }
            tariff ??= _tariffs.Values.FirstOrDefault(t => t.PlanId == planId && t.IsActive);

            var oldPlan = org.PlanName;
            org.PlanId = plan.Id;
            org.PlanName = plan.Name;
            org.TariffId = tariff?.Id;
            org.TariffName = tariff?.Name ?? "Standard";
            org.MonthlyRate = tariff != null ? tariff.Price : plan.BasePrice;

            if (_platformAdminRepository != null)
            {
                try
                {
                    await _platformAdminRepository.UpdateOrganizationPlanQuotaAsync(
                        organizationId,
                        plan.Id,
                        plan.Code,
                        plan.Name,
                        plan.MaxBranches,
                        plan.MaxClassrooms,
                        plan.MaxChildren,
                        plan.IncludesAiFeatures,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to persist organization plan quotas to database for {OrgId}", organizationId);
                }
            }

            AddAuditLog("Plan/Tariff Changed", $"Organization '{org.Name}' changed from '{oldPlan}' to '{plan.Name}' ({org.TariffName} - {org.MonthlyRate:C0}).", "Info", organizationId);

            if (_emailNotificationService != null && !string.IsNullOrWhiteSpace(org.AdminEmail))
            {
                try
                {
                    var daycareUrl = GetDaycarePortalUrl();
                    var billingUrl = $"{daycareUrl}/billing";
                    var summaryHtml = $@"
                        <div style='margin-bottom: 8px;'><strong>New Plan:</strong> {plan.Name} ({plan.Code})</div>
                        <div style='margin-bottom: 8px;'><strong>Selected Schedule:</strong> {org.TariffName}</div>
                        <div style='margin-bottom: 8px;'><strong>Monthly Rate:</strong> ₹{org.MonthlyRate:F2}/month</div>
                        <div style='margin-bottom: 8px;'><strong>Capacity Limits:</strong> Up to {plan.MaxChildren} children, {plan.MaxClassrooms} classrooms, {plan.MaxBranches} branches</div>
                        <div style='margin-bottom: 8px;'><strong>Experience Studio AI:</strong> {(plan.IncludesAiFeatures ? "Enabled ✨" : "Standard")}</div>
                        <div style='margin-top: 10px; font-size: 13px; color: #6c7870;'>{plan.Description}</div>";

                    await _emailNotificationService.SendPlanOrTariffUpdateNoticeAsync(
                        org.AdminEmail,
                        org.Name,
                        plan.Name,
                        $"Subscription Plan Updated to {plan.Name} ({org.TariffName})",
                        summaryHtml,
                        "Effective Immediately",
                        billingUrl,
                        org.TenantId,
                        cancellationToken);

                    AddAuditLog("Plan Update Email Sent", $"Dispatched plan assignment notification email to {org.AdminEmail} for '{org.Name}'.", "Info", organizationId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to dispatch plan update email to {Email}", org.AdminEmail);
                }
            }

            return true;
        }
        return false;
    }

    #endregion

    #region Regional Pricing & Tariff Layers

    public async Task<List<PricingRegion>> GetPricingRegionsAsync(CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            try
            {
                return await _platformAdminRepository.GetPricingRegionsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load pricing regions from database.");
            }
        }
        return new List<PricingRegion>();
    }

    public async Task<PricingRegion?> ResolvePricingRegionByLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null && !string.IsNullOrWhiteSpace(location))
        {
            try
            {
                return await _platformAdminRepository.ResolvePricingRegionByLocationAsync(location, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve pricing region for location '{Location}'", location);
            }
        }
        return null;
    }

    public async Task<List<RegionPlanTariff>> GetRegionPlanTariffsAsync(Guid regionId, CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            try
            {
                return await _platformAdminRepository.GetRegionPlanTariffsAsync(regionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load region plan tariffs for {RegionId}", regionId);
            }
        }
        return new List<RegionPlanTariff>();
    }

    public async Task SavePricingRegionAsync(PricingRegion region, CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            await _platformAdminRepository.SavePricingRegionAsync(region, cancellationToken);
            AddAuditLog("Pricing Region Saved", $"Pricing region '{region.Name}' ({region.Code}) saved with {region.MatchedCities.Count} cities.", "Info");
        }
    }

    public async Task SaveRegionPlanTariffAsync(RegionPlanTariff tariff, CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            await _platformAdminRepository.SaveRegionPlanTariffAsync(tariff, cancellationToken);
            AddAuditLog("Region Tariff Saved", $"Tariff '{tariff.Name}' ({tariff.Price:C0}/{tariff.Frequency}) saved for region {tariff.RegionId}.", "Info");
        }
    }

    public async Task<bool> AssignRegionalPlanToOrganizationAsync(
        Guid organizationId,
        Guid regionId,
        Guid planId,
        Guid? tariffId,
        string? currency = null,
        CancellationToken cancellationToken = default)
    {
        if (_organizations.TryGetValue(organizationId, out var org) && _plans.TryGetValue(planId, out var plan))
        {
            var oldPlan = org.PlanName;
            var oldRegion = org.PricingRegionName ?? "Standard";

            PricingRegion? region = null;
            if (_platformAdminRepository != null)
            {
                var regions = await _platformAdminRepository.GetPricingRegionsAsync(cancellationToken);
                region = regions.FirstOrDefault(r => r.Id == regionId);
            }

            decimal monthlyRate = plan.BasePrice;
            string tariffName = "Standard Monthly";
            string resolvedCurrency = !string.IsNullOrWhiteSpace(currency) ? currency.ToUpperInvariant() : (!string.IsNullOrWhiteSpace(org.Currency) ? org.Currency : "INR");
            if (region != null && region.Tariffs.Count > 0)
            {
                var regTariff = region.Tariffs.FirstOrDefault(t => t.PlanId == plan.Id && (tariffId == null || t.Id == tariffId));
                if (regTariff != null)
                {
                    monthlyRate = regTariff.Price;
                    tariffName = regTariff.Name;
                    if (string.IsNullOrWhiteSpace(currency) && !string.IsNullOrWhiteSpace(regTariff.Currency))
                    {
                        resolvedCurrency = regTariff.Currency;
                    }
                }
            }

            org.PlanId = plan.Id;
            org.PlanName = plan.Name;
            org.PricingRegionId = region?.Id;
            org.PricingRegionCode = region?.Code;
            org.PricingRegionName = region?.Name;
            org.TariffId = tariffId;
            org.TariffName = tariffName;
            org.MonthlyRate = monthlyRate;
            org.Currency = resolvedCurrency;

            if (_platformAdminRepository != null)
            {
                try
                {
                    await _platformAdminRepository.AssignRegionalPlanToOrganizationAsync(
                        organizationId,
                        regionId,
                        plan.Id,
                        tariffId,
                        resolvedCurrency,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to persist regional plan assignment to database for {OrgId}", organizationId);
                }
            }

            AddAuditLog("Regional Plan Assigned", $"Organization '{org.Name}' assigned plan '{plan.Name}' in region '{region?.Name}' ({tariffName} - {resolvedCurrency} {monthlyRate:N0}).", "Info", organizationId);

            if (_emailNotificationService != null && !string.IsNullOrWhiteSpace(org.AdminEmail))
            {
                try
                {
                    var daycareUrl = GetDaycarePortalUrl();
                    var billingUrl = $"{daycareUrl}/billing";
                    var summaryHtml = $@"
                        <div style='margin-bottom: 8px;'><strong>New Plan:</strong> {plan.Name} ({plan.Code})</div>
                        <div style='margin-bottom: 8px;'><strong>Pricing Region:</strong> {region?.Name ?? "Standard"}</div>
                        <div style='margin-bottom: 8px;'><strong>Selected Schedule:</strong> {tariffName}</div>
                        <div style='margin-bottom: 8px;'><strong>Monthly Rate:</strong> ₹{monthlyRate:F2}/month</div>
                        <div style='margin-bottom: 8px;'><strong>Capacity Limits:</strong> Up to {plan.MaxChildren} children, {plan.MaxClassrooms} classrooms, {plan.MaxBranches} branches</div>
                        <div style='margin-bottom: 8px;'><strong>Experience Studio AI:</strong> {(plan.IncludesAiFeatures ? "Enabled ✨" : "Standard")}</div>";

                    await _emailNotificationService.SendPlanOrTariffUpdateNoticeAsync(
                        org.AdminEmail,
                        org.Name,
                        plan.Name,
                        $"Your {plan.Name} Subscription Schedule in {region?.Name ?? "Your Region"}",
                        summaryHtml,
                        "Effective Immediately",
                        billingUrl,
                        org.TenantId,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to dispatch regional plan update email to {Email}", org.AdminEmail);
                }
            }

            return true;
        }

        return false;
    }

    public async Task<List<OrganizationCurrencyHistory>> GetOrganizationCurrencyHistoryAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            return await _platformAdminRepository.GetOrganizationCurrencyHistoryAsync(organizationId, cancellationToken);
        }
        return new List<OrganizationCurrencyHistory>();
    }

    #endregion

}

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class TenantPreferencesRepository : ITenantPreferencesRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public TenantPreferencesRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<TenantPreferences?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT tenant_id, currency, time_zone,
                   operating_hours_json, statutory_ratios_json,
                   billing_rules_json, safety_policies_json,
                   custom_settings_json, updated_at
            FROM public.fn_get_tenant_preferences(@TenantId);";

        var row = await connection.QueryFirstOrDefaultAsync<TenantPreferencesDbRow>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));

        if (row == null) return null;

        var prefs = new TenantPreferences
        {
            TenantId = row.tenant_id,
            Currency = row.currency ?? "INR",
            TimeZone = row.time_zone ?? "Asia/Kolkata",
            UpdatedAt = row.updated_at
        };

        if (!string.IsNullOrWhiteSpace(row.operating_hours_json))
        {
            try { prefs.OperatingHours = JsonSerializer.Deserialize<OperatingHoursConfig>(row.operating_hours_json) ?? new(); } catch { }
        }

        if (!string.IsNullOrWhiteSpace(row.statutory_ratios_json))
        {
            try { prefs.StatutoryRatios = JsonSerializer.Deserialize<Dictionary<string, int>>(row.statutory_ratios_json) ?? prefs.StatutoryRatios; } catch { }
        }

        if (!string.IsNullOrWhiteSpace(row.billing_rules_json))
        {
            try { prefs.BillingRules = JsonSerializer.Deserialize<BillingRulesConfig>(row.billing_rules_json) ?? new(); } catch { }
        }

        if (!string.IsNullOrWhiteSpace(row.safety_policies_json))
        {
            try { prefs.SafetyPolicies = JsonSerializer.Deserialize<SafetyPoliciesConfig>(row.safety_policies_json) ?? new(); } catch { }
        }

        if (!string.IsNullOrWhiteSpace(row.custom_settings_json))
        {
            try { prefs.CustomSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(row.custom_settings_json) ?? new(); } catch { }
        }

        return prefs;
    }

    public async Task UpsertAsync(TenantPreferences preferences, CancellationToken ct = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(ct);

        var operatingHoursJson = JsonSerializer.Serialize(preferences.OperatingHours);
        var statutoryRatiosJson = JsonSerializer.Serialize(preferences.StatutoryRatios);
        var billingRulesJson = JsonSerializer.Serialize(preferences.BillingRules);
        var safetyPoliciesJson = JsonSerializer.Serialize(preferences.SafetyPolicies);
        var customSettingsJson = JsonSerializer.Serialize(preferences.CustomSettings);

        await connection.ExecuteProcedureAsync(
            "public.sp_upsert_tenant_preferences",
            new
            {
                p_tenant_id = preferences.TenantId,
                p_currency = preferences.Currency,
                p_time_zone = preferences.TimeZone,
                p_operating_hours_json = operatingHoursJson,
                p_statutory_ratios_json = statutoryRatiosJson,
                p_billing_rules_json = billingRulesJson,
                p_safety_policies_json = safetyPoliciesJson,
                p_custom_settings_json = customSettingsJson
            },
            cancellationToken: ct);
    }

    private class TenantPreferencesDbRow
    {
        public Guid tenant_id { get; set; }
        public string? currency { get; set; }
        public string? time_zone { get; set; }
        public string? operating_hours_json { get; set; }
        public string? statutory_ratios_json { get; set; }
        public string? billing_rules_json { get; set; }
        public string? safety_policies_json { get; set; }
        public string? custom_settings_json { get; set; }
        public DateTimeOffset updated_at { get; set; }
    }
}

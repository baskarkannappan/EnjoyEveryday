using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Services.Commercial;

public class PlatformPaymentSettingsService : IPlatformPaymentSettingsService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PlatformPaymentSettingsService> _logger;
    private volatile PlatformPaymentSettings _cached = new();
    private volatile bool _loadedFromDb;

    public PlatformPaymentSettingsService(
        IDbConnectionFactory connectionFactory,
        ILogger<PlatformPaymentSettingsService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public PlatformPaymentSettings GetCachedSettings()
    {
        if (!_loadedFromDb)
        {
            try
            {
                using var conn = _connectionFactory.CreateConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
                var row = conn.QueryFirstOrDefault<PlatformPaymentSettings>(
                    "SELECT * FROM public.platform_payment_settings ORDER BY updated_at DESC LIMIT 1;");
                if (row != null)
                {
                    _cached = row;
                }
                _loadedFromDb = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load platform_payment_settings synchronously; using fallback.");
            }
        }

        return _cached;
    }

    public async Task<PlatformPaymentSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            using var conn = await _connectionFactory.CreateConnectionAsync(ct);
            var row = await conn.QueryFirstOrDefaultAsync<PlatformPaymentSettings>(
                new CommandDefinition(
                    "SELECT * FROM public.platform_payment_settings ORDER BY updated_at DESC LIMIT 1;",
                    cancellationToken: ct));

            if (row != null)
            {
                _cached = row;
                _loadedFromDb = true;
                return row;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load platform_payment_settings from PostgreSQL.");
        }

        return _cached;
    }

    public async Task<PlatformPaymentSettings> SaveSettingsAsync(
        PlatformPaymentSettings settings,
        string? updatedByEmail = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(settings.UpiId))
        {
            throw new InvalidOperationException("UPI ID is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.PayeeName))
        {
            throw new InvalidOperationException("Payee Name is required.");
        }

        settings.UpiId = settings.UpiId.Trim();
        settings.PayeeName = settings.PayeeName.Trim();
        settings.BankName = settings.BankName?.Trim();
        settings.AccountNumber = settings.AccountNumber?.Trim();
        settings.IfscCode = settings.IfscCode?.Trim().ToUpperInvariant();
        settings.UpdatedByEmail = updatedByEmail ?? settings.UpdatedByEmail;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var existingId = await conn.QueryFirstOrDefaultAsync<Guid?>(
            new CommandDefinition("SELECT id FROM public.platform_payment_settings ORDER BY updated_at DESC LIMIT 1;", cancellationToken: ct));

        if (existingId.HasValue && existingId.Value != Guid.Empty)
        {
            settings.Id = existingId.Value;
            const string updateSql = @"
                UPDATE public.platform_payment_settings
                SET
                    upi_id = @UpiId,
                    payee_name = @PayeeName,
                    bank_name = @BankName,
                    account_number = @AccountNumber,
                    ifsc_code = @IfscCode,
                    use_custom_qr_image = @UseCustomQrImage,
                    custom_qr_data_uri = @CustomQrDataUri,
                    updated_by_email = @UpdatedByEmail,
                    updated_at = NOW()
                WHERE id = @Id;";
            await conn.ExecuteAsync(new CommandDefinition(updateSql, settings, cancellationToken: ct));
        }
        else
        {
            if (settings.Id == Guid.Empty) settings.Id = Guid.NewGuid();
            const string insertSql = @"
                INSERT INTO public.platform_payment_settings (
                    id, upi_id, payee_name, bank_name, account_number, ifsc_code,
                    use_custom_qr_image, custom_qr_data_uri, updated_by_email, updated_at
                ) VALUES (
                    @Id, @UpiId, @PayeeName, @BankName, @AccountNumber, @IfscCode,
                    @UseCustomQrImage, @CustomQrDataUri, @UpdatedByEmail, NOW()
                );";
            await conn.ExecuteAsync(new CommandDefinition(insertSql, settings, cancellationToken: ct));
        }

        _cached = settings;
        _loadedFromDb = true;
        return settings;
    }
}

